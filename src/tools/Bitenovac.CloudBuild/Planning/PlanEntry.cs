namespace Bitenovac.CloudBuild.Planning;

/// <summary>
/// Everything <c>build</c> and <c>test</c> need about one project, for one configuration,
/// without re-evaluating MSBuild — <c>plan</c> already did that. Persisted to
/// <see cref="PipelineOptions.PlanFile"/> so it survives across the separate process, and
/// possibly separate container, each pipeline stage runs in.
/// </summary>
internal sealed record PlanEntry(
    string ProjectPath,
    string FullPath,
    string AssemblyName,
    bool IsTestProject,
    bool ExcludeFromCoverage,
    double MinimumLineCoverage,
    double MinimumBranchCoverage,
    bool CacheTestResults,
    string OwnHash,
    string FullHash,
    bool Forced,
    bool Hit,
    bool ShouldGateCoverage);
