using Bitenovac.CloudBuild.Core.Graph;

namespace Bitenovac.CloudBuild.MsBuild;

/// <summary>Everything the pipeline needs from one project's MSBuild evaluation, for one configuration.</summary>
internal sealed record EvaluatedProject(
    ProjectId Id,
    string FullPath,
    IReadOnlyList<ProjectId> ProjectReferences,
    bool IsTestProject,
    bool ExcludeFromCoverage,
    double MinimumLineCoverage,
    double MinimumBranchCoverage,
    bool CacheTestResults,
    string TargetPath,
    string AssemblyName,
    bool HasTargetFrameworks,
    IReadOnlyList<string> OwnHashInputs);
