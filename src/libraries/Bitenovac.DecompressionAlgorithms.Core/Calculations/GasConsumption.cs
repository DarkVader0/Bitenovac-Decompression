using Bitenovac.DecompressionAlgorithms.Core.Environment;
using Bitenovac.DecompressionAlgorithms.Core.Equipment;
using Bitenovac.DecompressionAlgorithms.Core.Planning;
using Bitenovac.DecompressionAlgorithms.Units;

namespace Bitenovac.DecompressionAlgorithms.Core.Calculations;

/// <summary>
/// Provides the model-agnostic calculation of the gas consumed from each cylinder over a
/// dive. The volume of gas breathed in a segment is the product of the surface air
/// consumption rate, the absolute ambient pressure at the segment's depth, and the
/// segment's duration; this free-gas volume is drawn from the cylinder whose gas matches
/// the segment's breathing gas. The resulting per-cylinder usage, including the pressure
/// remaining at the end of the dive, depends only on the expanded profile and the
/// equipment and is therefore identical for every decompression model.
/// </summary>
public static class GasConsumption
{
    /// <summary>
    /// Computes the gas consumed from each supplied cylinder over the given expanded dive
    /// profile. Each segment's gas is drawn from the cylinder holding the matching mixture,
    /// consuming a free-gas volume equal to the surface air consumption rate scaled by the
    /// ambient pressure at the segment's depth and the segment's duration. Only
    /// decompression stops are breathed at the decompression rate; every other segment,
    /// including descents, bottom time, working ascents between levels, and gas switches, is
    /// breathed at the higher bottom rate, since the diver is moving or working rather than
    /// holding a stop.
    /// </summary>
    /// <param name="segments">The fully expanded, ordered dive segments.</param>
    /// <param name="cylinders">The cylinders available to the diver.</param>
    /// <param name="settings">The settings that supply the consumption rates and the environment.</param>
    /// <returns>The gas usage for each cylinder, in the same order as <paramref name="cylinders" />.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="segments" />, <paramref name="cylinders" />, or <paramref name="settings" /> is
    /// <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException"><paramref name="cylinders" /> is empty.</exception>
    /// <exception cref="InvalidOperationException">
    /// A segment's breathing gas does not match any supplied cylinder, or the demand for gas
    /// exceeds what the matching cylinder holds.
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

        // Accumulate the free-gas volume consumed from each cylinder, indexed in step with
        // the supplied cylinder list.
        var consumedMilliliters = new double[cylinders.Count];

        foreach (var segment in segments)
        {
            var cylinderIndex = FindCylinderIndex(cylinders, segment.Gas);
            if (cylinderIndex < 0)
            {
                throw new InvalidOperationException(
                    "A segment's breathing gas does not match any supplied cylinder.");
            }

            // Only decompression stops are breathed at the decompression rate; descent,
            // bottom, working ascents between levels, and gas switches are all breathed at
            // the higher bottom rate, since the diver is moving or working rather than
            // holding a stop.
            var sacLitersPerMinute = segment.Kind == SegmentKind.Stop
                ? settings.DecoSacLitersPerMinute
                : settings.BottomSacLitersPerMinute;

            // Free-gas volume (at surface conditions) = SAC × ambient-pressure-ratio × time.
            var ambientRatio = AmbientPressure(segment.Depth, settings).InMillibar
                               / settings.SurfacePressure.InMillibar;
            var liters = sacLitersPerMinute * ambientRatio * segment.Duration.TotalMinutes;

            consumedMilliliters[cylinderIndex] += liters * 1000.0;
        }

        var usage = new CylinderGasUsage[cylinders.Count];
        for (var i = 0; i < cylinders.Count; i++)
        {
            usage[i] = BuildUsage(cylinders[i], consumedMilliliters[i], settings.SurfacePressure);
        }

        return usage;
    }

    /// <summary>
    /// Returns the index of the first cylinder whose gas equals the given mixture, or a
    /// negative value if no cylinder holds that mixture.
    /// </summary>
    /// <param name="cylinders">The cylinders to search.</param>
    /// <param name="gas">The breathing gas to match.</param>
    /// <returns>The zero-based index of the matching cylinder, or -1 if none matches.</returns>
    private static int FindCylinderIndex(IReadOnlyList<Cylinder> cylinders, GasMixture gas)
    {
        for (var i = 0; i < cylinders.Count; i++)
        {
            if (cylinders[i].Gas == gas)
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
            throw new InvalidOperationException(
                "The demand for gas exceeds what the matching cylinder holds.");
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
    /// Returns the absolute ambient pressure at a given depth, being the surface pressure
    /// plus the hydrostatic pressure of the water column for the configured salinity.
    /// </summary>
    /// <param name="depth">The depth at which the ambient pressure is required.</param>
    /// <param name="settings">The settings that supply the surface pressure and salinity.</param>
    /// <returns>The absolute ambient pressure at the depth.</returns>
    private static Pressure AmbientPressure(Depth depth, DivePlanSettings settings)
    {
        var hydrostaticMillibar = PhysicalConstants.HydrostaticPressureMillibar(settings.Salinity, depth.InMeter);
        return Pressure.FromMillibar(settings.SurfacePressure.InMillibar + hydrostaticMillibar);
    }
}