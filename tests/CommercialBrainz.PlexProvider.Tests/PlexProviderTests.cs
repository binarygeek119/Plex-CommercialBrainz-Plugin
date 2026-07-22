using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using CommercialBrainz.PlexProvider.Mappers;
using CommercialBrainz.PlexProvider.Models;
using CommercialBrainz.PlexProvider.Routes;
using Microsoft.AspNetCore.Mvc.Testing;
using Newtonsoft.Json.Linq;
using Xunit;

namespace CommercialBrainz.PlexProvider.Tests;

public class PlexMetadataMapperTests
{
    [Fact]
    public void ToMetadata_MapsCoreFieldsAndGuids()
    {
        var sbid = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        var commercialId = Guid.Parse("11111111-2222-3333-4444-555555555555");
        var video = new VideoDetailDto
        {
            Sbid = sbid,
            CommercialId = commercialId,
            YoutubeId = "dQw4w9WgXcQ",
            Slogan = "Just Do It",
            VersionLabel = "30s",
            DurationMs = 30000,
            Network = "NBC",
            Region = "US",
            Tags = ["sports"],
            Commercial = new CommercialPublicDto
            {
                Title = "Nike Air",
                Year = 1990,
                Description = "A classic spot.",
                CampaignName = "Air"
            },
            Advertiser = new AdvertiserPublicDto { Name = "Nike" },
            Agency = new AgencyPublicDto { Name = "Wieden+Kennedy" },
            Credits =
            [
                new VideoCreditDto { Name = "Jane Director", Role = "Director" },
                new VideoCreditDto { Name = "Copy Writer", Role = "Writer" },
                new VideoCreditDto { Name = "Star Talent", Role = "Talent" }
            ]
        };

        var metadata = PlexMetadataMapper.ToMetadata(video);

        Assert.Equal(sbid.ToString(), metadata.RatingKey);
        Assert.Equal($"tv.plex.agents.custom.commercialbrainz.movie://movie/{sbid}", metadata.Guid);
        Assert.Equal("Nike Air (30s)", metadata.Title);
        Assert.Equal(1990, metadata.Year);
        Assert.Equal("1990-01-01", metadata.OriginallyAvailableAt);
        Assert.Equal("Nike", metadata.Studio);
        Assert.Equal(30000, metadata.Duration);
        Assert.Equal("Just Do It", metadata.Tagline);
        Assert.Contains("Slogan: Just Do It", metadata.Summary);
        Assert.NotNull(metadata.Director);
        Assert.Contains(metadata.Director!, d => d.Tag == "Jane Director");
        Assert.NotNull(metadata.Writer);
        Assert.Contains(metadata.Writer!, w => w.Tag == "Copy Writer");
        Assert.NotNull(metadata.Role);
        Assert.Contains(metadata.Role!, r => r.Tag == "Star Talent");
        Assert.NotNull(metadata.Genre);
        Assert.Contains(metadata.Genre!, g => g.Tag == "NBC");
        Assert.Equal($"https://i.ytimg.com/vi/{video.YoutubeId}/hqdefault.jpg", metadata.Thumb);
        Assert.Contains(metadata.GuidList!, g => g.Id == $"commercialbrainz://{sbid}");
        Assert.Contains(metadata.GuidList!, g => g.Id == $"youtube://{video.YoutubeId}");
    }

    [Fact]
    public void TryParseGuid_AcceptsProviderAndBareForms()
    {
        var id = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        Assert.True(PlexMetadataMapper.TryParseGuid($"tv.plex.agents.custom.commercialbrainz.movie://movie/{id}", out var a));
        Assert.Equal(id, a);
        Assert.True(PlexMetadataMapper.TryParseGuid($"commercialbrainz://{id}", out var b));
        Assert.Equal(id, b);
        Assert.True(PlexMetadataMapper.TryParseGuid(id.ToString(), out var c));
        Assert.Equal(id, c);
    }

    [Fact]
    public void SerializedMetadata_ContainsBothGuidCasings()
    {
        var sbid = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        var metadata = PlexMetadataMapper.ToMetadata(new VideoDetailDto
        {
            Sbid = sbid,
            CommercialId = Guid.NewGuid(),
            Commercial = new CommercialPublicDto { Title = "Test" }
        });

        var text = Newtonsoft.Json.JsonConvert.SerializeObject(
            new MediaContainerRoot
            {
                MediaContainer = new MediaContainer
                {
                    Size = 1,
                    TotalSize = 1,
                    Metadata = [metadata]
                }
            },
            new Newtonsoft.Json.JsonSerializerSettings
            {
                NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore
            });

        var obj = JObject.Parse(text);
        var item = obj["MediaContainer"]!["Metadata"]![0]!;
        Assert.NotNull(item["guid"]);
        Assert.NotNull(item["Guid"]);
        Assert.Equal($"tv.plex.agents.custom.commercialbrainz.movie://movie/{sbid}", item["guid"]!.ToString());
    }
}

public class MovieProviderEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public MovieProviderEndpointTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetMovie_ReturnsMediaProviderDefinition()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/movie");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var text = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(text);
        var provider = doc.RootElement.GetProperty("MediaProvider");
        Assert.Equal(Constants.ProviderIdentifier, provider.GetProperty("identifier").GetString());
        Assert.Equal(Constants.ProviderTitle, provider.GetProperty("title").GetString());

        var types = provider.GetProperty("Types");
        Assert.Equal(1, types.GetArrayLength());
        Assert.Equal(1, types[0].GetProperty("type").GetInt32());

        var features = provider.GetProperty("Feature");
        Assert.Equal(2, features.GetArrayLength());
    }

    [Fact]
    public async Task PostMatches_EmptyTitle_ReturnsEmptyMetadataList()
    {
        var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync("/movie/library/metadata/matches", new MatchRequest
        {
            Type = 1,
            Title = "",
            Manual = 0
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var text = await response.Content.ReadAsStringAsync();
        var payload = Newtonsoft.Json.JsonConvert.DeserializeObject<MediaContainerRoot>(text);
        Assert.NotNull(payload);
        Assert.Equal(0, payload!.MediaContainer.Size);
        Assert.Equal(Constants.ProviderIdentifier, payload.MediaContainer.Identifier);
        Assert.NotNull(payload.MediaContainer.Metadata);
        Assert.Empty(payload.MediaContainer.Metadata!);
    }

    [Fact]
    public async Task GetMetadata_UnknownKey_Returns404()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync($"/movie/library/metadata/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public void GetProviderDefinition_HasExpectedFeatureKeys()
    {
        var def = MovieRoutes.GetProviderDefinition();
        Assert.Contains(def.MediaProvider.Feature, f => f.Type == "match" && f.Key == "/library/metadata/matches");
        Assert.Contains(def.MediaProvider.Feature, f => f.Type == "metadata" && f.Key == "/library/metadata");
    }
}

public class PathResolverTests
{
    [Fact]
    public void Resolve_FindsFileUnderMediaRoot()
    {
        var root = Path.Combine(Path.GetTempPath(), "cb-plex-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            var file = Path.Combine(root, "spot.mp4");
            File.WriteAllText(file, "x");

            var options = Microsoft.Extensions.Options.Options.Create(new Configuration.ProviderOptions
            {
                MediaRoots = [root]
            });
            var resolver = new Services.PathResolver(options, Microsoft.Extensions.Logging.Abstractions.NullLogger<Services.PathResolver>.Instance);
            var resolved = resolver.Resolve("spot.mp4");
            Assert.Equal(Path.GetFullPath(file), resolved);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }
}
