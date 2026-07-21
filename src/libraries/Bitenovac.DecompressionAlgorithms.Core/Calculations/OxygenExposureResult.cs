namespace Bitenovac.DecompressionAlgorithms.Core.Calculations;

/// <summary>
/// Represents the total oxygen toxicity accrued over a dive: the central nervous system
/// toxicity, as a fraction of the single-exposure limit, and the pulmonary toxicity, in
/// oxygen tolerance units. Both are accumulated over the expanded profile and depend only
/// on the partial pressure of oxygen and the time of exposure.
/// </summary>
/// <remarks>Instances are immutable.</remarks>
public sealed class OxygenExposureResult
{
    /// <summary>Initializes a new instance of the <see cref="OxygenExposureResult" /> class.</summary>
    /// <param name="centralNervousSystemFraction">
    /// The accrued central nervous system toxicity, as a fraction of the single-exposure
    /// limit.
    /// </param>
    /// <param name="oxygenToleranceUnits">The accrued pulmonary toxicity, in oxygen tolerance units.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="centralNervousSystemFraction" /> or <paramref name="oxygenToleranceUnits" /> is negative.
    /// </exception>
    public OxygenExposureResult(double centralNervousSystemFraction, double oxygenToleranceUnits)
    {
        if (centralNervousSystemFraction < 0.0)
        {
            throw new ArgumentOutOfRangeException(nameof(centralNervousSystemFraction), centralNervousSystemFraction,
                "The central nervous system fraction must not be negative.");
        }

        if (oxygenToleranceUnits < 0.0)
        {
            throw new ArgumentOutOfRangeException(nameof(oxygenToleranceUnits), oxygenToleranceUnits,
                "The oxygen tolerance units must not be negative.");
        }

        CentralNervousSystemFraction = centralNervousSystemFraction;
        OxygenToleranceUnits = oxygenToleranceUnits;
    }

    /// <summary>Gets the accrued central nervous system toxicity, as a fraction of the single-exposure limit.</summary>
    public double CentralNervousSystemFraction { get; }

    /// <summary>Gets the accrued pulmonary toxicity, in oxygen tolerance units.</summary>
    public double OxygenToleranceUnits { get; }
}