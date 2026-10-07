using System.Text.Json;

namespace Bitenovac.RemoteBuildTool.Hashing;

/// <summary>
/// Provides methods for reading a project's resolved package closure from
/// <c>obj/project.assets.json</c>.
/// </summary>
/// <remarks>
/// The assets file is written by <c>dotnet restore</c>, so the project must be restored before it
/// is read.
/// </remarks>
internal static class PackageClosureReader
{
    /// <summary>
    /// Reads the resolved package closure of the specified project.
    /// </summary>
    /// <param name="projectFullPath">The full path of the project file.</param>
    /// <returns>
    /// One <c>"package:Id/Version"</c> entry per library in the closure, ordered ordinally, or an
    /// empty list if the project has no assets file.
    /// </returns>
    public static IReadOnlyList<string> Read(string projectFullPath)
    {
        var assetsPath = Path.Combine(Path.GetDirectoryName(projectFullPath)!, "obj", "project.assets.json");
        if (!File.Exists(assetsPath))
            return [];

        using var document = JsonDocument.Parse(File.ReadAllBytes(assetsPath));
        if (!document.RootElement.TryGetProperty("libraries", out var libraries))
            return [];

        return libraries.EnumerateObject()
            .Select(library => $"package:{library.Name}")
            .OrderBy(entry => entry, StringComparer.Ordinal)
            .ToList();
    }
}
