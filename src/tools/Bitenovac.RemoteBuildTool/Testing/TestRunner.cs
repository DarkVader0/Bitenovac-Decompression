using Bitenovac.RemoteBuildTool.Processes;

namespace Bitenovac.RemoteBuildTool.Testing;

/// <summary>
/// Provides methods for running one test project by starting its own executable and collecting
/// coverage.
/// </summary>
/// <remarks>
/// A Microsoft.Testing.Platform test project is compiled as an executable that hosts the test
/// platform, so the apphost that MSBuild reports as <c>RunCommand</c> is started directly.
/// </remarks>
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
