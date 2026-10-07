namespace Bitenovac.RemoteBuildTool.Core.Coverage;

/// <summary>
/// Represents the line and branch coverage measured for one project.
/// </summary>
/// <param name="LinePercent">The measured line coverage, from 0 to 100.</param>
/// <param name="BranchPercent">The measured branch coverage, from 0 to 100.</param>
public readonly record struct CoverageMeasurement(double LinePercent, double BranchPercent);
