using System.Globalization;
using System.Net.Http.Json;
using System.Text;
using CommercialBrainz.PlexProvider.Configuration;
using CommercialBrainz.PlexProvider.Models;
using Microsoft.Extensions.Options;

namespace CommercialBrainz.PlexProvider.Api;

/// <summary>
/// HTTP client for the CommercialBrainz public API.
/// </summary>
public class CommercialBrainzApi
{
    private static readonly SemaphoreSlim Throttle = new(1, 1);
    private static readonly TimeSpan MinInterval = TimeSpan.FromSeconds(1.05);
    private static DateTime _lastRequestUtc = DateTime.MinValue;

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ProviderOptions _options;
    private readonly ILogger<CommercialBrainzApi> _logger;

    public CommercialBrainzApi(
        IHttpClientFactory httpClientFactory,
        IOptions<ProviderOptions> options,
        ILogger<CommercialBrainzApi> logger)
    {
        _httpClientFactory = httpClientFactory;
        _options = options.Value;
        _logger = logger;
    }

    public Task<IReadOnlyList<DuplicateMatch>> LookupByFileSha256Async(string sha256, CancellationToken cancellationToken)
        => LookupGetAsync($"file_sha256={Uri.EscapeDataString(sha256)}", cancellationToken);

    public Task<IReadOnlyList<DuplicateMatch>> LookupByPhashAsync(string phash, int? threshold, CancellationToken cancellationToken)
    {
        var query = new StringBuilder("phash=").Append(Uri.EscapeDataString(phash));
        if (threshold.HasValue)
        {
            query.Append(CultureInfo.InvariantCulture, $"&threshold={threshold.Value}");
        }

        return LookupGetAsync(query.ToString(), cancellationToken);
    }

    public async Task<IReadOnlyList<DuplicateMatch>> LookupByAudioFingerprintAsync(string fingerprint, CancellationToken cancellationToken)
    {
        var body = new HashLookupRequest { AudioFingerprint = fingerprint };
        return await PostLookupAsync(body, cancellationToken).ConfigureAwait(false);
    }

    public async Task<VideoDetailDto?> GetVideoAsync(Guid sbid, CancellationToken cancellationToken)
    {
        var client = CreateClient();
        var url = $"{GetApiBase().TrimEnd('/')}/videos/{sbid}";
        await WaitForRateLimitAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            using var response = await client.GetAsync(url, cancellationToken).ConfigureAwait(false);
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return null;
            }

            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<VideoDetailDto>(cancellationToken: cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to fetch CommercialBrainz video {Sbid}", sbid);
            return null;
        }
    }

    public async Task<IReadOnlyList<SearchResultDto>> SearchAsync(string query, string type, int limit, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return Array.Empty<SearchResultDto>();
        }

        var client = CreateClient();
        var url = $"{GetApiBase().TrimEnd('/')}/search?query={Uri.EscapeDataString(query)}&type={Uri.EscapeDataString(type)}&limit={limit}";
        await WaitForRateLimitAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            using var response = await client.GetAsync(url, cancellationToken).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
            var results = await response.Content.ReadFromJsonAsync<List<SearchResultDto>>(cancellationToken: cancellationToken).ConfigureAwait(false);
            return results ?? (IReadOnlyList<SearchResultDto>)Array.Empty<SearchResultDto>();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "CommercialBrainz search failed for {Query}", query);
            return Array.Empty<SearchResultDto>();
        }
    }

    private async Task<IReadOnlyList<DuplicateMatch>> LookupGetAsync(string query, CancellationToken cancellationToken)
    {
        var client = CreateClient();
        var url = $"{GetApiBase().TrimEnd('/')}/hashes/lookup?{query}";
        await WaitForRateLimitAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            using var response = await client.GetAsync(url, cancellationToken).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
            var results = await response.Content.ReadFromJsonAsync<List<DuplicateMatch>>(cancellationToken: cancellationToken).ConfigureAwait(false);
            return results ?? (IReadOnlyList<DuplicateMatch>)Array.Empty<DuplicateMatch>();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "CommercialBrainz hash lookup failed ({Query})", query);
            return Array.Empty<DuplicateMatch>();
        }
    }

    private async Task<IReadOnlyList<DuplicateMatch>> PostLookupAsync(HashLookupRequest body, CancellationToken cancellationToken)
    {
        var client = CreateClient();
        var url = $"{GetApiBase().TrimEnd('/')}/hashes/lookup";
        await WaitForRateLimitAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            using var response = await client.PostAsJsonAsync(url, body, cancellationToken).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
            var results = await response.Content.ReadFromJsonAsync<List<DuplicateMatch>>(cancellationToken: cancellationToken).ConfigureAwait(false);
            return results ?? (IReadOnlyList<DuplicateMatch>)Array.Empty<DuplicateMatch>();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "CommercialBrainz POST hash lookup failed");
            return Array.Empty<DuplicateMatch>();
        }
    }

    private HttpClient CreateClient()
    {
        var client = _httpClientFactory.CreateClient(nameof(CommercialBrainzApi));
        if (!client.DefaultRequestHeaders.UserAgent.TryParseAdd(Constants.UserAgent))
        {
            client.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", Constants.UserAgent);
        }

        return client;
    }

    private string GetApiBase()
    {
        return string.IsNullOrWhiteSpace(_options.ApiBaseUrl)
            ? Constants.DefaultApiBaseUrl
            : _options.ApiBaseUrl.Trim();
    }

    private static async Task WaitForRateLimitAsync(CancellationToken cancellationToken)
    {
        await Throttle.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var elapsed = DateTime.UtcNow - _lastRequestUtc;
            if (elapsed < MinInterval)
            {
                await Task.Delay(MinInterval - elapsed, cancellationToken).ConfigureAwait(false);
            }

            _lastRequestUtc = DateTime.UtcNow;
        }
        finally
        {
            Throttle.Release();
        }
    }
}
