using CommercialBrainz.PlexProvider.Mappers;
using CommercialBrainz.PlexProvider.Models;
using CommercialBrainz.PlexProvider.Services;

namespace CommercialBrainz.PlexProvider.Services;

/// <summary>
/// Handles Plex match feature requests.
/// </summary>
public class MatchService
{
    private readonly MetadataLookupService _lookup;
    private readonly ILogger<MatchService> _logger;

    public MatchService(MetadataLookupService lookup, ILogger<MatchService> logger)
    {
        _lookup = lookup;
        _logger = logger;
    }

    public async Task<MediaContainerRoot> MatchAsync(MatchRequest request, CancellationToken cancellationToken)
    {
        if (request.Type != Constants.MovieType && request.Type != 0)
        {
            _logger.LogDebug("Ignoring non-movie match request type={Type}", request.Type);
            return EmptyContainer();
        }

        var manual = request.Manual == 1;
        List<PlexMetadata> metadata;

        if (manual)
        {
            metadata = (await _lookup.GetSearchResultsAsync(request, cancellationToken).ConfigureAwait(false)).ToList();
        }
        else
        {
            var video = await _lookup.ResolveVideoAsync(request, cancellationToken).ConfigureAwait(false);
            metadata = video is null
                ? []
                : [PlexMetadataMapper.ToMetadata(video)];
        }

        return new MediaContainerRoot
        {
            MediaContainer = new MediaContainer
            {
                Offset = 0,
                TotalSize = metadata.Count,
                Size = metadata.Count,
                Identifier = Constants.ProviderIdentifier,
                Metadata = metadata
            }
        };
    }

    private static MediaContainerRoot EmptyContainer()
        => new()
        {
            MediaContainer = new MediaContainer
            {
                Offset = 0,
                TotalSize = 0,
                Size = 0,
                Identifier = Constants.ProviderIdentifier,
                Metadata = []
            }
        };
}
