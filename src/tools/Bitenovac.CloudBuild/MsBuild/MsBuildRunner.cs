using Bitenovac.CloudBuild.Processes;

namespace Bitenovac.CloudBuild.MsBuild;

/// <summary>Restores or builds a set of projects through a <see cref="SyntheticBuildProject"/>.</summary>
internal static class MsBuildRunner
{
    public static int Restore(string repositoryRoot, IEnumerable<string> projectFullPaths, string configuration, string syntheticProjectPath)
    {
        SyntheticBuildProject.Write(syntheticProjectPath, projectFullPaths);
        return RunMsBuild(repositoryRoot, syntheticProjectPath, "Restore", configuration);
    }

    public static int Build(string repositoryRoot, IEnumerable<string> projectFullPaths, string configuration, string syntheticProjectPath)
    {
        SyntheticBuildProject.Write(syntheticProjectPath, projectFullPaths);
        return RunMsBuild(repositoryRoot, syntheticProjectPath, "Build", configuration);
    }

    private static int RunMsBuild(string repositoryRoot, string projectPath, string target, string configuration)
    {
        var maxCpuArgument = Environment.GetEnvironmentVariable("CLOUDBUILD_MAX_CPU") is { Length: > 0 } maxCpu
            ? $"-maxCpuCount:{maxCpu}"
            : "-maxCpuCount";

        return ProcessRunner.Run(
            "dotnet",
            [
                "msbuild", projectPath,
                $"-t:{target}",
                "-nologo",
                maxCpuArgument,
                $"-p:Configuration={configuration}",
            ],
            repositoryRoot);
    }
}
