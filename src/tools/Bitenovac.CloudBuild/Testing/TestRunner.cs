using Bitenovac.CloudBuild.Processes;

namespace Bitenovac.CloudBuild.Testing;

/// <summary>
/// Runs one test assembly directly via <c>dotnet exec</c>, collecting coverage. Not
/// <c>dotnet test</c>: Microsoft.Testing.Platform does not discover through it reliably, and it
/// does not forward the <c>--coverlet</c> extension arguments coverlet.MTP needs — confirmed
/// against this repository, where <c>dotnet test</c> against a built assembly reports "zero
/// tests ran" while <c>dotnet exec</c> against the same assembly runs them.
/// </summary>
internal static class TestRunner
{
    public static TestRunResult Run(string assemblyPath, string projectName, string resultsDirectory)
    {
        Directory.CreateDirectory(resultsDirectory);

        var exitCode = ProcessRunner.Run(
            "dotnet",
            [
                "exec", assemblyPath,
                "--coverlet",
                "--coverlet-output-format", "cobertura",
                "--coverlet-file-prefix", projectName,
                "--results-directory", resultsDirectory,
            ],
            Path.GetDirectoryName(assemblyPath)!);

        var outcome = exitCode switch
        {
            0 => TestRunOutcome.Passed,
            5 or 8 => TestRunOutcome.NoTestsRan,
            _ => TestRunOutcome.Failed,
        };

        return new TestRunResult(outcome, exitCode, resultsDirectory);
    }
}
