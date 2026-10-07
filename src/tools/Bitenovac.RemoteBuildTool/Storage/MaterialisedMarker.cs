using Bitenovac.RemoteBuildTool.Core.Graph;

namespace Bitenovac.RemoteBuildTool.Storage;

/// <summary>
/// Provides methods for recording which full hash the workspace's copy of a project holds, so that
/// materializing content already in place can be skipped.
/// </summary>
/// <remarks>
/// Markers are stored in the workspace under <c>artifacts/materialised/</c>, not in a store, so
/// they describe only the current checkout. They are outside every toolchain's output directories,
/// so they are never stored in an artifact entry.
/// </remarks>
internal static class MaterialisedMarker
{
    /// <summary>
    /// Determines whether the workspace already holds the output of the specified project for the
    /// specified hash.
    /// </summary>
    /// <param name="repositoryRoot">The repository root.</param>
    /// <param name="project">The project to check.</param>
    /// <param name="configuration">The build configuration.</param>
    /// <param name="fullHash">The full hash the workspace must hold.</param>
    /// <param name="outputDirectory">The project's primary output directory, which must exist.</param>
    /// <returns>
    /// <see langword="true"/> if the marker names <paramref name="fullHash"/> and
    /// <paramref name="outputDirectory"/> exists; otherwise, <see langword="false"/>.
    /// </returns>
    public static bool Matches(string repositoryRoot, ProjectId project, string configuration, string fullHash, string outputDirectory)
    {
        var marker = MarkerPath(repositoryRoot, project, configuration);
        if (!File.Exists(marker))
            return false;

        if (!Directory.Exists(outputDirectory))
            return false;

        return File.ReadAllText(marker).Trim() == fullHash;
    }

    /// <summary>
    /// Records that the workspace now holds the output of the specified project for the specified hash.
    /// </summary>
    /// <param name="repositoryRoot">The repository root.</param>
    /// <param name="project">The project that was just materialized or built.</param>
    /// <param name="configuration">The build configuration.</param>
    /// <param name="fullHash">The full hash the workspace now holds.</param>
    public static void Write(string repositoryRoot, ProjectId project, string configuration, string fullHash)
    {
        var marker = MarkerPath(repositoryRoot, project, configuration);
        Directory.CreateDirectory(Path.GetDirectoryName(marker)!);
        File.WriteAllText(marker, fullHash);
    }

    private static string MarkerPath(string repositoryRoot, ProjectId project, string configuration) =>
        Path.Combine(repositoryRoot, "artifacts", "materialised", configuration, project.Value + ".hash");
}
