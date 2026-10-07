namespace Bitenovac.RemoteBuildTool.MsBuild;

/// <summary>
/// Provides methods for finding every MSBuild project file in a repository.
/// </summary>
internal static class ProjectDiscovery
{
    private static readonly string[] Extensions = [".csproj", ".fsproj", ".vbproj"];
    private static readonly string[] ExcludedSegments = ["bin", "obj", "artifacts"];

    /// <summary>
    /// Returns every project file under <paramref name="repositoryRoot"/>.
    /// </summary>
    /// <param name="repositoryRoot">The repository root to search.</param>
    /// <returns>
    /// The repository-relative paths of the project files, ordered ordinally. Files under a
    /// <c>bin</c>, <c>obj</c> or <c>artifacts</c> directory are excluded.
    /// </returns>
    public static IReadOnlyList<string> FindRelativePaths(string repositoryRoot)
    {
        var root = Path.GetFullPath(repositoryRoot);

        return Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories)
            .Where(path => Extensions.Contains(Path.GetExtension(path), StringComparer.OrdinalIgnoreCase))
            .Select(path => Path.GetRelativePath(root, path).Replace('\\', '/'))
            .Where(relative => !relative.Split('/').Any(segment => ExcludedSegments.Contains(segment, StringComparer.OrdinalIgnoreCase)))
            .OrderBy(relative => relative, StringComparer.Ordinal)
            .ToList();
    }
}
