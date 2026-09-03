using System.Text.Json;

namespace Bitenovac.Ci.Hashing;

/// <summary>
/// Reads a project's fully resolved package closure from <c>obj/project.assets.json</c>,
/// written by <c>dotnet restore</c>. This is why <c>plan</c> restores before it hashes: with
/// <c>CentralPackageTransitivePinningEnabled</c>, a project can be affected by a package it
/// never directly references — the resolved closure is the only place that shows the real,
/// final set of package/version pairs a project compiles against.
/// </summary>
internal static class PackageClosureReader
{
    /// <summary>
    /// One <c>"package:Id/Version"</c> entry per library in the resolved closure, or empty when
    /// the project has not been restored (declares no packages at all, or restore has not run).
    /// </summary>
    public static IReadOnlyList<string> Read(string projectFullPath)
    {
        var assetsPath = Path.Combine(Path.GetDirectoryName(projectFullPath)!, "obj", "project.assets.json");
        if (!File.Exists(assetsPath))
            return [];

        using var document = JsonDocument.Parse(File.ReadAllBytes(assetsPath));
        if (!document.RootElement.TryGetProperty("libraries", out var libraries))
            return [];

        // Each key is already "Id/Version" — the resolved identity is the entry; nothing else
        // about a library needs to be read for hashing purposes.
        return libraries.EnumerateObject()
            .Select(library => $"package:{library.Name}")
            .OrderBy(entry => entry, StringComparer.Ordinal)
            .ToList();
    }
}
