using Bitenovac.RemoteBuildTool.Processes;

namespace Bitenovac.RemoteBuildTool.MsBuild;

/// <summary>
/// Provides methods for restoring or building a set of projects through a
/// <see cref="SyntheticSolution"/>.
/// </summary>
internal static class MsBuildRunner
{
    public static int Restore(string repositoryRoot, IEnumerable<string> projectFullPaths, string configuration, string solutionPath)
    {
        SyntheticSolution.Write(solutionPath, projectFullPaths);
        return RunMsBuild(repositoryRoot, solutionPath, "Restore", configuration, graph: false);
    }

    public static int Build(string repositoryRoot, IEnumerable<string> projectFullPaths, string configuration, string solutionPath)
    {
        SyntheticSolution.Write(solutionPath, projectFullPaths);
        return RunMsBuild(repositoryRoot, solutionPath, "Build", configuration, graph: true);
    }

    private static int RunMsBuild(string repositoryRoot, string solutionPath, string target, string configuration, bool graph)
    {
        List<string> arguments =
        [
            "msbuild", solutionPath,
            $"-t:{target}",
            "-nologo",
            $"-maxCpuCount:{PipelineOptions.MaxParallelism()}",
            $"-p:Configuration={configuration}",
        ];

        if (graph)
            arguments.Add("-graphBuild");

        return ProcessRunner.Run("dotnet", arguments, repositoryRoot);
    }
}
