using Bitenovac.DecompressionAlgorithms.Core.Abstractions;
using Bitenovac.DecompressionAlgorithms.Core.Calculations;
using Bitenovac.DecompressionAlgorithms.Core.Environment;
using Bitenovac.DecompressionAlgorithms.Core.Equipment;
using Bitenovac.DecompressionAlgorithms.Units;

namespace Bitenovac.DecompressionAlgorithms.Core.Planning;

/// <summary>
/// Plans a dive by combining a decompression model with the shared, model-agnostic
/// planning logic. The planner walks the planned profile level by level: it builds the
/// descent, working ascent, and bottom segments, advances the model's tissue state over
/// each, and validates every inter-level ascent against the model's ceiling. It then asks
/// the model for the final ascent to the surface and runs the shared calculators for gas
/// consumption, reserve gas, and oxygen toxicity over the complete profile. An inter-level
/// ascent that would incur a decompression obligation is recorded as a violation and marks
/// the resulting plan invalid, rather than being silently planned without the required
/// stop.
/// </summary>
public sealed class DivePlanner
{
    private readonly IDecompressionAlgorithm _algorithm;

    /// <summary>Initializes a new instance of the <see cref="DivePlanner" /> class.</summary>
    /// <param name="algorithm">The decompression model used to track tissue loading and compute the ascent.</param>
    /// <exception cref="ArgumentNullException"><paramref name="algorithm" /> is <see langword="null" />.</exception>
    public DivePlanner(IDecompressionAlgorithm algorithm)
    {
        ArgumentNullException.ThrowIfNull(algorithm);
        _algorithm = algorithm;
    }

    /// <summary>
    /// Creates the decompression plan for the given request. The working phase is built and
    /// loaded into the model level by level, each inter-level ascent is validated against
    /// the model's ceiling, the final ascent to the surface is computed by the model, and
    /// the shared calculators are run over the complete profile.
    /// </summary>
    /// <param name="request">The planning request supplying the profile, cylinders, and settings.</param>
    /// <returns>The decompression plan, marked invalid if any inter-level ascent would incur an obligation.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="request" /> is <see langword="null" />.</exception>
    public DecoPlan CreatePlan(DivePlanRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var settings = request.Settings;
        var cylinders = request.Cylinders;

        var segments = new List<DiveSegment>();
        var violations = new List<AscentViolation>();

        var state = _algorithm.BeginDive(request);
        var currentDepthMeter = 0.0;

        foreach (var target in request.Profile.Segments)
        {
            var targetDepthMeter = target.Depth.InMeter;

            if (targetDepthMeter > currentDepthMeter)
            {
                // Descend to the deeper level; a descent never incurs an obligation.
                var descent = BuildTravel(currentDepthMeter, targetDepthMeter,
                    settings.DescentRateMetersPerMinute, SegmentKind.Descent, cylinders, settings);
                segments.Add(descent);
                state = _algorithm.LoadSegment(state, descent);
                currentDepthMeter = targetDepthMeter;
            }
            else if (targetDepthMeter < currentDepthMeter)
            {
                // Validate the inter-level ascent against the model's ceiling before making
                // it. A ceiling deeper than the target means a stop would be required.
                var ceiling = _algorithm.CurrentCeiling(state);
                if (ceiling.InMeter > targetDepthMeter)
                {
                    violations.Add(new AscentViolation(
                        Depth.FromMeter(currentDepthMeter),
                        Depth.FromMeter(targetDepthMeter),
                        ceiling));
                }

                // The working ascent is emitted regardless so that the profile stays
                // continuous for the shared calculators; the violation records the problem.
                var ascent = BuildTravel(currentDepthMeter, targetDepthMeter,
                    settings.AscentRateBelow75PercentMetersPerMinute, SegmentKind.Ascent, cylinders, settings);
                segments.Add(ascent);
                state = _algorithm.LoadSegment(state, ascent);
                currentDepthMeter = targetDepthMeter;
            }

            if (target.Duration > TimeSpan.Zero)
            {
                var bottomCylinder = SelectBottomGas(cylinders, targetDepthMeter, settings);
                var bottom = new DiveSegment(Depth.FromMeter(targetDepthMeter), target.Duration,
                    bottomCylinder.Gas, SegmentKind.Bottom);
                segments.Add(bottom);
                state = _algorithm.LoadSegment(state, bottom);
                currentDepthMeter = targetDepthMeter;
            }
        }

        // The model computes the final ascent to the surface, including the decompression
        // stops and any gas switches.
        var finalAscent = _algorithm.CalculateFinalAscent(state, request);
        segments.AddRange(finalAscent);

        // Run the shared, model-agnostic calculators over the complete profile.
        var gasUsage = GasConsumption.Calculate(segments, cylinders, settings);
        var reserve = ReserveGas.Calculate(segments, cylinders, settings);
        var oxygen = OxygenExposure.Calculate(segments, settings);
        var runtime = TotalRuntime(segments);

        return new DecoPlan(segments, gasUsage, runtime, reserve,
            oxygen.CentralNervousSystemFraction, oxygen.OxygenToleranceUnits, violations);
    }

    /// <summary>
    /// Builds a travel segment between two depths at a given rate, breathing the gas
    /// selected for the depth at which the travel ends.
    /// </summary>
    /// <param name="fromDepthMeter">The depth, in meters, at which the travel begins.</param>
    /// <param name="toDepthMeter">The depth, in meters, at which the travel ends.</param>
    /// <param name="rateMetersPerMinute">The vertical rate of the travel, in meters per minute.</param>
    /// <param name="kind">The role of the travel segment within the dive.</param>
    /// <param name="cylinders">The cylinders available to the diver.</param>
    /// <param name="settings">The settings supplying the environment and the bottom oxygen partial pressure limit.</param>
    /// <returns>The travel segment.</returns>
    private static DiveSegment BuildTravel(double fromDepthMeter,
        double toDepthMeter,
        double rateMetersPerMinute,
        SegmentKind kind,
        IReadOnlyList<Cylinder> cylinders,
        DivePlanSettings settings)
    {
        var cylinder = SelectBottomGas(cylinders, toDepthMeter, settings);
        var travelMeters = Math.Abs(toDepthMeter - fromDepthMeter);
        var duration = TimeSpan.FromMinutes(travelMeters / rateMetersPerMinute);
        return new DiveSegment(Depth.FromMeter(toDepthMeter), duration, cylinder.Gas, kind);
    }

    /// <summary>
    /// Selects the richest breathing gas for a working-phase segment at the given depth,
    /// within the bottom oxygen partial pressure limit.
    /// </summary>
    /// <param name="cylinders">The cylinders available to the diver.</param>
    /// <param name="depthMeter">The depth, in meters, at which the gas is breathed.</param>
    /// <param name="settings">The settings supplying the environment and the bottom oxygen partial pressure limit.</param>
    /// <returns>The cylinder holding the selected gas.</returns>
    private static Cylinder SelectBottomGas(IReadOnlyList<Cylinder> cylinders,
        double depthMeter,
        DivePlanSettings settings)
    {
        var hydrostaticMillibar = PhysicalConstants.HydrostaticPressureMillibar(settings.Salinity, depthMeter);
        var ambient = Pressure.FromMillibar(settings.SurfacePressure.InMillibar + hydrostaticMillibar);
        return GasSelector.SelectRichestGas(cylinders, ambient, settings.BottomPo2);
    }

    /// <summary>Returns the total runtime of the given segments, being the sum of their durations.</summary>
    /// <param name="segments">The segments whose combined duration is required.</param>
    /// <returns>The sum of the durations of every segment.</returns>
    private static TimeSpan TotalRuntime(IEnumerable<DiveSegment> segments)
    {
        var total = TimeSpan.Zero;
        foreach (var segment in segments)
        {
            total += segment.Duration;
        }

        return total;
    }
}