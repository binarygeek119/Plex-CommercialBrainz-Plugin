using CommercialBrainz.PlexProvider.Configuration;
using Microsoft.Extensions.Options;

namespace CommercialBrainz.PlexProvider.Services;

/// <summary>
/// Resolves Plex relative filenames against configured media roots.
/// </summary>
public class PathResolver
{
    private readonly ProviderOptions _options;
    private readonly ILogger<PathResolver> _logger;

    public PathResolver(IOptions<ProviderOptions> options, ILogger<PathResolver> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    /// <summary>
    /// Attempts to resolve a Plex relative <paramref name="filename"/> to an absolute readable path.
    /// </summary>
    public string? Resolve(string? filename)
    {
        if (string.IsNullOrWhiteSpace(filename))
        {
            return null;
        }

        // If Plex somehow sends an absolute path that already exists, use it.
        if (Path.IsPathRooted(filename) && File.Exists(filename))
        {
            return Path.GetFullPath(filename);
        }

        var relative = filename.TrimStart('/', '\\').Replace('/', Path.DirectorySeparatorChar);
        if (_options.MediaRoots.Count == 0)
        {
            _logger.LogDebug("MEDIA_ROOTS not configured; cannot resolve {Filename} for hashing", filename);
            return null;
        }

        foreach (var root in _options.MediaRoots)
        {
            if (string.IsNullOrWhiteSpace(root))
            {
                continue;
            }

            var candidate = Path.GetFullPath(Path.Combine(root, relative));
            if (!IsUnderRoot(root, candidate))
            {
                _logger.LogWarning("Rejected path escape attempt for {Filename} under {Root}", filename, root);
                continue;
            }

            if (File.Exists(candidate))
            {
                return candidate;
            }

            // Also try just the leaf name under the root (library folder already is the root).
            var leaf = Path.GetFileName(relative);
            if (!string.IsNullOrWhiteSpace(leaf) && !string.Equals(leaf, relative, StringComparison.Ordinal))
            {
                var leafCandidate = Path.GetFullPath(Path.Combine(root, leaf));
                if (IsUnderRoot(root, leafCandidate) && File.Exists(leafCandidate))
                {
                    return leafCandidate;
                }
            }
        }

        _logger.LogDebug("Could not resolve media file {Filename} under MEDIA_ROOTS", filename);
        return null;
    }

    private static bool IsUnderRoot(string root, string candidate)
    {
        var fullRoot = Path.GetFullPath(root)
            .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
            + Path.DirectorySeparatorChar;
        var fullCandidate = Path.GetFullPath(candidate);
        return fullCandidate.StartsWith(fullRoot, StringComparison.OrdinalIgnoreCase)
            || string.Equals(
                Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar),
                Path.GetDirectoryName(fullCandidate),
                StringComparison.OrdinalIgnoreCase);
    }
}
