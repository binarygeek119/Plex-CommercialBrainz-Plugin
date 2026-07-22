using CommercialBrainz.PlexProvider.Models;
using CommercialBrainz.PlexProvider.Serialization;
using CommercialBrainz.PlexProvider.Services;

namespace CommercialBrainz.PlexProvider.Routes;

/// <summary>
/// Movie metadata provider routes for Plex.
/// </summary>
public static class MovieRoutes
{
    public static MediaProviderRoot GetProviderDefinition()
    {
        return new MediaProviderRoot
        {
            MediaProvider = new MediaProvider
            {
                Identifier = Constants.ProviderIdentifier,
                Title = Constants.ProviderTitle,
                Version = Constants.ProviderVersion,
                Types =
                [
                    new MediaProviderType
                    {
                        Type = Constants.MovieType,
                        Scheme =
                        [
                            new MediaProviderScheme { Scheme = Constants.ProviderIdentifier }
                        ]
                    }
                ],
                Feature =
                [
                    new MediaProviderFeature
                    {
                        Type = "metadata",
                        Key = "/library/metadata"
                    },
                    new MediaProviderFeature
                    {
                        Type = "match",
                        Key = "/library/metadata/matches"
                    }
                ]
            }
        };
    }

    public static void MapMovieRoutes(this WebApplication app)
    {
        var movie = app.MapGroup("/movie");

        movie.MapGet("/", () => PlexJson.JsonResult(GetProviderDefinition()));

        movie.MapPost("/library/metadata/matches", async (
            MatchRequest request,
            MatchService matchService,
            CancellationToken cancellationToken) =>
        {
            var result = await matchService.MatchAsync(request, cancellationToken).ConfigureAwait(false);
            return PlexJson.JsonResult(result);
        });

        movie.MapGet("/library/metadata/{ratingKey}", async (
            string ratingKey,
            MetadataService metadataService,
            CancellationToken cancellationToken) =>
        {
            var result = await metadataService.GetMetadataAsync(ratingKey, cancellationToken).ConfigureAwait(false);
            return result is null ? Results.NotFound() : PlexJson.JsonResult(result);
        });

        movie.MapGet("/library/metadata/{ratingKey}/images", async (
            string ratingKey,
            MetadataService metadataService,
            CancellationToken cancellationToken) =>
        {
            var result = await metadataService.GetImagesAsync(ratingKey, cancellationToken).ConfigureAwait(false);
            return result is null ? Results.NotFound() : PlexJson.JsonResult(result);
        });
    }
}
