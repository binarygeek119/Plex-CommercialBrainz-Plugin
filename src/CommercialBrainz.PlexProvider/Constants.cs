namespace CommercialBrainz.PlexProvider;

/// <summary>
/// Shared constants for the CommercialBrainz Plex metadata provider.
/// </summary>
public static class Constants
{
    /// <summary>
    /// Plex custom metadata provider identifier (GUID scheme).
    /// </summary>
    public const string ProviderIdentifier = "tv.plex.agents.custom.commercialbrainz.movie";

    /// <summary>
    /// Human-readable provider title shown in Plex.
    /// </summary>
    public const string ProviderTitle = "CommercialBrainz";

    /// <summary>
    /// Provider version string.
    /// </summary>
    public const string ProviderVersion = "1.0.0";

    /// <summary>
    /// Default API base URL including the /api/v1 prefix.
    /// </summary>
    public const string DefaultApiBaseUrl = "https://commercialbrainz.duckdns.org/api/v1";

    /// <summary>
    /// Default public website base URL.
    /// </summary>
    public const string DefaultSiteBaseUrl = "https://commercialbrainz.duckdns.org";

    /// <summary>
    /// HTTP User-Agent required by CommercialBrainz scraper etiquette.
    /// </summary>
    public const string UserAgent = "CommercialBrainz.PlexProvider/1.0 (+https://github.com/binarygeek119/CommercialBrainz)";

    /// <summary>
    /// Default pHash Hamming distance threshold.
    /// </summary>
    public const int DefaultPhashThreshold = 12;

    /// <summary>
    /// Default listen port.
    /// </summary>
    public const int DefaultPort = 8765;

    /// <summary>
    /// Plex metadata type for movies.
    /// </summary>
    public const int MovieType = 1;
}
