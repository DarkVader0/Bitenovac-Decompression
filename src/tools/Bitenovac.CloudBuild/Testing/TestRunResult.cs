namespace Bitenovac.CloudBuild.Testing;

/// <summary>The outcome of running one test assembly.</summary>
internal enum TestRunOutcome
{
    /// <summary>Every test passed.</summary>
    Passed,

    /// <summary>The run discovered or selected no tests — Microsoft.Testing.Platform exit code 5 or 8. Not a failure.</summary>
    NoTestsRan,

    /// <summary>At least one test failed.</summary>
    Failed,
}

/// <summary>The result of one test project's run, and where its coverage report landed.</summary>
internal sealed record TestRunResult(TestRunOutcome Outcome, int ExitCode, string ResultsDirectory);
