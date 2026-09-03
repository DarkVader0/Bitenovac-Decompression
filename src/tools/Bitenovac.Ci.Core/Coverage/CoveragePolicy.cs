namespace Bitenovac.Ci.Core.Coverage;

/// <summary>The minimum line and branch coverage a project must meet, read from its build properties.</summary>
/// <param name="MinimumLinePercent">The minimum acceptable line coverage, 0–100.</param>
/// <param name="MinimumBranchPercent">The minimum acceptable branch coverage, 0–100.</param>
public readonly record struct CoveragePolicy(double MinimumLinePercent, double MinimumBranchPercent);
