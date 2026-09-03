namespace Bitenovac.Ci.MsBuild;

/// <summary>Finds every MSBuild project file in the repository, mirroring the old <c>discover_projects</c> shell function.</summary>
internal static class ProjectDiscovery
{
    private static readonly string[] Extensions = [".csproj", ".fsproj", ".vbproj"];
    private static readonly string[] ExcludedSegments = ["bin", "obj", "artifacts"];

    /// <summary>Every project file under <paramref name="repositoryRoot"/>, repository-relative and sorted.</summary>
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
