using Bitenovac.DecompressionAlgorithms.Units;

namespace Bitenovac.DecompressionAlgorithms.Core.Calculations;

/// <summary>
/// Provides calculations of oxygen toxicity exposure: the central nervous system (CNS)
/// toxicity expressed as a percentage of the recommended single-exposure limit, and the
/// pulmonary oxygen toxicity expressed in oxygen tolerance units (OTU). Both quantities
/// depend only on the partial pressure of oxygen and the time of exposure and so are
/// independent of the decompression model in use.
/// </summary>
/// <remarks>
/// The core calculations operate on partial pressures in millibars and durations in
/// seconds, matching the integer canonical units used on the planning hot path.
/// Strongly-typed overloads that accept a <see cref="Pressure" /> and a
/// <see cref="TimeSpan" /> are provided for callers elsewhere in the system. The central
/// nervous system rate is the two-line exponential fit to the logarithm of the NOAA
/// single-exposure table used by common dive-planning software. Both the central nervous
/// system and the pulmonary (OTU) calculations evaluate the exact time-integral of their
/// relation over a segment during which the partial pressure of oxygen changes linearly,
/// which is more precise than a fixed-mean evaluation or a truncated polynomial
/// approximation. Evaluating either relation at the mean partial pressure of a segment
/// understates the exposure, because both are convex in the partial pressure.
/// </remarks>
public static class OxygenToxicity
{
    /// <summary>
    /// The partial pressure of oxygen, in millibars, at or below which no oxygen toxicity
    /// is accrued.
    /// </summary>
    private const int ThresholdMbar = 500;

    /// <summary>The exponent applied in Baker's pulmonary oxygen tolerance relation.</summary>
    private const double OtuExponent = 0.83;

    /// <summary>
    /// The partial pressure of oxygen, in millibars, at which the two-line fit to the NOAA
    /// single-exposure table changes from its lower to its upper branch.
    /// </summary>
    private const int BranchMbar = 1500;

    /// <summary>The intercept of the lower branch of the central nervous system fit.</summary>
    private const double LowerBranchIntercept = -11.7853;

    /// <summary>The slope of the lower branch of the central nervous system fit, per millibar.</summary>
    private const double LowerBranchSlope = 0.00193873;

    /// <summary>The intercept of the upper branch of the central nervous system fit.</summary>
    private const double UpperBranchIntercept = -23.6349;

    /// <summary>The slope of the upper branch of the central nervous system fit, per millibar.</summary>
    private const double UpperBranchSlope = 0.00980829;

    /// <summary>
    /// Returns the instantaneous central nervous system oxygen toxicity rate, as a
    /// fraction of the single-exposure limit accrued per second, for a given partial
    /// pressure of oxygen.
    /// </summary>
    /// <param name="po2Mbar">The partial pressure of oxygen, in millibars.</param>
    /// <returns>
    /// The fraction of the single-exposure limit accrued per second. A partial pressure at
    /// or below the toxicity threshold accrues nothing and returns zero.
    /// </returns>
    public static double CnsRatePerSecond(int po2Mbar)
    {
        if (po2Mbar <= ThresholdMbar)
        {
            return 0.0;
        }

        // Two lines fitted to the logarithm of the NOAA single-exposure CNS table.
        return po2Mbar <= BranchMbar
            ? Math.Exp(LowerBranchIntercept + LowerBranchSlope * po2Mbar)
            : Math.Exp(UpperBranchIntercept + UpperBranchSlope * po2Mbar);
    }

    /// <summary>
    /// Returns the central nervous system oxygen toxicity accrued by breathing a gas at a
    /// fixed partial pressure of oxygen for a given time, expressed as a percentage of the
    /// recommended single-exposure limit.
    /// </summary>
    /// <param name="po2Mbar">The partial pressure of oxygen, in millibars.</param>
    /// <param name="durationSec">The duration of the exposure, in seconds.</param>
    /// <returns>
    /// The percentage of the single-exposure central nervous system limit accrued, where a
    /// value of one hundred represents the whole limit.
    /// </returns>
    public static double CalculateCns(int po2Mbar, int durationSec) =>
        CnsRatePerSecond(po2Mbar) * durationSec * 100.0;

    /// <summary>
    /// Returns the central nervous system oxygen toxicity accrued over a segment during
    /// which the partial pressure of oxygen changes linearly from the start to the end
    /// value, expressed as a percentage of the recommended single-exposure limit. This
    /// evaluates the exact time-integral of the rate over the linear ramp, and is therefore
    /// more precise than evaluating the rate at the mean partial pressure of the segment:
    /// the rate is exponential in the partial pressure, so a mean evaluation always
    /// understates the exposure. The portion of the ramp at or below the toxicity threshold
    /// contributes nothing, and the change of branch in the underlying fit is integrated
    /// across exactly.
    /// </summary>
    /// <param name="startPo2Mbar">The partial pressure of oxygen at the start of the segment, in millibars.</param>
    /// <param name="endPo2Mbar">The partial pressure of oxygen at the end of the segment, in millibars.</param>
    /// <param name="durationSec">The duration of the segment, in seconds.</param>
    /// <returns>The percentage of the single-exposure central nervous system limit accrued over the segment.</returns>
    public static double CalculateCnsTransition(int startPo2Mbar,
        int endPo2Mbar,
        int durationSec)
    {
        // A flat segment has no ramp to integrate over, and the exposure is simply the rate
        // at that partial pressure held for the duration.
        if (startPo2Mbar == endPo2Mbar)
        {
            return CalculateCns(startPo2Mbar, durationSec);
        }

        // The exposure depends only on the span the ramp covers, not on its direction.
        var lowerMbar = Math.Min(startPo2Mbar, endPo2Mbar);
        var upperMbar = Math.Max(startPo2Mbar, endPo2Mbar);

        if (upperMbar <= ThresholdMbar)
        {
            return 0.0;
        }

        // The mean rate over the segment is the integral of the rate across the toxic part
        // of the ramp divided by the full span, since the part at or below the threshold
        // accrues nothing. The partial pressure moves linearly in time, so the mean over
        // the partial pressure is also the mean over time.
        var meanRate = RateIntegral(Math.Max(lowerMbar, ThresholdMbar), upperMbar)
                       / (upperMbar - lowerMbar);

        return meanRate * durationSec * 100.0;
    }

    /// <summary>
    /// Returns the integral of the central nervous system rate with respect to the partial
    /// pressure of oxygen, over a range that lies entirely above the toxicity threshold.
    /// The range is split at the branch point of the underlying fit so that each branch is
    /// integrated over the part of the range to which it applies.
    /// </summary>
    /// <param name="fromMbar">The lower bound of the range, in millibars, at or above the toxicity threshold.</param>
    /// <param name="toMbar">The upper bound of the range, in millibars.</param>
    /// <returns>The integral of the rate over the range, in fraction of the limit per second times millibars.</returns>
    private static double RateIntegral(double fromMbar, double toMbar)
    {
        var integral = 0.0;

        if (fromMbar < BranchMbar)
        {
            integral += BranchIntegral(LowerBranchIntercept, LowerBranchSlope,
                fromMbar, Math.Min(toMbar, BranchMbar));
        }

        if (toMbar > BranchMbar)
        {
            integral += BranchIntegral(UpperBranchIntercept, UpperBranchSlope,
                Math.Max(fromMbar, BranchMbar), toMbar);
        }

        return integral;
    }

    /// <summary>
    /// Returns the integral of a single exponential branch of the fit with respect to the
    /// partial pressure of oxygen, being the antiderivative of exp(intercept + slope × p)
    /// evaluated between the bounds.
    /// </summary>
    /// <param name="intercept">The intercept of the branch.</param>
    /// <param name="slope">The slope of the branch, per millibar.</param>
    /// <param name="fromMbar">The lower bound of the range, in millibars.</param>
    /// <param name="toMbar">The upper bound of the range, in millibars.</param>
    /// <returns>The integral of the branch over the range.</returns>
    private static double BranchIntegral(double intercept,
        double slope,
        double fromMbar,
        double toMbar) =>
        (Math.Exp(intercept + slope * toMbar) - Math.Exp(intercept + slope * fromMbar)) / slope;

    /// <summary>
    /// Returns the pulmonary oxygen toxicity, in oxygen tolerance units (OTU), accrued by
    /// breathing a gas at a fixed partial pressure of oxygen for a given time.
    /// </summary>
    /// <param name="po2Mbar">The partial pressure of oxygen, in millibars.</param>
    /// <param name="durationSec">The duration of the exposure, in seconds.</param>
    /// <returns>
    /// The oxygen tolerance units accrued. A partial pressure at or below the threshold
    /// accrues nothing and returns zero.
    /// </returns>
    public static double CalculateOtu(int po2Mbar, int durationSec) =>
        CalculateOtuTransition(po2Mbar, po2Mbar, durationSec);

    /// <summary>
    /// Returns the pulmonary oxygen toxicity, in oxygen tolerance units (OTU), accrued over
    /// a segment during which the partial pressure of oxygen changes linearly from the
    /// start to the end value. This evaluates the exact time-integral of Baker's oxygen
    /// tolerance relation over the linear ramp, and is therefore more precise than either a
    /// fixed-mean evaluation or a truncated polynomial approximation.
    /// </summary>
    /// <param name="startPo2Mbar">The partial pressure of oxygen at the start of the segment, in millibars.</param>
    /// <param name="endPo2Mbar">The partial pressure of oxygen at the end of the segment, in millibars.</param>
    /// <param name="durationSec">The duration of the segment, in seconds.</param>
    /// <returns>The oxygen tolerance units accrued over the segment.</returns>
    public static double CalculateOtuTransition(int startPo2Mbar,
        int endPo2Mbar,
        int durationSec)
    {
        double po2i = startPo2Mbar;
        double po2f = endPo2Mbar;
        double t = durationSec;

        // If the whole segment is at or below the threshold, no OTU accrues.
        if (po2i <= ThresholdMbar && po2f <= ThresholdMbar)
        {
            return 0.0;
        }

        // Clip the segment to the portion strictly above the threshold, so that the ramp
        // used for the integral covers only the toxic part of the exposure.
        if (po2i < ThresholdMbar)
        {
            t *= (po2f - ThresholdMbar) / (po2f - po2i);
            po2i = ThresholdMbar;
        }
        else if (po2f < ThresholdMbar)
        {
            t *= (po2i - ThresholdMbar) / (po2i - po2f);
            po2f = ThresholdMbar;
        }

        var durationMin = t / 60.0;

        // Normalised toxicity variable x = (po2 - 0.5 bar) / 0.5 bar, evaluated in bar.
        var xi = (po2i / 1000.0 - 0.5) / 0.5;
        var xf = (po2f / 1000.0 - 0.5) / 0.5;

        // The clipping above guarantees both endpoints are at or above the threshold, so
        // xi and xf are non-negative and the power below is always well-defined.

        // Exact integral of x^k over a linear ramp from xi to xf is
        //   (xf^(k+1) - xi^(k+1)) / ((k + 1) * (xf - xi)).
        // When the endpoints coincide the ramp is flat and the integral is simply x^k.
        if (Math.Abs(xf - xi) < 1e-12)
        {
            return durationMin * Math.Pow(xi, OtuExponent);
        }

        var k1 = OtuExponent + 1.0;
        var integralMean = (Math.Pow(xf, k1) - Math.Pow(xi, k1)) / (k1 * (xf - xi));
        return durationMin * integralMean;
    }

    /// <summary>
    /// Returns the central nervous system oxygen toxicity accrued over a segment during
    /// which the partial pressure of oxygen changes linearly, expressed as a percentage of
    /// the recommended single-exposure limit.
    /// </summary>
    /// <param name="startPo2">The partial pressure of oxygen at the start of the segment.</param>
    /// <param name="endPo2">The partial pressure of oxygen at the end of the segment.</param>
    /// <param name="duration">The duration of the segment.</param>
    /// <returns>The percentage of the single-exposure central nervous system limit accrued over the segment.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="duration" /> is negative.</exception>
    public static double CalculateCnsTransition(Pressure startPo2,
        Pressure endPo2,
        TimeSpan duration) =>
        CalculateCnsTransition(ToMbar(startPo2), ToMbar(endPo2), ToSeconds(duration));

    /// <summary>
    /// Returns the pulmonary oxygen toxicity, in oxygen tolerance units (OTU), accrued over
    /// a segment during which the partial pressure of oxygen changes linearly.
    /// </summary>
    /// <param name="startPo2">The partial pressure of oxygen at the start of the segment.</param>
    /// <param name="endPo2">The partial pressure of oxygen at the end of the segment.</param>
    /// <param name="duration">The duration of the segment.</param>
    /// <returns>The oxygen tolerance units accrued over the segment.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="duration" /> is negative.</exception>
    public static double CalculateOtuTransition(Pressure startPo2,
        Pressure endPo2,
        TimeSpan duration) =>
        CalculateOtuTransition(ToMbar(startPo2), ToMbar(endPo2), ToSeconds(duration));

    private static int ToMbar(Pressure pressure) => (int)Math.Round(pressure.InMillibar, MidpointRounding.AwayFromZero);

    private static int ToSeconds(TimeSpan duration)
    {
        if (duration < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(duration), duration,
                "The duration must not be negative.");
        }

        return (int)Math.Round(duration.TotalSeconds, MidpointRounding.AwayFromZero);
    }
}