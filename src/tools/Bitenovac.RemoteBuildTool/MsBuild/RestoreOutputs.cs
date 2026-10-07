namespace Bitenovac.RemoteBuildTool.MsBuild;

/// <summary>
/// Provides methods for carrying what restore wrote into each project's <c>obj/</c> directory from
/// <c>plan</c> to the later stages, which start from a clean checkout.
/// </summary>
internal static class RestoreOutputs
{
    private const string AssetsFileName = "project.assets.json";

    /// <summary>
    /// Copies the top level of every project's <c>obj/</c> directory under <paramref name="root"/>.
    /// </summary>
    public static void Save(string repositoryRoot, IEnumerable<string> projectFullPaths, string root)
    {
        foreach (var projectFullPath in projectFullPaths)
        {
            var objDirectory = ObjDirectory(projectFullPath);
            if (!Directory.Exists(objDirectory))
                continue;

            var destination = SavedDirectory(repositoryRoot, projectFullPath, root);
            Directory.CreateDirectory(destination);

            foreach (var file in Directory.EnumerateFiles(objDirectory))
                File.Copy(file, Path.Combine(destination, Path.GetFileName(file)), overwrite: true);
        }
    }

    /// <summary>
    /// Copies back what <see cref="Save"/> kept for a project whose <c>obj/</c> directory holds no
    /// restore output.
    /// </summary>
    /// <returns>
    /// <see langword="true"/> if the project is restored afterwards; otherwise, <see langword="false"/>.
    /// </returns>
    public static bool Materialise(string repositoryRoot, string projectFullPath, string root)
    {
        var objDirectory = ObjDirectory(projectFullPath);
        if (File.Exists(Path.Combine(objDirectory, AssetsFileName)))
            return true;

        var saved = SavedDirectory(repositoryRoot, projectFullPath, root);
        if (!File.Exists(Path.Combine(saved, AssetsFileName)))
            return false;

        Directory.CreateDirectory(objDirectory);
        foreach (var file in Directory.EnumerateFiles(saved))
            File.Copy(file, Path.Combine(objDirectory, Path.GetFileName(file)), overwrite: true);

        return true;
    }

    private static string ObjDirectory(string projectFullPath) =>
        Path.Combine(Path.GetDirectoryName(projectFullPath)!, "obj");

    private static string SavedDirectory(string repositoryRoot, string projectFullPath, string root) =>
        Path.Combine(root, Path.GetRelativePath(repositoryRoot, Path.GetDirectoryName(projectFullPath)!));
}
