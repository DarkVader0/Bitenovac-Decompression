using System.Globalization;

namespace Bitenovac.RemoteBuildTool.Releasing;

/// <summary>
/// Provides methods for numbering release drops as <c>yyyy.MM.dd.NNN</c>.
/// </summary>
internal static class ReleaseVersion
{
    /// <summary>
    /// Returns the next unused version for the specified date.
    /// </summary>
    /// <param name="dropRoot">The directory that holds one subdirectory per release, named by its version.</param>
    /// <param name="date">The date of the release.</param>
    /// <returns>
    /// The date followed by a three-digit counter one higher than the highest already dropped on that
    /// date, starting at <c>001</c>; for example <c>2026.10.07.001</c>.
    /// </returns>
    public static string Next(string dropRoot, DateOnly date)
    {
        var prefix = date.ToString("yyyy.MM.dd", CultureInfo.InvariantCulture) + ".";

        var highest = Directory.Exists(dropRoot)
            ? Directory.EnumerateDirectories(dropRoot)
                .Select(Path.GetFileName)
                .Where(name => name is not null && name.StartsWith(prefix, StringComparison.Ordinal))
                .Select(name => int.TryParse(name![prefix.Length..], NumberStyles.None, CultureInfo.InvariantCulture, out var counter) ? counter : 0)
                .DefaultIfEmpty(0)
                .Max()
            : 0;

        return $"{prefix}{highest + 1:D3}";
    }
}
