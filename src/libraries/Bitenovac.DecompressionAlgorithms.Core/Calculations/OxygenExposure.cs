using Bitenovac.DecompressionAlgorithms.Core.Environment;
using Bitenovac.DecompressionAlgorithms.Core.Planning;
using Bitenovac.DecompressionAlgorithms.Units;

namespace Bitenovac.DecompressionAlgorithms.Core.Calculations;

/// <summary>
/// Provides the model-agnostic aggregation of oxygen toxicity over an expanded dive
/// profile. The central nervous system toxicity and the pulmonary oxygen tolerance units
/// are accumulated segment by segment, with the partial pressure of oxygen taken to change
/// linearly across each segment from the depth at the end of the previous segment to the
/// depth at the end of the current one, so the segments are required to be contiguous in
/// depth. The exposure depends only on the partial pressure of oxygen and the time, and is
/// shared by every decompression model.
/// </summary>
public static class OxygenExposure
{
    /// <summary>
    /// Computes the total oxygen toxicity accrued over the given expanded dive profile. The
    /// partial pressure of oxygen at the start of each segment is that at the end of the
    /// previous segment, so that the exposure across ascents and descents is integrated
    /// over the linear change in partial pressure rather than approximated by a single
    /// depth. The first segment begins at the surface.
    /// </summary>
    /// <param name="segments">The fully expanded, ordered, depth-contiguous dive segments.</param>
    /// <param name="settings">The settings that supply the surface pressure and salinity.</param>
    /// <returns>The accrued central nervous system and pulmonary oxygen toxicity.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="segments" /> or <paramref name="settings" /> is <see langword="null" />.
    /// </exception>
    public static OxygenExposureResult Calculate(
        IReadOnlyList<DiveSegment> segments,
        DivePlanSettings settings)
    {
        ArgumentNullException.ThrowIfNull(segments);
        ArgumentNullException.ThrowIfNull(settings);

        var cnsPercent = 0.0;
        var otu = 0.0;

        var previousDepthMeter = 0.0;

        foreach (var segment in segments)
        {
            var startPo2Mbar = Po2Mbar(previousDepthMeter, segment, settings);
            var endPo2Mbar = Po2Mbar(segment.Depth.InMeter, segment, settings);
            var durationSec = (int)Math.Round(segment.Duration.TotalSeconds, MidpointRounding.AwayFromZero);

            cnsPercent += OxygenToxicity.CalculateCnsTransition(startPo2Mbar, endPo2Mbar, durationSec);
            otu += OxygenToxicity.CalculateOtuTransition(startPo2Mbar, endPo2Mbar, durationSec);

            previousDepthMeter = segment.Depth.InMeter;
        }

        var cnsFraction = cnsPercent / 100.0;

        return new OxygenExposureResult(cnsFraction, otu);
    }

    /// <summary>
    /// Returns the partial pressure of oxygen, in millibars, that the segment's breathing
    /// apparatus delivers from its supply gas at the given depth. On open circuit this is
    /// the ambient pressure scaled by the oxygen fraction of the supply; on a rebreather the
    /// loop sets it.
    /// </summary>
    /// <param name="depthMeter">The depth, in meters, at which the partial pressure is required.</param>
    /// <param name="segment">The segment supplying the gas and the breathing apparatus.</param>
    /// <param name="settings">The settings that supply the surface pressure and salinity.</param>
    /// <returns>The partial pressure of oxygen, in millibars, rounded to the nearest millibar.</returns>
    private static int Po2Mbar(double depthMeter,
        in DiveSegment segment,
        DivePlanSettings settings)
    {
        var hydrostaticMillibar = PhysicalConstants.HydrostaticPressureMillibar(settings.Salinity, depthMeter);
        var ambient = Pressure.FromMillibar(settings.SurfacePressure.InMillibar + hydrostaticMillibar);
        var po2Millibar = segment.Loop.InspiredOxygenPressure(segment.Gas, ambient).InMillibar;
        return (int)Math.Round(po2Millibar, MidpointRounding.AwayFromZero);
    }
}