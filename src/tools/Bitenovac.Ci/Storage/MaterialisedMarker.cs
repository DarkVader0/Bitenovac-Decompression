using Bitenovac.Ci.Core.Graph;

namespace Bitenovac.Ci.Storage;

/// <summary>
/// Records which fullHash a workspace's copy of a project currently holds, so a second
/// materialisation of content already sitting there can be skipped.
/// </summary>
/// <remarks>
/// <para>
/// Markers live under <c>artifacts/materialised/</c> in the workspace, deliberately:
/// </para>
/// <list type="bullet">
/// <item>
/// In the <em>workspace</em>, not the store, because the claim they make is about this checkout.
/// In CI, <c>build</c> and <c>test</c> run in separate containers with separate checkouts — a
/// marker recorded in the shared run volume would tell <c>test</c> that files are present when
/// its own checkout is empty. A workspace marker is simply absent there, so it materialises.
/// </item>
/// <item>
/// Outside <c>bin/</c> and <c>obj/</c>, so it is never swept into a store entry and cannot come
/// back as a stale claim attached to some other hash.
/// </item>
/// <item>
/// Under <c>artifacts/</c>, which is already in <c>.gitignore</c>, so it never shows up as an
/// untracked file in someone's working tree.
/// </item>
/// </list>
/// </remarks>
internal static class MaterialisedMarker
{
    /// <summary>True when the workspace already holds this project's output for this hash.</summary>
    /// <param name="repositoryRoot">The repository root.</param>
    /// <param name="project">The project to check.</param>
    /// <param name="configuration">The build configuration.</param>
    /// <param name="fullHash">The hash the workspace would need to hold.</param>
    /// <param name="projectDirectory">The project's directory, checked for actual output.</param>
    public static bool Matches(string repositoryRoot, ProjectId project, string configuration, string fullHash, string projectDirectory)
    {
        var marker = MarkerPath(repositoryRoot, project, configuration);
        if (!File.Exists(marker))
            return false;

        // The marker is a claim about the workspace, and a claim is not evidence: someone may
        // have deleted bin/ since. Cheap corroboration beats trusting it alone.
        if (!Directory.Exists(Path.Combine(projectDirectory, "bin")))
            return false;

        return File.ReadAllText(marker).Trim() == fullHash;
    }

    /// <summary>Records that the workspace now holds this project's output for this hash.</summary>
    /// <param name="repositoryRoot">The repository root.</param>
    /// <param name="project">The project just materialised or built.</param>
    /// <param name="configuration">The build configuration.</param>
    /// <param name="fullHash">The hash the workspace now holds.</param>
    public static void Write(string repositoryRoot, ProjectId project, string configuration, string fullHash)
    {
        var marker = MarkerPath(repositoryRoot, project, configuration);
        Directory.CreateDirectory(Path.GetDirectoryName(marker)!);
        File.WriteAllText(marker, fullHash);
    }

    private static string MarkerPath(string repositoryRoot, ProjectId project, string configuration) =>
        Path.Combine(repositoryRoot, "artifacts", "materialised", configuration, project.Value + ".hash");
}
