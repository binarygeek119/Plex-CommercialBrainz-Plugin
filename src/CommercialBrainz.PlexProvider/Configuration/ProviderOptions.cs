namespace CommercialBrainz.PlexProvider.Configuration;

/// <summary>
/// Runtime configuration for the CommercialBrainz Plex provider.
/// </summary>
public sealed class ProviderOptions
{
    public string ApiBaseUrl { get; set; } = Constants.DefaultApiBaseUrl;

    public string SiteBaseUrl { get; set; } = Constants.DefaultSiteBaseUrl;

    public bool EnableAudioFingerprint { get; set; } = true;

    public int PhashThreshold { get; set; } = Constants.DefaultPhashThreshold;

    /// <summary>
    /// Absolute roots used to resolve Plex's relative <c>filename</c> for hashing.
    /// </summary>
    public List<string> MediaRoots { get; set; } = new();

    public string FfmpegPath { get; set; } = "ffmpeg";

    public string FfprobePath { get; set; } = "ffprobe";

    public string FpcalcPath { get; set; } = "fpcalc";

    public string BaseUrl { get; set; } = $"http://localhost:{Constants.DefaultPort}";

    public static ProviderOptions FromEnvironment()
    {
        var options = new ProviderOptions();

        var api = Environment.GetEnvironmentVariable("API_BASE_URL");
        if (!string.IsNullOrWhiteSpace(api))
        {
            options.ApiBaseUrl = api.Trim();
        }

        var site = Environment.GetEnvironmentVariable("SITE_BASE_URL");
        if (!string.IsNullOrWhiteSpace(site))
        {
            options.SiteBaseUrl = site.Trim();
        }

        var enableAudio = Environment.GetEnvironmentVariable("ENABLE_AUDIO_FINGERPRINT");
        if (!string.IsNullOrWhiteSpace(enableAudio)
            && bool.TryParse(enableAudio, out var enableAudioValue))
        {
            options.EnableAudioFingerprint = enableAudioValue;
        }

        var threshold = Environment.GetEnvironmentVariable("PHASH_THRESHOLD");
        if (!string.IsNullOrWhiteSpace(threshold)
            && int.TryParse(threshold, out var thresholdValue))
        {
            options.PhashThreshold = thresholdValue;
        }

        var mediaRoots = Environment.GetEnvironmentVariable("MEDIA_ROOTS");
        if (!string.IsNullOrWhiteSpace(mediaRoots))
        {
            options.MediaRoots = mediaRoots
                .Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Where(r => !string.IsNullOrWhiteSpace(r))
                .ToList();
        }

        var ffmpeg = Environment.GetEnvironmentVariable("FFMPEG_PATH");
        if (!string.IsNullOrWhiteSpace(ffmpeg))
        {
            options.FfmpegPath = ffmpeg.Trim();
        }

        var ffprobe = Environment.GetEnvironmentVariable("FFPROBE_PATH");
        if (!string.IsNullOrWhiteSpace(ffprobe))
        {
            options.FfprobePath = ffprobe.Trim();
        }

        var fpcalc = Environment.GetEnvironmentVariable("FPCALC_PATH");
        if (!string.IsNullOrWhiteSpace(fpcalc))
        {
            options.FpcalcPath = fpcalc.Trim();
        }

        var baseUrl = Environment.GetEnvironmentVariable("BASE_URL");
        if (!string.IsNullOrWhiteSpace(baseUrl))
        {
            options.BaseUrl = baseUrl.Trim().TrimEnd('/');
        }

        return options;
    }
}
