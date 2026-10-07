namespace Bitenovac.RemoteBuildTool;

/// <summary>
/// Represents the locations of this run's inputs and artifact volumes, and the switches that
/// change its behavior.
/// </summary>
internal sealed record PipelineOptions(
    string RepositoryRoot,
    string MainStoreRoot,
    string PrStoreRoot,
    bool Cacheless,
    bool CoverageHtml)
{
    public static readonly string[] Configurations = ["Debug", "Release"];

    public static PipelineOptions FromEnvironment()
    {
        var repositoryRoot = Environment.GetEnvironmentVariable("REMOTEBUILDTOOL_REPO_ROOT") is { Length: > 0 } repo
            ? repo
            : Directory.GetCurrentDirectory();

        var mainStoreRoot = Environment.GetEnvironmentVariable("REMOTEBUILDTOOL_MAIN_STORE") is { Length: > 0 } main
            ? main
            : Path.Combine(repositoryRoot, "artifacts", "ci-main");

        var prStoreRoot = Environment.GetEnvironmentVariable("REMOTEBUILDTOOL_PR_STORE") is { Length: > 0 } pr
            ? pr
            : Path.Combine(repositoryRoot, "artifacts", "ci-pr");

        var cacheless = IsTrue(Environment.GetEnvironmentVariable("REMOTEBUILDTOOL_CACHELESS"));
        var coverageHtml = Environment.GetEnvironmentVariable("REMOTEBUILDTOOL_COVERAGE_HTML") != "0";

        return new PipelineOptions(repositoryRoot, mainStoreRoot, prStoreRoot, cacheless, coverageHtml);
    }

    public string PlanFile => Path.Combine(PrStoreRoot, "plan.json");

    public string SyntheticSolutionPath(string stage) =>
        Path.Combine(PrStoreRoot, $"{stage}.slnx");

    public string RestoreOutputsRoot => Path.Combine(PrStoreRoot, "restore");

    /// <summary>
    /// Returns the number of processes and MSBuild nodes that one job may run at once.
    /// </summary>
    /// <returns>
    /// The value of <c>REMOTEBUILDTOOL_MAX_CPU</c> if it is a positive integer; otherwise, the
    /// number of processors.
    /// </returns>
    public static int MaxParallelism() =>
        int.TryParse(Environment.GetEnvironmentVariable("REMOTEBUILDTOOL_MAX_CPU"), out var maxCpu) && maxCpu > 0
            ? maxCpu
            : Environment.ProcessorCount;

    public string CoverageDirectory(string configuration) =>
        Path.Combine(RepositoryRoot, "artifacts", "coverage", configuration);

    public string CoverageReportDirectory(string configuration) =>
        Path.Combine(RepositoryRoot, "artifacts", "coverage-report", configuration);

    private static bool IsTrue(string? value) =>
        string.Equals(value, "true", StringComparison.OrdinalIgnoreCase) || value == "1";
}
