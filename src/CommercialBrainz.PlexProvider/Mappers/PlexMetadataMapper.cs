using System.Text;
using CommercialBrainz.PlexProvider.Models;

namespace CommercialBrainz.PlexProvider.Mappers;

/// <summary>
/// Maps CommercialBrainz API responses to Plex Media Provider metadata objects.
/// </summary>
public static class PlexMetadataMapper
{
    public static string BuildGuid(Guid videoSbid)
        => $"{Constants.ProviderIdentifier}://movie/{videoSbid}";

    public static string BuildKey(Guid videoSbid)
        => $"/library/metadata/{videoSbid}";

    public static bool TryParseRatingKey(string? ratingKey, out Guid sbid)
    {
        sbid = default;
        if (string.IsNullOrWhiteSpace(ratingKey))
        {
            return false;
        }

        return Guid.TryParse(ratingKey, out sbid);
    }

    public static bool TryParseGuid(string? guid, out Guid sbid)
    {
        sbid = default;
        if (string.IsNullOrWhiteSpace(guid))
        {
            return false;
        }

        // tv.plex.agents.custom.commercialbrainz.movie://movie/{sbid}
        var prefix = $"{Constants.ProviderIdentifier}://movie/";
        if (guid.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        {
            return Guid.TryParse(guid[prefix.Length..], out sbid);
        }

        // commercialbrainz://{sbid} or bare UUID
        const string shortPrefix = "commercialbrainz://";
        if (guid.StartsWith(shortPrefix, StringComparison.OrdinalIgnoreCase))
        {
            return Guid.TryParse(guid[shortPrefix.Length..], out sbid);
        }

        return Guid.TryParse(guid, out sbid);
    }

    public static PlexMetadata ToMetadata(VideoDetailDto video, bool includeImages = true)
    {
        var title = GetTitle(video);
        var thumb = GetThumbnailUrl(video);
        var metadata = new PlexMetadata
        {
            RatingKey = video.Sbid.ToString(),
            Key = BuildKey(video.Sbid),
            Guid = BuildGuid(video.Sbid),
            Type = "movie",
            Title = title,
            Summary = BuildOverview(video),
            Tagline = string.IsNullOrWhiteSpace(video.Slogan) ? null : video.Slogan,
            Year = video.Commercial?.Year,
            OriginallyAvailableAt = video.Commercial?.Year is int year
                ? $"{year:D4}-01-01"
                : null,
            Thumb = thumb,
            Art = thumb,
            Studio = video.Advertiser?.Name ?? video.Agency?.Name,
            Duration = video.DurationMs,
            GuidList = BuildExternalGuids(video),
            Genre = BuildGenres(video),
            Director = BuildPeople(video, "director"),
            Writer = BuildPeople(video, "writer"),
            Role = BuildPeople(video, "actor")
        };

        if (includeImages && !string.IsNullOrWhiteSpace(thumb))
        {
            metadata.Image =
            [
                new PlexImage
                {
                    Type = "coverPoster",
                    Url = thumb,
                    Alt = title
                }
            ];
        }

        return metadata;
    }

    public static PlexMetadata ToMetadata(SearchResultDto hit)
    {
        return new PlexMetadata
        {
            RatingKey = hit.Sbid.ToString(),
            Key = BuildKey(hit.Sbid),
            Guid = BuildGuid(hit.Sbid),
            Type = "movie",
            Title = hit.Title,
            Summary = hit.Subtitle
        };
    }

    public static List<PlexImage> ToImages(VideoDetailDto video)
    {
        var url = GetThumbnailUrl(video);
        if (string.IsNullOrWhiteSpace(url))
        {
            return [];
        }

        return
        [
            new PlexImage
            {
                Type = "coverPoster",
                Url = url,
                Alt = GetTitle(video)
            }
        ];
    }

    public static string? GetThumbnailUrl(VideoDetailDto video)
    {
        if (!string.IsNullOrWhiteSpace(video.ThumbnailUrl))
        {
            return video.ThumbnailUrl;
        }

        if (!string.IsNullOrWhiteSpace(video.YoutubeId))
        {
            return $"https://i.ytimg.com/vi/{video.YoutubeId}/hqdefault.jpg";
        }

        return null;
    }

    public static string GetTitle(VideoDetailDto video)
    {
        if (!string.IsNullOrWhiteSpace(video.Commercial?.Title))
        {
            if (!string.IsNullOrWhiteSpace(video.VersionLabel))
            {
                return $"{video.Commercial.Title} ({video.VersionLabel})";
            }

            return video.Commercial.Title;
        }

        if (!string.IsNullOrWhiteSpace(video.Advertiser?.Name) && !string.IsNullOrWhiteSpace(video.VersionLabel))
        {
            return $"{video.Advertiser.Name} — {video.VersionLabel}";
        }

        return video.Advertiser?.Name ?? video.YoutubeId ?? video.Sbid.ToString();
    }

    public static string? BuildOverview(VideoDetailDto video)
    {
        var sb = new StringBuilder();
        if (!string.IsNullOrWhiteSpace(video.Commercial?.Description))
        {
            sb.Append(video.Commercial.Description);
        }

        if (!string.IsNullOrWhiteSpace(video.Slogan))
        {
            if (sb.Length > 0)
            {
                sb.AppendLine().AppendLine();
            }

            sb.Append("Slogan: ").Append(video.Slogan);
        }

        if (!string.IsNullOrWhiteSpace(video.CtaText))
        {
            if (sb.Length > 0)
            {
                sb.AppendLine().AppendLine();
            }

            sb.Append("CTA: ").Append(video.CtaText);
        }

        if (!string.IsNullOrWhiteSpace(video.Commercial?.CampaignName))
        {
            if (sb.Length > 0)
            {
                sb.AppendLine().AppendLine();
            }

            sb.Append("Campaign: ").Append(video.Commercial.CampaignName);
        }

        return sb.Length == 0 ? null : sb.ToString();
    }

    private static List<PlexGuid>? BuildExternalGuids(VideoDetailDto video)
    {
        var list = new List<PlexGuid>
        {
            new() { Id = $"commercialbrainz://{video.Sbid}" },
            new() { Id = $"commercialbrainz.commercial://{video.CommercialId}" }
        };

        if (!string.IsNullOrWhiteSpace(video.YoutubeId))
        {
            list.Add(new PlexGuid { Id = $"youtube://{video.YoutubeId}" });
        }

        return list;
    }

    private static List<PlexTag>? BuildGenres(VideoDetailDto video)
    {
        var tags = new List<string>(video.Tags ?? Enumerable.Empty<string>());
        if (!string.IsNullOrWhiteSpace(video.VersionLabel))
        {
            tags.Add(video.VersionLabel);
        }

        if (!string.IsNullOrWhiteSpace(video.Network))
        {
            tags.Add(video.Network);
        }

        if (!string.IsNullOrWhiteSpace(video.Region))
        {
            tags.Add(video.Region);
        }

        if (tags.Count == 0)
        {
            return null;
        }

        return tags
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Select(t => new PlexTag { Tag = t })
            .ToList();
    }

    private static List<PlexPerson>? BuildPeople(VideoDetailDto video, string kind)
    {
        var people = new List<PlexPerson>();
        foreach (var credit in video.Credits ?? Enumerable.Empty<VideoCreditDto>())
        {
            if (string.IsNullOrWhiteSpace(credit.Name))
            {
                continue;
            }

            var mapped = MapPersonKind(credit.Role);
            if (!string.Equals(mapped, kind, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            people.Add(new PlexPerson
            {
                Tag = credit.Name,
                Role = string.IsNullOrWhiteSpace(credit.Role) ? null : credit.Role
            });
        }

        return people.Count == 0 ? null : people;
    }

    private static string MapPersonKind(string? role)
    {
        if (string.IsNullOrWhiteSpace(role))
        {
            return "actor";
        }

        var r = role.Trim().ToLowerInvariant();
        if (r.Contains("director", StringComparison.Ordinal) || r.Contains("directed", StringComparison.Ordinal))
        {
            return "director";
        }

        if (r.Contains("writer", StringComparison.Ordinal) || r.Contains("copy", StringComparison.Ordinal))
        {
            return "writer";
        }

        if (r.Contains("actor", StringComparison.Ordinal) || r.Contains("talent", StringComparison.Ordinal) || r.Contains("cast", StringComparison.Ordinal))
        {
            return "actor";
        }

        if (r.Contains("composer", StringComparison.Ordinal) || r.Contains("music", StringComparison.Ordinal))
        {
            return "writer";
        }

        if (r.Contains("producer", StringComparison.Ordinal))
        {
            return "director";
        }

        return "actor";
    }
}
