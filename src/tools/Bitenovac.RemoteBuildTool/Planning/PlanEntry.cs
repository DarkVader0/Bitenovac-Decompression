namespace Bitenovac.RemoteBuildTool.Planning;

/// <summary>
/// Everything <c>build</c> and <c>test</c> need about one project, for one configuration,
/// without re-evaluating it — <c>plan</c> already did that. Persisted to
/// <see cref="PipelineOptions.PlanFile"/> so it survives across the separate process, and
/// possibly separate container, each pipeline stage runs in.
/// </summary>
/// <param name="Toolchain">The <see cref="Toolchains.IToolchain.Name"/> of the toolchain that owns the project.</param>
/// <param name="References">The repository-relative paths of the projects this one references.</param>
/// <param name="Properties">The owning toolchain's own data; see <see cref="Toolchains.EvaluatedProject.Properties"/>.</param>
internal sealed record PlanEntry(
    string ProjectPath,
    string FullPath,
    string Toolchain,
    IReadOnlyList<string> References,
    string CoverageName,
    IReadOnlyDictionary<string, string> Properties,
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
