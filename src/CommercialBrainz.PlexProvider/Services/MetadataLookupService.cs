using CommercialBrainz.PlexProvider.Api;
using CommercialBrainz.PlexProvider.Configuration;
using CommercialBrainz.PlexProvider.Hashing;
using CommercialBrainz.PlexProvider.Mappers;
using CommercialBrainz.PlexProvider.Models;
using Microsoft.Extensions.Options;

namespace CommercialBrainz.PlexProvider.Services;

/// <summary>
/// Shared lookup logic: provider GUID → hash match → title search.
/// </summary>
public class MetadataLookupService
{
    private readonly CommercialBrainzApi _api;
    private readonly MediaHasher _hasher;
    private readonly PathResolver _pathResolver;
    private readonly ProviderOptions _options;
    private readonly ILogger<MetadataLookupService> _logger;

    public MetadataLookupService(
        CommercialBrainzApi api,
        MediaHasher hasher,
        PathResolver pathResolver,
        IOptions<ProviderOptions> options,
        ILogger<MetadataLookupService> logger)
    {
        _api = api;
        _hasher = hasher;
        _pathResolver = pathResolver;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<VideoDetailDto?> ResolveVideoAsync(MatchRequest request, CancellationToken cancellationToken)
    {
        if (PlexMetadataMapper.TryParseGuid(request.Guid, out var sbid))
        {
            var byId = await _api.GetVideoAsync(sbid, cancellationToken).ConfigureAwait(false);
            if (byId is not null)
            {
                return byId;
            }
        }

        var path = _pathResolver.Resolve(request.Filename);
        if (!string.IsNullOrWhiteSpace(path))
        {
            var byHash = await ResolveByHashAsync(path, cancellationToken).ConfigureAwait(false);
            if (byHash is not null)
            {
                return byHash;
            }
        }

        if (!string.IsNullOrWhiteSpace(request.Title))
        {
            var search = await SearchVideosAsync(request.Title, cancellationToken).ConfigureAwait(false);
            var first = search.FirstOrDefault();
            if (first is not null && Guid.TryParse(first.RatingKey, out var videoSbid))
            {
                return await _api.GetVideoAsync(videoSbid, cancellationToken).ConfigureAwait(false);
            }
        }

        return null;
    }

    public async Task<IReadOnlyList<PlexMetadata>> GetSearchResultsAsync(MatchRequest request, CancellationToken cancellationToken)
    {
        var results = new List<PlexMetadata>();

        if (PlexMetadataMapper.TryParseGuid(request.Guid, out var sbid))
        {
            var video = await _api.GetVideoAsync(sbid, cancellationToken).ConfigureAwait(false);
            if (video is not null)
            {
                results.Add(PlexMetadataMapper.ToMetadata(video));
                return results;
            }
        }

        var path = _pathResolver.Resolve(request.Filename);
        if (!string.IsNullOrWhiteSpace(path))
        {
            var matches = await CollectHashMatchesAsync(path, cancellationToken).ConfigureAwait(false);
            foreach (var match in matches)
            {
                if (!Guid.TryParse(match.VideoSbid, out var matchId))
                {
                    continue;
                }

                var detail = await _api.GetVideoAsync(matchId, cancellationToken).ConfigureAwait(false);
                if (detail is not null)
                {
                    results.Add(PlexMetadataMapper.ToMetadata(detail));
                }
            }

            if (results.Count > 0)
            {
                return results;
            }
        }

        if (!string.IsNullOrWhiteSpace(request.Title))
        {
            return await SearchVideosAsync(request.Title, cancellationToken).ConfigureAwait(false);
        }

        return results;
    }

    public Task<VideoDetailDto?> GetVideoByRatingKeyAsync(string ratingKey, CancellationToken cancellationToken)
    {
        if (!PlexMetadataMapper.TryParseRatingKey(ratingKey, out var sbid))
        {
            return Task.FromResult<VideoDetailDto?>(null);
        }

        return _api.GetVideoAsync(sbid, cancellationToken);
    }

    private async Task<VideoDetailDto?> ResolveByHashAsync(string path, CancellationToken cancellationToken)
    {
        var matches = await CollectHashMatchesAsync(path, cancellationToken).ConfigureAwait(false);
        var best = matches.FirstOrDefault();
        if (best is null || !Guid.TryParse(best.VideoSbid, out var sbid))
        {
            return null;
        }

        _logger.LogInformation(
            "CommercialBrainz matched {Path} via {MatchType} (distance={Distance})",
            path,
            best.MatchType,
            best.HammingDistance);
        return await _api.GetVideoAsync(sbid, cancellationToken).ConfigureAwait(false);
    }

    private async Task<List<DuplicateMatch>> CollectHashMatchesAsync(string path, CancellationToken cancellationToken)
    {
        var ordered = new List<DuplicateMatch>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        void AddMatches(IReadOnlyList<DuplicateMatch> matches)
        {
            foreach (var match in matches)
            {
                if (seen.Add(match.VideoSbid))
                {
                    ordered.Add(match);
                }
            }
        }

        try
        {
            var sha = await _hasher.ComputeFileSha256Async(path, cancellationToken).ConfigureAwait(false);
            AddMatches(await _api.LookupByFileSha256Async(sha, cancellationToken).ConfigureAwait(false));
            if (ordered.Count > 0)
            {
                return ordered;
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "SHA-256 lookup failed for {Path}", path);
        }

        if (_options.EnableAudioFingerprint)
        {
            try
            {
                var fingerprint = await _hasher.TryComputeAudioFingerprintAsync(path, cancellationToken).ConfigureAwait(false);
                if (!string.IsNullOrWhiteSpace(fingerprint))
                {
                    AddMatches(await _api.LookupByAudioFingerprintAsync(fingerprint, cancellationToken).ConfigureAwait(false));
                    if (ordered.Count > 0)
                    {
                        return ordered;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Audio fingerprint lookup failed for {Path}", path);
            }
        }

        try
        {
            var phash = await _hasher.TryComputeVideoPhashAsync(path, cancellationToken).ConfigureAwait(false);
            if (!string.IsNullOrWhiteSpace(phash))
            {
                AddMatches(await _api.LookupByPhashAsync(phash, _options.PhashThreshold, cancellationToken).ConfigureAwait(false));
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "pHash lookup failed for {Path}", path);
        }

        return ordered;
    }

    private async Task<List<PlexMetadata>> SearchVideosAsync(string name, CancellationToken cancellationToken)
    {
        var results = new List<PlexMetadata>();
        var videoHits = await _api.SearchAsync(name, "video", 25, cancellationToken).ConfigureAwait(false);
        foreach (var hit in videoHits)
        {
            if (string.Equals(hit.Type, "video", StringComparison.OrdinalIgnoreCase))
            {
                var detail = await _api.GetVideoAsync(hit.Sbid, cancellationToken).ConfigureAwait(false);
                results.Add(detail is not null
                    ? PlexMetadataMapper.ToMetadata(detail)
                    : PlexMetadataMapper.ToMetadata(hit));
            }
            else
            {
                results.Add(PlexMetadataMapper.ToMetadata(hit));
            }
        }

        if (results.Count == 0)
        {
            var commercialHits = await _api.SearchAsync(name, "commercial", 25, cancellationToken).ConfigureAwait(false);
            foreach (var hit in commercialHits)
            {
                results.Add(PlexMetadataMapper.ToMetadata(hit));
            }
        }

        return results;
    }
}
