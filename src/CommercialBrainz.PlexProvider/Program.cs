using CommercialBrainz.PlexProvider;
using CommercialBrainz.PlexProvider.Api;
using CommercialBrainz.PlexProvider.Configuration;
using CommercialBrainz.PlexProvider.Hashing;
using CommercialBrainz.PlexProvider.Routes;
using CommercialBrainz.PlexProvider.Services;

var options = ProviderOptions.FromEnvironment();

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseUrls($"http://0.0.0.0:{Environment.GetEnvironmentVariable("PORT") ?? Constants.DefaultPort.ToString()}");

builder.Services.AddSingleton(options);
builder.Services.AddSingleton(Microsoft.Extensions.Options.Options.Create(options));
builder.Services.AddHttpClient(nameof(CommercialBrainzApi));
builder.Services.AddSingleton<CommercialBrainzApi>();
builder.Services.AddSingleton<MediaHasher>();
builder.Services.AddSingleton<PathResolver>();
builder.Services.AddSingleton<MetadataLookupService>();
builder.Services.AddSingleton<MatchService>();
builder.Services.AddSingleton<MetadataService>();

var app = builder.Build();

app.MapGet("/", () => Results.Redirect("/movie"));
app.MapMovieRoutes();

var logger = app.Services.GetRequiredService<ILoggerFactory>().CreateLogger("Startup");
logger.LogInformation(
    "CommercialBrainz Plex provider listening; add in Plex as {BaseUrl}/movie (MEDIA_ROOTS={RootCount})",
    options.BaseUrl,
    options.MediaRoots.Count);

app.Run();

/// <summary>
/// Exposes the entry assembly for integration tests.
/// </summary>
public partial class Program;
