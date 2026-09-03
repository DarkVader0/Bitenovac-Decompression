namespace Bitenovac.Ci.Core.Coverage;

/// <summary>Measured coverage for one project, read from a merged cobertura report.</summary>
/// <param name="LinePercent">The measured line coverage, 0–100.</param>
/// <param name="BranchPercent">The measured branch coverage, 0–100.</param>
public readonly record struct CoverageMeasurement(double LinePercent, double BranchPercent);
