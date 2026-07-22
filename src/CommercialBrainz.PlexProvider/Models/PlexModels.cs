using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace CommercialBrainz.PlexProvider.Models;

public class MediaProviderRoot
{
    [JsonProperty("MediaProvider")]
    [JsonPropertyName("MediaProvider")]
    public MediaProvider MediaProvider { get; set; } = new();
}

public class MediaProvider
{
    [JsonProperty("identifier")]
    [JsonPropertyName("identifier")]
    public string Identifier { get; set; } = string.Empty;

    [JsonProperty("title")]
    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonProperty("version")]
    [JsonPropertyName("version")]
    public string Version { get; set; } = string.Empty;

    [JsonProperty("Types")]
    [JsonPropertyName("Types")]
    public List<MediaProviderType> Types { get; set; } = new();

    [JsonProperty("Feature")]
    [JsonPropertyName("Feature")]
    public List<MediaProviderFeature> Feature { get; set; } = new();
}

public class MediaProviderType
{
    [JsonProperty("type")]
    [JsonPropertyName("type")]
    public int Type { get; set; }

    [JsonProperty("Scheme")]
    [JsonPropertyName("Scheme")]
    public List<MediaProviderScheme> Scheme { get; set; } = new();
}

public class MediaProviderScheme
{
    [JsonProperty("scheme")]
    [JsonPropertyName("scheme")]
    public string Scheme { get; set; } = string.Empty;
}

public class MediaProviderFeature
{
    [JsonProperty("type")]
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonProperty("key")]
    [JsonPropertyName("key")]
    public string Key { get; set; } = string.Empty;
}

public class MediaContainerRoot
{
    [JsonProperty("MediaContainer")]
    [JsonPropertyName("MediaContainer")]
    public MediaContainer MediaContainer { get; set; } = new();
}

public class MediaContainer
{
    [JsonProperty("offset")]
    [JsonPropertyName("offset")]
    public int Offset { get; set; }

    [JsonProperty("totalSize")]
    [JsonPropertyName("totalSize")]
    public int TotalSize { get; set; }

    [JsonProperty("identifier")]
    [JsonPropertyName("identifier")]
    public string Identifier { get; set; } = CommercialBrainz.PlexProvider.Constants.ProviderIdentifier;

    [JsonProperty("size")]
    [JsonPropertyName("size")]
    public int Size { get; set; }

    [JsonProperty("Metadata", NullValueHandling = NullValueHandling.Ignore)]
    [JsonPropertyName("Metadata")]
    [System.Text.Json.Serialization.JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<PlexMetadata>? Metadata { get; set; }

    [JsonProperty("Image", NullValueHandling = NullValueHandling.Ignore)]
    [JsonPropertyName("Image")]
    [System.Text.Json.Serialization.JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<PlexImage>? Image { get; set; }
}

public class PlexMetadata
{
    [JsonProperty("ratingKey")]
    [JsonPropertyName("ratingKey")]
    public string RatingKey { get; set; } = string.Empty;

    [JsonProperty("key")]
    [JsonPropertyName("key")]
    public string Key { get; set; } = string.Empty;

    [JsonProperty("guid")]
    [JsonPropertyName("guid")]
    public string Guid { get; set; } = string.Empty;

    [JsonProperty("type")]
    [JsonPropertyName("type")]
    public string Type { get; set; } = "movie";

    [JsonProperty("title")]
    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonProperty("summary", NullValueHandling = NullValueHandling.Ignore)]
    [JsonPropertyName("summary")]
    [System.Text.Json.Serialization.JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Summary { get; set; }

    [JsonProperty("tagline", NullValueHandling = NullValueHandling.Ignore)]
    [JsonPropertyName("tagline")]
    [System.Text.Json.Serialization.JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Tagline { get; set; }

    [JsonProperty("year", NullValueHandling = NullValueHandling.Ignore)]
    [JsonPropertyName("year")]
    [System.Text.Json.Serialization.JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? Year { get; set; }

    [JsonProperty("originallyAvailableAt", NullValueHandling = NullValueHandling.Ignore)]
    [JsonPropertyName("originallyAvailableAt")]
    [System.Text.Json.Serialization.JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? OriginallyAvailableAt { get; set; }

    [JsonProperty("thumb", NullValueHandling = NullValueHandling.Ignore)]
    [JsonPropertyName("thumb")]
    [System.Text.Json.Serialization.JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Thumb { get; set; }

    [JsonProperty("art", NullValueHandling = NullValueHandling.Ignore)]
    [JsonPropertyName("art")]
    [System.Text.Json.Serialization.JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Art { get; set; }

    [JsonProperty("studio", NullValueHandling = NullValueHandling.Ignore)]
    [JsonPropertyName("studio")]
    [System.Text.Json.Serialization.JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Studio { get; set; }

    [JsonProperty("duration", NullValueHandling = NullValueHandling.Ignore)]
    [JsonPropertyName("duration")]
    [System.Text.Json.Serialization.JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public long? Duration { get; set; }

    [JsonProperty("Guid", NullValueHandling = NullValueHandling.Ignore)]
    [JsonPropertyName("Guid")]
    [System.Text.Json.Serialization.JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<PlexGuid>? GuidList { get; set; }

    [JsonProperty("Genre", NullValueHandling = NullValueHandling.Ignore)]
    [JsonPropertyName("Genre")]
    [System.Text.Json.Serialization.JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<PlexTag>? Genre { get; set; }

    [JsonProperty("Director", NullValueHandling = NullValueHandling.Ignore)]
    [JsonPropertyName("Director")]
    [System.Text.Json.Serialization.JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<PlexPerson>? Director { get; set; }

    [JsonProperty("Writer", NullValueHandling = NullValueHandling.Ignore)]
    [JsonPropertyName("Writer")]
    [System.Text.Json.Serialization.JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<PlexPerson>? Writer { get; set; }

    [JsonProperty("Role", NullValueHandling = NullValueHandling.Ignore)]
    [JsonPropertyName("Role")]
    [System.Text.Json.Serialization.JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<PlexPerson>? Role { get; set; }

    [JsonProperty("Image", NullValueHandling = NullValueHandling.Ignore)]
    [JsonPropertyName("Image")]
    [System.Text.Json.Serialization.JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<PlexImage>? Image { get; set; }
}

public class PlexGuid
{
    [JsonProperty("id")]
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;
}

public class PlexTag
{
    [JsonProperty("tag")]
    [JsonPropertyName("tag")]
    public string Tag { get; set; } = string.Empty;
}

public class PlexPerson
{
    [JsonProperty("tag")]
    [JsonPropertyName("tag")]
    public string Tag { get; set; } = string.Empty;

    [JsonProperty("role", NullValueHandling = NullValueHandling.Ignore)]
    [JsonPropertyName("role")]
    [System.Text.Json.Serialization.JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Role { get; set; }
}

public class PlexImage
{
    [JsonProperty("type")]
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonProperty("url")]
    [JsonPropertyName("url")]
    public string Url { get; set; } = string.Empty;

    [JsonProperty("alt", NullValueHandling = NullValueHandling.Ignore)]
    [JsonPropertyName("alt")]
    [System.Text.Json.Serialization.JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Alt { get; set; }
}

/// <summary>
/// POST body for Plex match requests.
/// </summary>
public class MatchRequest
{
    [JsonPropertyName("type")]
    public int Type { get; set; }

    [JsonPropertyName("title")]
    public string? Title { get; set; }

    [JsonPropertyName("year")]
    public int? Year { get; set; }

    [JsonPropertyName("guid")]
    public string? Guid { get; set; }

    [JsonPropertyName("filename")]
    public string? Filename { get; set; }

    [JsonPropertyName("manual")]
    public int? Manual { get; set; }

    [JsonPropertyName("hash")]
    public string? Hash { get; set; }
}
