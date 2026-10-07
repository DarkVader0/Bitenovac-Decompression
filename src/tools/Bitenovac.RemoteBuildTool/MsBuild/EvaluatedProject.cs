using Bitenovac.RemoteBuildTool.Core.Graph;

namespace Bitenovac.RemoteBuildTool.MsBuild;

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
    string RunCommand,
    bool HasTargetFrameworks,
    IReadOnlyList<string> OwnHashInputs);
