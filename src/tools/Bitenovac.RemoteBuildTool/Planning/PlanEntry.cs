namespace Bitenovac.RemoteBuildTool.Planning;

/// <summary>
/// Represents everything <c>build</c> and <c>test</c> need to know about one project in one
/// configuration, as decided by <c>plan</c>.
/// </summary>
/// <remarks>
/// Entries are persisted to <see cref="PipelineOptions.PlanFile"/>, so they survive across the
/// separate processes that the pipeline stages run in.
/// </remarks>
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
