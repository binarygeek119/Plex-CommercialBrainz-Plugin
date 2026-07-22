using System.Diagnostics;
using System.Globalization;
using CommercialBrainz.PlexProvider.Configuration;
using Microsoft.Extensions.Options;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace CommercialBrainz.PlexProvider.Hashing;

/// <summary>
/// Computes CommercialBrainz-compatible media hashes for local video files.
/// </summary>
public class MediaHasher
{
    private const int ScreenshotSize = 160;
    private const int Columns = 5;
    private const int Rows = 5;
    private const int ChunkCount = Columns * Rows;

    private readonly ProviderOptions _options;
    private readonly ILogger<MediaHasher> _logger;

    public MediaHasher(IOptions<ProviderOptions> options, ILogger<MediaHasher> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    /// <summary>
    /// Computes the SHA-256 hex digest of a file.
    /// </summary>
    public async Task<string> ComputeFileSha256Async(string path, CancellationToken cancellationToken)
    {
        await using var stream = File.OpenRead(path);
        var hash = await System.Security.Cryptography.SHA256.HashDataAsync(stream, cancellationToken).ConfigureAwait(false);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    /// <summary>
    /// Attempts to compute a Chromaprint fingerprint via fpcalc.
    /// </summary>
    public async Task<string?> TryComputeAudioFingerprintAsync(string path, CancellationToken cancellationToken)
    {
        try
        {
            using var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = _options.FpcalcPath,
                    Arguments = $"-json \"{path}\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            process.Start();
            var stdout = await process.StandardOutput.ReadToEndAsync(cancellationToken).ConfigureAwait(false);
            await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);

            if (process.ExitCode != 0 || string.IsNullOrWhiteSpace(stdout))
            {
                return null;
            }

            using var doc = System.Text.Json.JsonDocument.Parse(stdout);
            if (doc.RootElement.TryGetProperty("fingerprint", out var fp) && fp.ValueKind == System.Text.Json.JsonValueKind.String)
            {
                return fp.GetString();
            }
        }
        catch (Exception ex) when (ex is InvalidOperationException or System.ComponentModel.Win32Exception or IOException)
        {
            _logger.LogDebug(ex, "fpcalc not available or failed for {Path}", path);
        }

        return null;
    }

    /// <summary>
    /// Computes the Stash-style video pHash used by CommercialBrainz.
    /// </summary>
    public async Task<string?> TryComputeVideoPhashAsync(string path, CancellationToken cancellationToken)
    {
        try
        {
            var duration = await ProbeDurationSecondsAsync(path, cancellationToken).ConfigureAwait(false);
            if (duration <= 0)
            {
                _logger.LogWarning("Could not determine duration for pHash: {Path}", path);
                return null;
            }

            using var sprite = await GenerateSpriteAsync(path, duration, cancellationToken).ConfigureAwait(false);
            return ImageHashPhash.ComputeHex(sprite);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to compute video pHash for {Path}", path);
            return null;
        }
    }

    private async Task<double> ProbeDurationSecondsAsync(string path, CancellationToken cancellationToken)
    {
        var args = $"-v error -show_entries format=duration -of default=noprint_wrappers=1:nokey=1 \"{path}\"";
        var output = await RunProcessAsync(_options.FfprobePath, args, cancellationToken).ConfigureAwait(false);
        if (double.TryParse(output.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out var seconds))
        {
            return seconds;
        }

        return 0;
    }

    private async Task<Image<Rgb24>> GenerateSpriteAsync(string path, double durationSec, CancellationToken cancellationToken)
    {
        var offset = 0.05 * durationSec;
        var stepSize = (0.9 * durationSec) / ChunkCount;
        var images = new List<Image<Rgb24>>(ChunkCount);
        var slowSeek = false;

        try
        {
            for (var i = 0; i < ChunkCount; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var timestamp = offset + (i * stepSize);
                Image<Rgb24>? frame = null;
                try
                {
                    frame = await CaptureFrameAsync(path, timestamp, slowSeek, cancellationToken).ConfigureAwait(false);
                }
                catch (Exception ex) when (!slowSeek)
                {
                    _logger.LogDebug(ex, "Fast seek failed at {Timestamp}s, retrying with accurate seek", timestamp);
                    slowSeek = true;
                    frame = await CaptureFrameAsync(path, timestamp, slowSeek, cancellationToken).ConfigureAwait(false);
                }

                if (frame is not null)
                {
                    images.Add(frame);
                }
            }

            if (images.Count == 0)
            {
                throw new InvalidOperationException($"Failed to generate pHash sprite for {path}");
            }

            return CombineImages(images);
        }
        finally
        {
            foreach (var img in images)
            {
                img.Dispose();
            }
        }
    }

    private async Task<Image<Rgb24>> CaptureFrameAsync(string path, double timestamp, bool slowSeek, CancellationToken cancellationToken)
    {
        var seekArg = timestamp.ToString("0.###", CultureInfo.InvariantCulture);
        var args = slowSeek
            ? $"-hide_banner -loglevel error -ss {seekArg} -i \"{path}\" -frames:v 1 -vf scale={ScreenshotSize}:{ScreenshotSize} -f image2pipe -vcodec png pipe:1"
            : $"-hide_banner -loglevel error -i \"{path}\" -ss {seekArg} -frames:v 1 -vf scale={ScreenshotSize}:{ScreenshotSize} -f image2pipe -vcodec png pipe:1";

        var bytes = await RunProcessBytesAsync(_options.FfmpegPath, args, cancellationToken).ConfigureAwait(false);
        if (bytes.Length == 0)
        {
            throw new InvalidOperationException("ffmpeg produced no frame output");
        }

        return Image.Load<Rgb24>(bytes);
    }

    private static Image<Rgb24> CombineImages(IReadOnlyList<Image<Rgb24>> images)
    {
        var width = images[0].Width;
        var height = images[0].Height;
        var canvas = new Image<Rgb24>(width * Columns, height * Rows);

        for (var index = 0; index < images.Count; index++)
        {
            var x = width * (index % Columns);
            var y = height * (index / Columns);
            canvas.Mutate(ctx => ctx.DrawImage(images[index], new Point(x, y), 1f));
        }

        return canvas;
    }

    private static async Task<string> RunProcessAsync(string fileName, string arguments, CancellationToken cancellationToken)
    {
        using var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };

        process.Start();
        var stdout = await process.StandardOutput.ReadToEndAsync(cancellationToken).ConfigureAwait(false);
        var stderr = await process.StandardError.ReadToEndAsync(cancellationToken).ConfigureAwait(false);
        await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);

        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException($"{fileName} failed ({process.ExitCode}): {stderr}");
        }

        return stdout;
    }

    private static async Task<byte[]> RunProcessBytesAsync(string fileName, string arguments, CancellationToken cancellationToken)
    {
        using var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };

        process.Start();
        await using var ms = new MemoryStream();
        await process.StandardOutput.BaseStream.CopyToAsync(ms, cancellationToken).ConfigureAwait(false);
        var stderr = await process.StandardError.ReadToEndAsync(cancellationToken).ConfigureAwait(false);
        await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);

        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException($"{fileName} failed ({process.ExitCode}): {stderr}");
        }

        return ms.ToArray();
    }
}
