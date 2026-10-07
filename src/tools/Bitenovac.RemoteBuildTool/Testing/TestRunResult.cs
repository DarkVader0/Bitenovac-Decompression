namespace Bitenovac.RemoteBuildTool.Testing;

/// <summary>
/// Specifies the outcome of running one test project.
/// </summary>
internal enum TestRunOutcome
{
    /// <summary>
    /// Every test passed.
    /// </summary>
    Passed,

    /// <summary>
    /// The run discovered or selected no tests, which is not a failure.
    /// </summary>
    /// <remarks>
    /// Microsoft.Testing.Platform reports this outcome with exit code 5 or 8.
    /// </remarks>
    NoTestsRan,

    /// <summary>
    /// At least one test failed.
    /// </summary>
    Failed,
}

/// <summary>
/// Represents the result of running one test project: its outcome, where its coverage report was
/// written, and what it printed.
/// </summary>
internal sealed record TestRunResult(TestRunOutcome Outcome, int ExitCode, string ResultsDirectory, string Output);
