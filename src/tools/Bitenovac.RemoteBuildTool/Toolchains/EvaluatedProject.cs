using Bitenovac.RemoteBuildTool.Core.Graph;

namespace Bitenovac.RemoteBuildTool.Toolchains;

/// <summary>Everything the pipeline needs from one project's evaluation, for one configuration.</summary>
/// <param name="Toolchain">The <see cref="IToolchain.Name"/> of the toolchain that owns the project.</param>
/// <param name="CoverageName">The name the project's code is reported under in cobertura reports.</param>
/// <param name="Properties">
/// Whatever the owning toolchain needs again after <c>plan</c>, carried through the plan file.
/// Nothing outside that toolchain reads it.
/// </param>
internal sealed record EvaluatedProject(
    ProjectId Id,
    string FullPath,
    string Toolchain,
    IReadOnlyList<ProjectId> ProjectReferences,
    bool IsTestProject,
    bool ExcludeFromCoverage,
    double MinimumLineCoverage,
    double MinimumBranchCoverage,
    bool CacheTestResults,
    string CoverageName,
    IReadOnlyDictionary<string, string> Properties,
    IReadOnlyList<string> OwnHashInputs);
