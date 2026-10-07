namespace Bitenovac.RemoteBuildTool.Core.Coverage;

/// <summary>
/// Represents the minimum line and branch coverage a project must meet.
/// </summary>
/// <param name="MinimumLinePercent">The minimum acceptable line coverage, from 0 to 100.</param>
/// <param name="MinimumBranchPercent">The minimum acceptable branch coverage, from 0 to 100.</param>
public readonly record struct CoveragePolicy(double MinimumLinePercent, double MinimumBranchPercent);
