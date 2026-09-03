namespace Bitenovac.Ci;

/// <summary>
/// Where this run's inputs and volumes are, read from environment variables so the same binary
/// behaves identically whether launched by <c>runner/in-container.sh</c>, by
/// <c>docker/ci-local.sh</c>, or directly for local debugging.
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
        var repositoryRoot = Environment.GetEnvironmentVariable("CI_REPO_ROOT") is { Length: > 0 } repo
            ? repo
            : Directory.GetCurrentDirectory();

        var mainStoreRoot = Environment.GetEnvironmentVariable("CI_MAIN_STORE") is { Length: > 0 } main
            ? main
            : Path.Combine(repositoryRoot, "artifacts", "ci-main");

        var prStoreRoot = Environment.GetEnvironmentVariable("CI_PR_STORE") is { Length: > 0 } pr
            ? pr
            : Path.Combine(repositoryRoot, "artifacts", "ci-pr");

        var cacheless = IsTrue(Environment.GetEnvironmentVariable("CI_CACHELESS"));
        var coverageHtml = Environment.GetEnvironmentVariable("CI_COVERAGE_HTML") != "0";

        return new PipelineOptions(repositoryRoot, mainStoreRoot, prStoreRoot, cacheless, coverageHtml);
    }

    public string PlanFile => Path.Combine(PrStoreRoot, "plan.json");

    public string SyntheticProjectPath(string configuration) =>
        Path.Combine(PrStoreRoot, $"build-{configuration}.proj");

    // Under the checkout, not the PR store: a GitHub Actions upload-artifact step reads from
    // the runner's own filesystem, and the PR store is a separate Docker volume it cannot see
    // into. Test runs in its own container per stage, so nothing here needs to survive between
    // containers the way the store's content-addressed entries do.
    public string CoverageDirectory(string configuration) =>
        Path.Combine(RepositoryRoot, "artifacts", "coverage", configuration);

    public string CoverageReportDirectory(string configuration) =>
        Path.Combine(RepositoryRoot, "artifacts", "coverage-report", configuration);

    private static bool IsTrue(string? value) =>
        string.Equals(value, "true", StringComparison.OrdinalIgnoreCase) || value == "1";
}
