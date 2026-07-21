using Bitenovac.DecompressionAlgorithms.Units;

namespace Bitenovac.DecompressionAlgorithms.Core.Gas;

/// <summary>
/// Provides calculations of oxygen toxicity exposure, being the central nervous system
/// (CNS) toxicity as a percentage of the recommended single-exposure limit and the
/// pulmonary oxygen toxicity expressed in oxygen tolerance units (OTU). Both quantities
/// depend only on the partial pressure of oxygen and the time of exposure, and so are
/// independent of the decompression model in use.
/// </summary>
public static class OxygenToxicity
{
    /// <summary>
    /// The partial pressure of oxygen, in bar, below which no central nervous system
    /// toxicity is accrued.
    /// </summary>
    private const double CnsThresholdBar = 0.5;

    /// <summary>The exponent applied in the pulmonary oxygen toxicity (OTU) formula.</summary>
    private const double OtuExponent = -0.83;

    /// <summary>
    /// Returns the central nervous system oxygen toxicity accrued by breathing a gas at a
    /// given partial pressure of oxygen for a given time, expressed as a fraction of the
    /// recommended single-exposure limit.
    /// </summary>
    /// <param name="partialPressureOfOxygen">The partial pressure of oxygen breathed.</param>
    /// <param name="duration">The length of time for which the gas is breathed.</param>
    /// <returns>
    /// The fraction of the single-exposure central nervous system limit accrued, where a
    /// value of one represents one hundred percent of the limit. A partial pressure at or
    /// below the toxicity threshold accrues no exposure and returns zero.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="duration" /> is negative.</exception>
    public static double CentralNervousSystemFraction(Pressure partialPressureOfOxygen, TimeSpan duration)
    {
        if (duration < TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(duration), duration,
                "The duration must not be negative.");

        var po2Bar = partialPressureOfOxygen.InBar;
        if (po2Bar <= CnsThresholdBar)
            return 0.0;

        // The single-exposure time limit, in minutes, is a function of the partial
        // pressure of oxygen. The accrued fraction is the exposure time divided by that
        // limit.
        var limitMinutes = SingleExposureLimitMinutes(po2Bar);
        return duration.TotalMinutes / limitMinutes;
    }

    /// <summary>
    /// Returns the pulmonary oxygen toxicity accrued by breathing a gas at a given partial
    /// pressure of oxygen for a given time, expressed in oxygen tolerance units (OTU).
    /// </summary>
    /// <param name="partialPressureOfOxygen">The partial pressure of oxygen breathed.</param>
    /// <param name="duration">The length of time for which the gas is breathed.</param>
    /// <returns>
    /// The oxygen tolerance units accrued. A partial pressure at or below the threshold of
    /// one half bar accrues no units and returns zero.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="duration" /> is negative.</exception>
    public static double OxygenToleranceUnits(Pressure partialPressureOfOxygen, TimeSpan duration)
    {
        if (duration < TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(duration), duration,
                "The duration must not be negative.");

        var po2Bar = partialPressureOfOxygen.InBar;
        if (po2Bar <= CnsThresholdBar)
            return 0.0;

        // OTU = t * ((0.5 / (po2 - 0.5)) ^ -0.83), with time in minutes.
        var factor = Math.Pow(0.5 / (po2Bar - 0.5), OtuExponent);
        return duration.TotalMinutes * factor;
    }

    /// <summary>
    /// Returns the recommended single-exposure central nervous system time limit, in
    /// minutes, for a given partial pressure of oxygen expressed in bar. The limit is
    /// derived from the piecewise linear representation of the widely published exposure
    /// table.
    /// </summary>
    /// <param name="po2Bar">The partial pressure of oxygen, in bar, strictly above the toxicity threshold.</param>
    /// <returns>The single-exposure limit, in minutes.</returns>
    private static double SingleExposureLimitMinutes(double po2Bar) =>
        // Piecewise linear segments of the NOAA single-exposure oxygen exposure limits,
        po2Bar switch
        // expressed as (partial pressure in bar, limit in minutes) breakpoints.
        {
            <= 0.6 => 720.0,
            <= 0.7 => 570.0,
            <= 0.8 => 450.0,
            <= 0.9 => 360.0,
            <= 1.0 => 300.0,
            <= 1.1 => 240.0,
            <= 1.2 => 210.0,
            <= 1.3 => 180.0,
            <= 1.4 => 150.0,
            <= 1.5 => 120.0,
            <= 1.6 => 45.0,
            _ => 45.0,
        };
}