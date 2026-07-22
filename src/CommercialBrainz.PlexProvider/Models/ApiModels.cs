using System.Text.Json.Serialization;

namespace CommercialBrainz.PlexProvider.Models;

/// <summary>
/// Hash lookup match from CommercialBrainz.
/// </summary>
public class DuplicateMatch
{
    [JsonPropertyName("video_sbid")]
    public string VideoSbid { get; set; } = string.Empty;

    [JsonPropertyName("youtube_id")]
    public string? YoutubeId { get; set; }

    [JsonPropertyName("commercial_id")]
    public string? CommercialId { get; set; }

    [JsonPropertyName("match_type")]
    public string MatchType { get; set; } = string.Empty;

    [JsonPropertyName("phash")]
    public string? Phash { get; set; }

    [JsonPropertyName("file_sha256")]
    public string? FileSha256 { get; set; }

    [JsonPropertyName("audio_fingerprint")]
    public string? AudioFingerprint { get; set; }

    [JsonPropertyName("hamming_distance")]
    public int? HammingDistance { get; set; }

    [JsonPropertyName("visibility")]
    public string? Visibility { get; set; }
}

/// <summary>
/// POST body for hash lookup.
/// </summary>
public class HashLookupRequest
{
    [JsonPropertyName("phash")]
    public string? Phash { get; set; }

    [JsonPropertyName("file_sha256")]
    public string? FileSha256 { get; set; }

    [JsonPropertyName("audio_fingerprint")]
    public string? AudioFingerprint { get; set; }

    [JsonPropertyName("threshold")]
    public int? Threshold { get; set; }
}

/// <summary>
/// Search result entry.
/// </summary>
public class SearchResultDto
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("sbid")]
    public Guid Sbid { get; set; }

    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("subtitle")]
    public string? Subtitle { get; set; }
}

/// <summary>
/// Nested commercial summary on a video detail.
/// </summary>
public class CommercialPublicDto
{
    [JsonPropertyName("sbid")]
    public Guid Sbid { get; set; }

    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("advertiser_id")]
    public Guid? AdvertiserId { get; set; }

    [JsonPropertyName("agency_id")]
    public Guid? AgencyId { get; set; }

    [JsonPropertyName("year")]
    public int? Year { get; set; }

    [JsonPropertyName("decade")]
    public int? Decade { get; set; }

    [JsonPropertyName("campaign_name")]
    public string? CampaignName { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }
}

/// <summary>
/// Advertiser summary.
/// </summary>
public class AdvertiserPublicDto
{
    [JsonPropertyName("sbid")]
    public Guid Sbid { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("logo_url")]
    public string? LogoUrl { get; set; }
}

/// <summary>
/// Agency summary.
/// </summary>
public class AgencyPublicDto
{
    [JsonPropertyName("sbid")]
    public Guid Sbid { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
}

/// <summary>
/// Credit on a video.
/// </summary>
public class VideoCreditDto
{
    [JsonPropertyName("role")]
    public string Role { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
}

/// <summary>
/// Full video detail from GET /videos/{sbid}.
/// </summary>
public class VideoDetailDto
{
    [JsonPropertyName("sbid")]
    public Guid Sbid { get; set; }

    [JsonPropertyName("commercial_id")]
    public Guid CommercialId { get; set; }

    [JsonPropertyName("youtube_id")]
    public string? YoutubeId { get; set; }

    [JsonPropertyName("youtube_url")]
    public string? YoutubeUrl { get; set; }

    [JsonPropertyName("thumbnail_url")]
    public string? ThumbnailUrl { get; set; }

    [JsonPropertyName("channel_name")]
    public string? ChannelName { get; set; }

    [JsonPropertyName("upload_date")]
    public string? UploadDate { get; set; }

    [JsonPropertyName("duration_ms")]
    public int? DurationMs { get; set; }

    [JsonPropertyName("language")]
    public string? Language { get; set; }

    [JsonPropertyName("region")]
    public string? Region { get; set; }

    [JsonPropertyName("network")]
    public string? Network { get; set; }

    [JsonPropertyName("transcript")]
    public string? Transcript { get; set; }

    [JsonPropertyName("slogan")]
    public string? Slogan { get; set; }

    [JsonPropertyName("cta_text")]
    public string? CtaText { get; set; }

    [JsonPropertyName("version_label")]
    public string? VersionLabel { get; set; }

    [JsonPropertyName("commercial")]
    public CommercialPublicDto? Commercial { get; set; }

    [JsonPropertyName("advertiser")]
    public AdvertiserPublicDto? Advertiser { get; set; }

    [JsonPropertyName("agency")]
    public AgencyPublicDto? Agency { get; set; }

    [JsonPropertyName("credits")]
    public List<VideoCreditDto> Credits { get; set; } = new();

    [JsonPropertyName("tags")]
    public List<string> Tags { get; set; } = new();
}
