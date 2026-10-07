using Bitenovac.RemoteBuildTool.Processes;

namespace Bitenovac.RemoteBuildTool.MsBuild;

/// <summary>
/// Provides methods for restoring or building a set of projects through a
/// <see cref="SyntheticSolution"/>, and for packing or publishing one built project.
/// </summary>
internal static class MsBuildRunner
{
    public static int Restore(string repositoryRoot, IEnumerable<string> projectFullPaths, string configuration, string solutionPath)
    {
        SyntheticSolution.Write(solutionPath, projectFullPaths);
        return RunMsBuild(repositoryRoot, solutionPath, "Restore", configuration, graph: false, new Dictionary<string, string>());
    }

    public static int Build(
        string repositoryRoot,
        IEnumerable<string> projectFullPaths,
        string configuration,
        string solutionPath,
        IReadOnlyDictionary<string, string> properties)
    {
        SyntheticSolution.Write(solutionPath, projectFullPaths);
        return RunMsBuild(repositoryRoot, solutionPath, "Build", configuration, graph: true, properties);
    }

    /// <summary>
    /// Packs a project already built in Release into <paramref name="outputDirectory"/>.
    /// </summary>
    /// <returns>The process exit code.</returns>
    public static int Pack(string repositoryRoot, string projectFullPath, IReadOnlyDictionary<string, string> properties, string outputDirectory) =>
        RunDotNet(repositoryRoot, "pack", projectFullPath, properties, outputDirectory);

    /// <summary>
    /// Publishes a project already built in Release into <paramref name="outputDirectory"/>.
    /// </summary>
    /// <returns>The process exit code.</returns>
    public static int Publish(string repositoryRoot, string projectFullPath, IReadOnlyDictionary<string, string> properties, string outputDirectory) =>
        RunDotNet(repositoryRoot, "publish", projectFullPath, properties, outputDirectory);

    private static int RunDotNet(
        string repositoryRoot,
        string command,
        string projectFullPath,
        IReadOnlyDictionary<string, string> properties,
        string outputDirectory)
    {
        List<string> arguments =
        [
            command, projectFullPath,
            "--configuration", "Release",
            "--no-build",
            "--nologo",
            "--output", outputDirectory,
            .. properties.Select(property => $"-p:{property.Key}={property.Value}"),
        ];

        return ProcessRunner.Run("dotnet", arguments, repositoryRoot);
    }

    private static int RunMsBuild(
        string repositoryRoot,
        string solutionPath,
        string target,
        string configuration,
        bool graph,
        IReadOnlyDictionary<string, string> properties)
    {
        List<string> arguments =
        [
            "msbuild", solutionPath,
            $"-t:{target}",
            "-nologo",
            $"-maxCpuCount:{PipelineOptions.MaxParallelism()}",
            $"-p:Configuration={configuration}",
            .. properties.Select(property => $"-p:{property.Key}={property.Value}"),
        ];

        if (graph)
            arguments.Add("-graphBuild");

        return ProcessRunner.Run("dotnet", arguments, repositoryRoot);
    }
}
