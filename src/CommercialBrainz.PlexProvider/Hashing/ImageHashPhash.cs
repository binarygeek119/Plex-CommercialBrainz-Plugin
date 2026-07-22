using System.Globalization;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace CommercialBrainz.PlexProvider.Hashing;

/// <summary>
/// Python imagehash.phash–compatible perceptual hash (DCT, median threshold).
/// Matches JohannesBuchner imagehash used by CommercialBrainz.
/// </summary>
public static class ImageHashPhash
{
    private const int HashSize = 8;
    private const int HighFreqFactor = 4;

    /// <summary>
    /// Computes a 64-bit pHash as a lowercase 16-character hex string.
    /// </summary>
    public static string ComputeHex(Image<Rgb24> image)
    {
        return Compute(image).ToString("x16", CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Computes a 64-bit pHash as an unsigned long.
    /// </summary>
    public static ulong Compute(Image<Rgb24> image)
    {
        ArgumentNullException.ThrowIfNull(image);

        var imgSize = HashSize * HighFreqFactor;
        var srcW = image.Width;
        var srcH = image.Height;

        using var grayImg = new Image<L8>(srcW, srcH);
        for (var y = 0; y < srcH; y++)
        {
            for (var x = 0; x < srcW; x++)
            {
                var p = image[x, y];
                // Pillow convert("L") integer form
                var luma = ((p.R * 299) + (p.G * 587) + (p.B * 114) + 500) / 1000;
                grayImg[x, y] = new L8((byte)Math.Clamp(luma, 0, 255));
            }
        }

        if (srcW != imgSize || srcH != imgSize)
        {
            grayImg.Mutate(ctx => ctx.Resize(imgSize, imgSize, KnownResamplers.Lanczos3));
        }

        var resized = new double[imgSize, imgSize];
        for (var y = 0; y < imgSize; y++)
        {
            for (var x = 0; x < imgSize; x++)
            {
                resized[y, x] = grayImg[x, y].PackedValue;
            }
        }

        var dct = Dct2D(resized, imgSize);

        var lowFreq = new double[HashSize * HashSize];
        var index = 0;
        for (var y = 0; y < HashSize; y++)
        {
            for (var x = 0; x < HashSize; x++)
            {
                var value = dct[y, x];
                // Suppress float noise so constant images match imagehash (AC bins == 0).
                if (Math.Abs(value) < 1e-6)
                {
                    value = 0;
                }

                lowFreq[index++] = value;
            }
        }

        var median = Median(lowFreq);
        ulong hash = 0;
        for (var i = 0; i < lowFreq.Length; i++)
        {
            if (lowFreq[i] > median)
            {
                hash |= 1UL << (lowFreq.Length - 1 - i);
            }
        }

        return hash;
    }

    /// <summary>
    /// Diagnostic snapshot of luma statistics for tests.
    /// </summary>
    public readonly record struct LumaStats(int FirstLuma, int MinLuma, int MaxLuma, bool IsFlat);

    /// <summary>
    /// Describes luma conversion for debugging hash mismatches.
    /// </summary>
    public static LumaStats Describe(Image<Rgb24> image)
    {
        var first = -1;
        var min = int.MaxValue;
        var max = int.MinValue;
        for (var y = 0; y < image.Height; y++)
        {
            for (var x = 0; x < image.Width; x++)
            {
                var p = image[x, y];
                var luma = ((p.R * 299) + (p.G * 587) + (p.B * 114) + 500) / 1000;
                if (first < 0)
                {
                    first = luma;
                }

                if (luma < min)
                {
                    min = luma;
                }

                if (luma > max)
                {
                    max = luma;
                }
            }
        }

        return new LumaStats(first, min, max, min == max);
    }

    /// <summary>
    /// Loads an image from a file and computes its pHash hex string.
    /// </summary>
    public static async Task<string> ComputeHexFromFileAsync(string path, CancellationToken cancellationToken)
    {
        await using var stream = File.OpenRead(path);
        using var image = await Image.LoadAsync<Rgb24>(stream, cancellationToken).ConfigureAwait(false);
        return ComputeHex(image);
    }

    /// <summary>
    /// Hamming distance between two hex pHash strings.
    /// </summary>
    public static int HammingDistance(string a, string b)
    {
        return System.Numerics.BitOperations.PopCount(ParseHex(a) ^ ParseHex(b));
    }

    private static ulong ParseHex(string value)
    {
        var text = value.Trim().ToLowerInvariant();
        if (text.StartsWith("0x", StringComparison.Ordinal))
        {
            text = text[2..];
        }

        return ulong.Parse(text, NumberStyles.HexNumber, CultureInfo.InvariantCulture);
    }

    private static double[,] Dct2D(double[,] input, int size)
    {
        var rowDct = new double[size, size];
        var temp = new double[size];
        var outRow = new double[size];

        for (var y = 0; y < size; y++)
        {
            for (var x = 0; x < size; x++)
            {
                temp[x] = input[y, x];
            }

            Dct1DType2(temp, outRow);
            for (var x = 0; x < size; x++)
            {
                rowDct[y, x] = outRow[x];
            }
        }

        var result = new double[size, size];
        for (var x = 0; x < size; x++)
        {
            for (var y = 0; y < size; y++)
            {
                temp[y] = rowDct[y, x];
            }

            Dct1DType2(temp, outRow);
            for (var y = 0; y < size; y++)
            {
                result[y, x] = outRow[y];
            }
        }

        return result;
    }

    private static void Dct1DType2(double[] input, double[] output)
    {
        var n = input.Length;
        for (var k = 0; k < n; k++)
        {
            double sum = 0;
            for (var i = 0; i < n; i++)
            {
                sum += input[i] * Math.Cos(Math.PI * k * ((2.0 * i) + 1.0) / (2.0 * n));
            }

            output[k] = 2.0 * sum;
        }
    }

    private static double Median(double[] values)
    {
        var copy = (double[])values.Clone();
        Array.Sort(copy);
        var mid = copy.Length / 2;
        if (copy.Length % 2 == 0)
        {
            return (copy[mid - 1] + copy[mid]) / 2.0;
        }

        return copy[mid];
    }
}
