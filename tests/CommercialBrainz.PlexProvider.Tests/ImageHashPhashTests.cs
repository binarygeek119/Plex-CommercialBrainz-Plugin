using CommercialBrainz.PlexProvider.Hashing;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Xunit;

namespace CommercialBrainz.PlexProvider.Tests;

public class ImageHashPhashTests
{
    [Fact]
    public void ComputeHex_OnPilPreResizedGradient_MatchesPython()
    {
        var path = Path.Combine(TestContext.FixturesDir, "gradient_32_l.png");
        Assert.True(File.Exists(path), $"Missing fixture: {path}");

        using var image = Image.Load<Rgb24>(path);
        var hex = ImageHashPhash.ComputeHex(image);

        Assert.Equal("805f7a665bee5424", hex);
    }

    [Fact]
    public void ComputeHex_MatchesPythonImageHash_OnGradientFixture_WithinThreshold()
    {
        var path = Path.Combine(TestContext.FixturesDir, "gradient_800.png");
        Assert.True(File.Exists(path), $"Missing fixture: {path}");

        using var image = Image.Load<Rgb24>(path);
        var hex = ImageHashPhash.ComputeHex(image);
        var distance = ImageHashPhash.HammingDistance(hex, "805f7a665bee5424");

        Assert.True(distance <= Constants.DefaultPhashThreshold, $"Hamming distance {distance} for hash {hex}");
    }

    [Fact]
    public void ComputeHex_MatchesPythonImageHash_OnSolidFixture()
    {
        var path = Path.Combine(TestContext.FixturesDir, "solid_64.png");
        Assert.True(File.Exists(path), $"Missing fixture: {path}");

        using var image = Image.Load<Rgb24>(path);
        var hex = ImageHashPhash.ComputeHex(image);

        Assert.Equal("8000000000000000", hex);
    }

    [Fact]
    public void ComputeHex_SolidInMemory_MatchesPython()
    {
        using var image = new Image<Rgb24>(64, 64);
        for (var y = 0; y < 64; y++)
        {
            for (var x = 0; x < 64; x++)
            {
                image[x, y] = new Rgb24(128, 64, 32);
            }
        }

        var info = ImageHashPhash.Describe(image);
        Assert.True(info.IsFlat, $"expected flat luma, got min={info.MinLuma} max={info.MaxLuma} first={info.FirstLuma}");
        Assert.Equal(79, info.FirstLuma);

        var hex = ImageHashPhash.ComputeHex(image);
        Assert.Equal("8000000000000000", hex);
    }

    [Fact]
    public void HammingDistance_Identical_IsZero()
    {
        Assert.Equal(0, ImageHashPhash.HammingDistance("805f7a665bee5424", "805f7a665bee5424"));
    }

    [Fact]
    public async Task ComputeHexFromFileAsync_Works()
    {
        var path = Path.Combine(TestContext.FixturesDir, "solid_64.png");
        var hex = await ImageHashPhash.ComputeHexFromFileAsync(path, default);
        Assert.Equal("8000000000000000", hex);
    }
}

internal static class TestContext
{
    public static string FixturesDir
    {
        get
        {
            var dir = Path.GetDirectoryName(typeof(ImageHashPhashTests).Assembly.Location)!;
            return Path.Combine(dir, "Fixtures");
        }
    }
}
