using Bitenovac.RemoteBuildTool.Processes;

namespace Bitenovac.RemoteBuildTool.Testing;

/// <summary>
/// Runs one test project by starting its own executable, collecting coverage. A
/// Microsoft.Testing.Platform test project is compiled as an executable that hosts the test
/// platform itself, so the apphost MSBuild reports as <c>RunCommand</c> is the runner — there is
/// nothing to go through <c>dotnet test</c> or <c>dotnet exec</c> for.
/// </summary>
internal static class TestRunner
{
    private const UnixFileMode ExecuteBits = UnixFileMode.UserExecute | UnixFileMode.GroupExecute | UnixFileMode.OtherExecute;

    public static TestRunResult Run(string executablePath, string projectName, string resultsDirectory)
    {
        Directory.CreateDirectory(resultsDirectory);
        EnsureExecutable(executablePath);

        var (exitCode, output) = ProcessRunner.RunCaptured(
            executablePath,
            [
                "--coverlet",
                "--coverlet-output-format", "cobertura",
                "--coverlet-file-prefix", projectName,
                "--results-directory", resultsDirectory,
            ],
            Path.GetDirectoryName(executablePath)!);

        var outcome = exitCode switch
        {
            0 => TestRunOutcome.Passed,
            5 or 8 => TestRunOutcome.NoTestsRan,
            _ => TestRunOutcome.Failed,
        };

        return new TestRunResult(outcome, exitCode, resultsDirectory, output);
    }

    // The artifact store keeps content, not file modes, so an apphost materialised from it comes
    // back without its execute bit.
    private static void EnsureExecutable(string path)
    {
        if (OperatingSystem.IsWindows())
            return;

        var mode = File.GetUnixFileMode(path);
        if ((mode & ExecuteBits) != ExecuteBits)
            File.SetUnixFileMode(path, mode | ExecuteBits);
    }
}
