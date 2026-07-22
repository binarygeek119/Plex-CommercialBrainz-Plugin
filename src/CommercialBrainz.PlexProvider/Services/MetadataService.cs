using CommercialBrainz.PlexProvider.Mappers;
using CommercialBrainz.PlexProvider.Models;

namespace CommercialBrainz.PlexProvider.Services;

/// <summary>
/// Handles Plex metadata and images feature requests.
/// </summary>
public class MetadataService
{
    private readonly MetadataLookupService _lookup;

    public MetadataService(MetadataLookupService lookup)
    {
        _lookup = lookup;
    }

    public async Task<MediaContainerRoot?> GetMetadataAsync(string ratingKey, CancellationToken cancellationToken)
    {
        var video = await _lookup.GetVideoByRatingKeyAsync(ratingKey, cancellationToken).ConfigureAwait(false);
        if (video is null)
        {
            return null;
        }

        var metadata = PlexMetadataMapper.ToMetadata(video);
        return new MediaContainerRoot
        {
            MediaContainer = new MediaContainer
            {
                Offset = 0,
                TotalSize = 1,
                Size = 1,
                Identifier = Constants.ProviderIdentifier,
                Metadata = [metadata]
            }
        };
    }

    public async Task<MediaContainerRoot?> GetImagesAsync(string ratingKey, CancellationToken cancellationToken)
    {
        var video = await _lookup.GetVideoByRatingKeyAsync(ratingKey, cancellationToken).ConfigureAwait(false);
        if (video is null)
        {
            return null;
        }

        var images = PlexMetadataMapper.ToImages(video);
        return new MediaContainerRoot
        {
            MediaContainer = new MediaContainer
            {
                Offset = 0,
                TotalSize = images.Count,
                Size = images.Count,
                Identifier = Constants.ProviderIdentifier,
                Image = images
            }
        };
    }
}
