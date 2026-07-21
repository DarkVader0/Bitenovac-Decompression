using Bitenovac.DecompressionAlgorithms.Core.Environment;
using Bitenovac.DecompressionAlgorithms.Core.Equipment;
using Bitenovac.DecompressionAlgorithms.Core.Gas;
using Bitenovac.DecompressionAlgorithms.Core.Planning;
using Bitenovac.DecompressionAlgorithms.Units;

namespace Bitenovac.DecompressionAlgorithms.Core.Calculations;

/// <summary>
/// Provides the model-agnostic calculation of the reserve gas requirement for each
/// cylinder. Two rules apply according to the cylinder's role. A bottom-gas cylinder must
/// retain enough gas to bring the whole team, under stress, from the deepest point at
/// which its gas is breathed up to the next breathable gas (the next gas switch or the
/// surface); gas below the regulator's intermediate pressure is treated as unusable and is
/// excluded from what remains. A decompression-gas cylinder must simply retain a fixed
/// fraction of its capacity, so that no more than the permitted fraction is ever consumed.
/// Closed-circuit roles are not yet assessed and are reported with a zero requirement.
/// Whether a reserve is met is an expected planning outcome and is reported through flags
/// rather than by throwing.
/// </summary>
public static class ReserveGas
{
    /// <summary>
    /// The maximum fraction of a decompression cylinder's capacity that may be consumed,
    /// so that a reserve of the remaining fraction is always retained.
    /// </summary>
    private const double MaxDecoGasUsageFraction = 0.40;

    /// <summary>The depth, in meters, of the final ascent band to which the last-six-meters rate applies.</summary>
    private const double LastBandCeilingMeter = 6.0;

    /// <summary>
    /// The residual cylinder pressure below which a first stage can no longer deliver gas,
    /// being approximately the intermediate pressure of the regulator. Gas below this
    /// pressure is physically unusable and is excluded from the breathable reserve.
    /// </summary>
    private static readonly Pressure UnusableFirstStagePressure = Pressure.FromBar(10.0);

    /// <summary>
    /// Computes the reserve gas assessment for each supplied cylinder over the given
    /// expanded dive profile. Bottom-gas cylinders are assessed against a worst-case
    /// emergency ascent from the deepest point at which their gas is breathed to the next
    /// breathable gas; decompression-gas cylinders are assessed against a fixed maximum
    /// usage fraction; closed-circuit roles are reported with a zero requirement.
    /// </summary>
    /// <param name="segments">The fully expanded, ordered dive segments.</param>
    /// <param name="cylinders">The cylinders available to the diver.</param>
    /// <param name="settings">The settings that supply the ascent rates, consumption rates, and reserve factors.</param>
    /// <returns>The reserve assessment for every cylinder, in the same order as <paramref name="cylinders" />.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="segments" />, <paramref name="cylinders" />, or <paramref name="settings" /> is
    /// <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException"><paramref name="cylinders" /> is empty.</exception>
    public static ReserveGasResult Calculate(
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

        // The volume consumed from each cylinder, so that the projected remaining gas can
        // be derived without recomputing consumption for each rule.
        var usage = GasConsumption.Calculate(segments, cylinders, settings);

        var statuses = new CylinderReserveStatus[cylinders.Count];
        for (var i = 0; i < cylinders.Count; i++)
        {
            statuses[i] = AssessCylinder(cylinders[i], usage[i], segments, settings);
        }

        return new ReserveGasResult(statuses);
    }

    /// <summary>
    /// Assesses the reserve requirement for a single cylinder according to its role.
    /// </summary>
    /// <param name="cylinder">The cylinder being assessed.</param>
    /// <param name="usage">The gas usage computed for the cylinder over the dive.</param>
    /// <param name="segments">The fully expanded, ordered dive segments.</param>
    /// <param name="settings">The settings that supply the ascent and reserve parameters.</param>
    /// <returns>The reserve assessment for the cylinder.</returns>
    private static CylinderReserveStatus AssessCylinder(Cylinder cylinder,
        CylinderGasUsage usage,
        IReadOnlyList<DiveSegment> segments,
        DivePlanSettings settings)
    {
        var startMilliliters = cylinder.StartGasVolume(settings.SurfacePressure).InMilliliter;
        var consumedMilliliters = usage.GasUsed.InMilliliter;

        return cylinder.Purpose switch
        {
            CylinderPurpose.BottomGas => AssessBottomGas(cylinder, startMilliliters, consumedMilliliters, segments,
                settings),
            CylinderPurpose.DecoGas => AssessDecoGas(cylinder, startMilliliters, consumedMilliliters),
            _ => new CylinderReserveStatus(cylinder, Volume.FromMilliliter(0.0),
                Volume.FromMilliliter(Math.Max(startMilliliters - consumedMilliliters, 0.0)))
        };
    }

    /// <summary>
    /// Assesses a bottom-gas cylinder against a worst-case emergency ascent. The required
    /// reserve is the gas the whole team consumes, under stress, ascending from the deepest
    /// point at which this gas is breathed to the next breathable gas. The gas that remains
    /// excludes the unusable first-stage pressure.
    /// </summary>
    /// <param name="cylinder">The bottom-gas cylinder being assessed.</param>
    /// <param name="startMilliliters">The free-gas volume the cylinder holds at its start pressure, in milliliters.</param>
    /// <param name="consumedMilliliters">The free-gas volume consumed from the cylinder over the dive, in milliliters.</param>
    /// <param name="segments">The fully expanded, ordered dive segments.</param>
    /// <param name="settings">The settings that supply the ascent and reserve parameters.</param>
    /// <returns>The reserve assessment for the bottom-gas cylinder.</returns>
    private static CylinderReserveStatus AssessBottomGas(Cylinder cylinder,
        double startMilliliters,
        double consumedMilliliters,
        IReadOnlyList<DiveSegment> segments,
        DivePlanSettings settings)
    {
        var (deepestMeter, shallowestMeter) = DepthSpanForGas(segments, cylinder.Gas);

        var requiredMilliliters = EmergencyAscentMilliliters(deepestMeter, shallowestMeter, settings);

        // The unusable first-stage pressure is excluded from the gas that remains, since it
        // cannot be breathed in an emergency.
        var unusableMilliliters = FreeGasMilliliters(cylinder, UnusableFirstStagePressure, settings.SurfacePressure);
        var remainingMilliliters = Math.Max(startMilliliters - consumedMilliliters - unusableMilliliters, 0.0);

        return new CylinderReserveStatus(cylinder,
            Volume.FromMilliliter(requiredMilliliters),
            Volume.FromMilliliter(remainingMilliliters));
    }

    /// <summary>
    /// Assesses a decompression-gas cylinder against the fixed maximum usage fraction. The
    /// required reserve is the retained fraction of the cylinder's capacity, and the
    /// projected remaining gas is the capacity less what was consumed.
    /// </summary>
    /// <param name="cylinder">The decompression-gas cylinder being assessed.</param>
    /// <param name="startMilliliters">The free-gas volume the cylinder holds at its start pressure, in milliliters.</param>
    /// <param name="consumedMilliliters">The free-gas volume consumed from the cylinder over the dive, in milliliters.</param>
    /// <returns>The reserve assessment for the decompression-gas cylinder.</returns>
    private static CylinderReserveStatus AssessDecoGas(Cylinder cylinder,
        double startMilliliters,
        double consumedMilliliters)
    {
        var requiredMilliliters = startMilliliters * (1.0 - MaxDecoGasUsageFraction);
        var remainingMilliliters = Math.Max(startMilliliters - consumedMilliliters, 0.0);

        return new CylinderReserveStatus(cylinder,
            Volume.FromMilliliter(requiredMilliliters),
            Volume.FromMilliliter(remainingMilliliters));
    }

    /// <summary>
    /// Returns the deepest and shallowest depths, in meters, at which the given gas is
    /// breathed across the profile. When the gas is not breathed in any segment the span is
    /// empty and both depths are zero, giving a zero emergency ascent.
    /// </summary>
    /// <param name="segments">The fully expanded, ordered dive segments.</param>
    /// <param name="gas">The breathing gas whose depth span is required.</param>
    /// <returns>The deepest and shallowest depths, in meters, at which the gas is breathed.</returns>
    private static (double DeepestMeter, double ShallowestMeter) DepthSpanForGas(
        IReadOnlyList<DiveSegment> segments,
        GasMixture gas)
    {
        var found = false;
        var deepestMeter = 0.0;
        var shallowestMeter = 0.0;

        foreach (var segment in segments)
        {
            if (segment.Gas != gas)
            {
                continue;
            }

            // A segment carries a single depth (the depth held, or the depth reached at
            // its end), so that depth is both the deepest and shallowest point it covers.
            var depthMeter = segment.Depth.InMeter;

            if (!found)
            {
                deepestMeter = depthMeter;
                shallowestMeter = depthMeter;
                found = true;
                continue;
            }

            deepestMeter = Math.Max(deepestMeter, depthMeter);
            shallowestMeter = Math.Min(shallowestMeter, depthMeter);
        }

        return (deepestMeter, shallowestMeter);
    }

    /// <summary>
    /// Returns the free-gas volume the whole team consumes, under stress, ascending from
    /// the deepest to the shallowest depth. The ascent is divided at the boundary of the
    /// final ascent band so that each portion uses its appropriate ascent rate: the slower
    /// last-six-meters rate near the surface and the faster to-stops rate below it. The
    /// volume is the stress-multiplied bottom consumption rate, scaled by the mean ambient
    /// pressure of each portion and the number of team members, over the portion's time.
    /// </summary>
    /// <param name="deepestMeter">The depth, in meters, at which the emergency ascent begins.</param>
    /// <param name="shallowestMeter">The depth, in meters, at which the emergency ascent ends (the next breathable gas).</param>
    /// <param name="settings">The settings that supply the ascent rates, consumption rate, and reserve factors.</param>
    /// <returns>The free-gas volume required, in milliliters at surface conditions.</returns>
    private static double EmergencyAscentMilliliters(double deepestMeter,
        double shallowestMeter,
        DivePlanSettings settings)
    {
        if (deepestMeter - shallowestMeter <= 0.0)
        {
            return 0.0;
        }

        // Split the ascent at the final band boundary so each portion uses its own rate.
        // Portion below the final band (deeper than six meters) uses the to-stops rate;
        // the portion within the final six meters uses the last-six-meters rate.
        var lowerBound = Math.Max(shallowestMeter, LastBandCeilingMeter);
        var litersPerDiver = 0.0;

        if (deepestMeter > lowerBound)
        {
            litersPerDiver += PortionLitersPerDiver(deepestMeter, lowerBound,
                settings.AscentRate50PercentToStopsMetersPerMinute, settings);
        }

        var finalBandTop = Math.Min(deepestMeter, LastBandCeilingMeter);
        if (finalBandTop > shallowestMeter)
        {
            litersPerDiver += PortionLitersPerDiver(finalBandTop, shallowestMeter,
                settings.AscentRateLastSixMetersMetersPerMinute, settings);
        }

        return litersPerDiver * settings.ReserveTeamSize * 1000.0;
    }

    /// <summary>
    /// Returns the free-gas volume, in liters at surface conditions, that a single diver
    /// consumes ascending a portion of the water column from a deeper to a shallower depth
    /// at a given ascent rate, under the stress-multiplied bottom consumption rate.
    /// </summary>
    /// <param name="fromMeter">The depth, in meters, at which the portion begins.</param>
    /// <param name="toMeter">The depth, in meters, at which the portion ends.</param>
    /// <param name="ascentRateMetersPerMinute">The ascent rate applied over the portion, in meters per minute.</param>
    /// <param name="settings">The settings that supply the consumption rate, stress factor, and environment.</param>
    /// <returns>The free-gas volume for one diver over the portion, in liters at surface conditions.</returns>
    private static double PortionLitersPerDiver(double fromMeter,
        double toMeter,
        double ascentRateMetersPerMinute,
        DivePlanSettings settings)
    {
        var portionMeters = fromMeter - toMeter;
        if (portionMeters <= 0.0)
        {
            return 0.0;
        }

        var ascentMinutes = portionMeters / ascentRateMetersPerMinute;

        // Mean ambient pressure ratio over the portion, using its midpoint depth.
        var midpointMeter = (fromMeter + toMeter) / 2.0;
        var meanAmbientRatio = AmbientPressure(midpointMeter, settings).InMillibar
                               / settings.SurfacePressure.InMillibar;

        return settings.BottomSacLitersPerMinute
               * settings.ReserveStressFactor
               * meanAmbientRatio
               * ascentMinutes;
    }

    /// <summary>
    /// Returns the free-gas volume a cylinder holds at a given pressure, referenced to the
    /// surface pressure, in milliliters.
    /// </summary>
    /// <param name="cylinder">The cylinder whose free-gas volume is required.</param>
    /// <param name="pressure">The cylinder pressure at which the free-gas volume is measured.</param>
    /// <param name="surfacePressure">The surface pressure against which the free-gas volume is referenced.</param>
    /// <returns>The free-gas volume, in milliliters at surface conditions.</returns>
    private static double FreeGasMilliliters(Cylinder cylinder,
        Pressure pressure,
        Pressure surfacePressure) => cylinder.Size.InMilliliter * (pressure.InMillibar / surfacePressure.InMillibar);

    /// <summary>
    /// Returns the absolute ambient pressure at a given depth, being the surface pressure
    /// plus the hydrostatic pressure of the water column for the configured salinity.
    /// </summary>
    /// <param name="depthMeter">The depth, in meters, at which the ambient pressure is required.</param>
    /// <param name="settings">The settings that supply the surface pressure and salinity.</param>
    /// <returns>The absolute ambient pressure at the depth.</returns>
    private static Pressure AmbientPressure(double depthMeter, DivePlanSettings settings)
    {
        var hydrostaticMillibar = PhysicalConstants.HydrostaticPressureMillibar(settings.Salinity, depthMeter);
        return Pressure.FromMillibar(settings.SurfacePressure.InMillibar + hydrostaticMillibar);
    }
}