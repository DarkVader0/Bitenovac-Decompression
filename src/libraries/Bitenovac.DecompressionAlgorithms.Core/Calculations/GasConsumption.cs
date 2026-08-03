using System.Globalization;
using Bitenovac.DecompressionAlgorithms.Core.Environment;
using Bitenovac.DecompressionAlgorithms.Core.Equipment;
using Bitenovac.DecompressionAlgorithms.Core.Planning;
using Bitenovac.DecompressionAlgorithms.Units;

namespace Bitenovac.DecompressionAlgorithms.Core.Calculations;

/// <summary>
/// Provides the model-agnostic calculation of the gas consumed from each cylinder over a
/// dive. What the diver draws from the cylinders depends on the breathing apparatus. On
/// open circuit every breath is taken from the cylinder and vented, so the volume consumed
/// in a segment is the product of the surface air consumption rate, the absolute ambient
/// pressure at the segment's depth, and the segment's duration. A closed-circuit loop vents
/// nothing, so it draws only the oxygen the diver metabolises and the diluent needed to keep
/// the loop full as the ambient pressure rises during a descent. A passive semi-closed loop
/// vents a fixed fraction of each breath, so it draws that fraction of the open-circuit
/// demand from its supply, plus the same descent make-up. Per-cylinder usage, including the
/// pressure remaining at the end of the dive, depends only on the expanded profile and the
/// equipment, and is shared by every decompression model.
/// </summary>
public static class GasConsumption
{
    /// <summary>
    /// Computes the gas consumed from each supplied cylinder over the given expanded dive
    /// profile. Each segment draws from the cylinder holding its supply gas, preferring the
    /// diluent supply when breathed through a rebreather, at the rate its apparatus demands.
    /// Only decompression stops use the decompression rate; every other segment, including
    /// descents, bottom time, working ascents and gas switches, uses the bottom rate.
    /// </summary>
    /// <param name="segments">The fully expanded, ordered, depth-contiguous dive segments.</param>
    /// <param name="cylinders">The cylinders available to the diver.</param>
    /// <param name="settings">The settings that supply the consumption rates, the loop parameters, and the environment.</param>
    /// <returns>The gas usage for each cylinder, in the same order as <paramref name="cylinders" />.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="segments" />, <paramref name="cylinders" />, or <paramref name="settings" /> is
    /// <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException"><paramref name="cylinders" /> is empty.</exception>
    /// <exception cref="InvalidOperationException">
    /// A segment's supply gas does not match any supplied cylinder, a closed-circuit segment
    /// has no oxygen cylinder to draw on, or the demand for gas exceeds what the matching
    /// cylinder holds.
    /// </exception>
    public static IReadOnlyList<CylinderGasUsage> Calculate(
        IReadOnlyList<DiveSegment> segments,
        IReadOnlyList<Cylinder> cylinders,
        DivePlanSettings settings)
    {
        ArgumentNullException.ThrowIfNull(segments);
        ArgumentNullException.ThrowIfNull(cylinders);
        ArgumentNullException.ThrowIfNull(settings);

        if (cylinders.Count == 0)
        {
            throw new ArgumentException("At least one cylinder must be available.", nameof(cylinders));
        }

        // Indexed in step with the supplied cylinder list.
        var consumedMilliliters = new double[cylinders.Count];

        // Each segment begins where the previous one ended, which is what makes the loop
        // make-up over a descent measurable. The first begins at the surface.
        var previousDepthMeter = 0.0;

        foreach (var segment in segments)
        {
            AccumulateSegment(segment, previousDepthMeter, cylinders, settings, consumedMilliliters);
            previousDepthMeter = segment.Depth.InMeter;
        }

        var usage = new CylinderGasUsage[cylinders.Count];
        for (var i = 0; i < cylinders.Count; i++)
        {
            usage[i] = BuildUsage(cylinders[i], consumedMilliliters[i], settings.SurfacePressure);
        }

        return usage;
    }

    /// <summary>
    /// Adds the gas one segment draws from each cylinder to the running totals, according to
    /// the breathing apparatus through which it is breathed.
    /// </summary>
    /// <param name="segment">The segment whose demand is being accumulated.</param>
    /// <param name="startDepthMeter">The depth, in meters, at which the segment begins.</param>
    /// <param name="cylinders">The cylinders available to the diver.</param>
    /// <param name="settings">The settings that supply the consumption rates, the loop parameters, and the environment.</param>
    /// <param name="consumedMilliliters">The running per-cylinder totals, in milliliters, to which the demand is added.</param>
    /// <exception cref="InvalidOperationException">
    /// The segment's supply gas does not match any supplied cylinder, or a closed-circuit
    /// segment has no oxygen cylinder to draw on.
    /// </exception>
    private static void AccumulateSegment(in DiveSegment segment,
        double startDepthMeter,
        IReadOnlyList<Cylinder> cylinders,
        DivePlanSettings settings,
        double[] consumedMilliliters)
    {
        var mode = segment.Loop.Mode;
        var supplyIndex = FindSupplyIndex(cylinders, segment.Gas, mode);
        if (supplyIndex < 0)
        {
            throw new InvalidOperationException(
                "A segment's supply gas does not match any supplied cylinder.");
        }

        // Only decompression stops use the decompression rate. The metabolic demand of a
        // rebreather diver is split the same way.
        var restingAtAStop = segment.Kind == SegmentKind.Stop;
        var sacLitersPerMinute = restingAtAStop
            ? settings.DecoSacLitersPerMinute
            : settings.BottomSacLitersPerMinute;

        var minutes = segment.Duration.TotalMinutes;
        var endAmbientRatio = AmbientRatio(segment.Depth.InMeter, settings);

        if (mode == DiveMode.OC)
        {
            // Free-gas volume (at surface conditions) = SAC × ambient-pressure-ratio × time.
            consumedMilliliters[supplyIndex] += sacLitersPerMinute * endAmbientRatio * minutes * 1000.0;
            return;
        }

        // A rebreather loop is a fixed volume of gas held at ambient pressure, so descending
        // compresses it and the difference must be made up from the diluent supply. Ascending
        // vents the excess overboard and draws nothing.
        var startAmbientRatio = AmbientRatio(startDepthMeter, settings);
        var makeUpLiters = settings.LoopVolumeLiters * Math.Max(endAmbientRatio - startAmbientRatio, 0.0);
        consumedMilliliters[supplyIndex] += makeUpLiters * 1000.0;

        if (mode == DiveMode.PSCR)
        {
            // The loop vents its dump ratio of every breath and replaces it from the supply.
            var ventedLiters = segment.Loop.DumpRatio * sacLitersPerMinute * endAmbientRatio * minutes;
            consumedMilliliters[supplyIndex] += ventedLiters * 1000.0;
            return;
        }

        // A closed-circuit loop vents nothing, so the only gas it consumes beyond the descent
        // make-up is the oxygen the diver metabolises, which is drawn from the oxygen supply
        // at a rate that does not vary with depth.
        var metabolicRate = restingAtAStop
            ? settings.DecoMetabolicOxygenConsumptionLitersPerMinute
            : settings.BottomMetabolicOxygenConsumptionLitersPerMinute;
        var oxygenLiters = metabolicRate * minutes;
        if (oxygenLiters <= 0.0)
        {
            return;
        }

        var oxygenIndex = FindPurposeIndex(cylinders, CylinderPurpose.Oxygen);
        if (oxygenIndex < 0)
        {
            throw new InvalidOperationException(
                "A closed-circuit segment requires a cylinder carried for the oxygen supply.");
        }

        consumedMilliliters[oxygenIndex] += oxygenLiters * 1000.0;
    }

    /// <summary>
    /// Returns the index of the cylinder supplying the given gas, or a negative value if no
    /// cylinder holds it. When more than one cylinder holds the same mixture the role
    /// decides: a segment breathed through a rebreather is supplied from the diluent, and a
    /// segment breathed open circuit is taken from a stage rather than from the small oxygen
    /// supply that feeds a rebreather loop.
    /// </summary>
    /// <param name="cylinders">The cylinders to search.</param>
    /// <param name="gas">The supply gas to match.</param>
    /// <param name="mode">The breathing apparatus through which the gas is supplied.</param>
    /// <returns>The zero-based index of the supplying cylinder, or -1 if none matches.</returns>
    private static int FindSupplyIndex(IReadOnlyList<Cylinder> cylinders,
        GasMixture gas,
        DiveMode mode)
    {
        var preferred = mode == DiveMode.OC ? null : (CylinderPurpose?)CylinderPurpose.Diluent;

        for (var i = 0; i < cylinders.Count; i++)
        {
            if (cylinders[i].Gas != gas)
            {
                continue;
            }

            var matchesPreference = preferred is { } purpose
                ? cylinders[i].Purpose == purpose
                : cylinders[i].Purpose != CylinderPurpose.Oxygen;

            if (matchesPreference)
            {
                return i;
            }
        }

        for (var i = 0; i < cylinders.Count; i++)
        {
            if (cylinders[i].Gas == gas)
            {
                return i;
            }
        }

        return -1;
    }

    /// <summary>Returns the index of the first cylinder carried for the given role, or -1 if there is none.</summary>
    /// <param name="cylinders">The cylinders to search.</param>
    /// <param name="purpose">The role to match.</param>
    /// <returns>The zero-based index of the matching cylinder, or -1 if none matches.</returns>
    private static int FindPurposeIndex(IReadOnlyList<Cylinder> cylinders, CylinderPurpose purpose)
    {
        for (var i = 0; i < cylinders.Count; i++)
        {
            if (cylinders[i].Purpose == purpose)
            {
                return i;
            }
        }

        return -1;
    }

    /// <summary>
    /// Builds the gas usage for a single cylinder from the free-gas volume consumed,
    /// converting that volume back into a remaining cylinder pressure.
    /// </summary>
    /// <param name="cylinder">The cylinder from which gas was drawn.</param>
    /// <param name="consumedMilliliters">The free-gas volume consumed, in milliliters at surface conditions.</param>
    /// <param name="surfacePressure">The surface pressure against which free-gas volumes are referenced.</param>
    /// <returns>The gas usage for the cylinder.</returns>
    /// <exception cref="InvalidOperationException">The demand for gas exceeds what the cylinder holds.</exception>
    private static CylinderGasUsage BuildUsage(Cylinder cylinder,
        double consumedMilliliters,
        Pressure surfacePressure)
    {
        var availableMilliliters = cylinder.StartGasVolume(surfacePressure).InMilliliter;
        if (consumedMilliliters > availableMilliliters)
        {
            throw new InvalidOperationException(string.Create(CultureInfo.InvariantCulture,
                $"The demand for gas exceeds what the matching cylinder holds: the profile draws " +
                $"{consumedMilliliters / 1000.0:0.##} L of {cylinder.Gas} from a cylinder holding " +
                $"{availableMilliliters / 1000.0:0.##} L."));
        }

        var gasUsed = Volume.FromMilliliter(consumedMilliliters);

        // Remaining pressure is the start pressure scaled by the fraction of free gas that
        // remains, since free-gas volume is proportional to cylinder pressure.
        var remainingFraction = availableMilliliters > 0.0
            ? (availableMilliliters - consumedMilliliters) / availableMilliliters
            : 0.0;
        var endPressure = Pressure.FromMillibar(cylinder.StartPressure.InMillibar * remainingFraction);

        return new CylinderGasUsage(cylinder, gasUsed, endPressure);
    }

    /// <summary>
    /// Returns the ratio of the absolute ambient pressure at a given depth to the surface
    /// pressure, by which a surface-referenced volume is scaled to the volume breathed at
    /// that depth.
    /// </summary>
    /// <param name="depthMeter">The depth, in meters, at which the ratio is required.</param>
    /// <param name="settings">The settings that supply the surface pressure and salinity.</param>
    /// <returns>The ambient pressure at the depth as a multiple of the surface pressure.</returns>
    private static double AmbientRatio(double depthMeter, DivePlanSettings settings)
    {
        var hydrostaticMillibar = PhysicalConstants.HydrostaticPressureMillibar(settings.Salinity, depthMeter);
        return (settings.SurfacePressure.InMillibar + hydrostaticMillibar) / settings.SurfacePressure.InMillibar;
    }
}