using Bitenovac.DecompressionAlgorithms.Core.Abstractions;
using Bitenovac.DecompressionAlgorithms.Core.Calculations;
using Bitenovac.DecompressionAlgorithms.Core.Environment;
using Bitenovac.DecompressionAlgorithms.Core.Equipment;
using Bitenovac.DecompressionAlgorithms.Core.Planning;
using Bitenovac.DecompressionAlgorithms.Units;

namespace Bitenovac.DecompressionAlgorithms.BuhlmannZhl16c;

/// <summary>
/// The Bühlmann ZH-L16C dissolved-gas decompression model with gradient factors. Sixteen
/// tissue compartments track dissolved nitrogen and helium; constant-depth segments load
/// the tissues with the instantaneous (Haldane) exponential and depth-changing segments
/// with the Schreiner equation; the decompression ceiling is the deepest tolerated
/// ambient pressure over all compartments under the gradient-factor-reduced M-values.
/// Repetitive dives are supported without stored state: <see cref="BeginDive" /> replays
/// the request's prior dives — working phase, generated final ascent, and surface
/// interval, each under that dive's own settings — to reconstruct the residual tissue
/// loading deterministically from pure inputs. All internal arithmetic is performed in
/// millibars and meters, the canonical units of <see cref="Pressure" /> and
/// <see cref="Depth" />.
/// </summary>
/// <remarks>
/// <para>
/// An instance plans one dive at a time and is not thread-safe: the model state and the
/// returned final-ascent list are pooled and reused, so <see cref="BeginDive" />
/// invalidates any state previously returned by this instance, and
/// <see cref="CalculateFinalAscent" /> invalidates its previously returned list. In
/// return, a warmed instance allocates no heap memory while planning.
/// </para>
/// <para>
/// The gradient-factor slope is anchored at the first decompression stop: the low factor
/// applies at the first stop and the high factor at the surface, interpolated linearly by
/// depth. <see cref="CurrentCeiling" /> reports the ceiling at the low factor, the
/// conservative bound appropriate while the working phase is still in progress.
/// </para>
/// </remarks>
public sealed class BuhlmannZhl16cAlgorithm : IDecompressionAlgorithm
{
    /// <summary>Bühlmann's alveolar water vapor pressure convention, in millibars.</summary>
    private const double WaterVaporPressureMillibar = 62.7;

    private const double Ln2 = 0.6931471805599453;
    private const int StopIntervalMeter = 3;
    private const int ShallowStopDepthMeter = 3;
    private const int SixMeterStopDepthMeter = 6;
    private const double LastBandBoundaryMeter = 6.0;
    private const double SafetyStopDepthMeter = 5.0;
    private const double DepthToleranceMeter = 1e-9;
    private const double GasFractionTolerance = 1e-9;
    private const double PureOxygenFraction = 0.999;
    private const double MaximumStopMinutes = 1440.0;
    private static readonly TimeSpan SafetyStopDuration = TimeSpan.FromMinutes(3);
    private readonly SegmentBuffer _finalAscent = [];
    private readonly double _gradientFactorHigh;

    private readonly double _gradientFactorLow;
    private readonly BuhlmannState _state = new();

    /// <summary>Initializes a new instance of the <see cref="BuhlmannZhl16cAlgorithm" /> class.</summary>
    /// <param name="gradientFactorLow">The gradient factor applied at the first decompression stop, in (0, 1].</param>
    /// <param name="gradientFactorHigh">The gradient factor applied at the surface, in (0, 1].</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="gradientFactorLow" /> or <paramref name="gradientFactorHigh" /> is outside (0, 1], or
    /// <paramref name="gradientFactorLow" /> exceeds <paramref name="gradientFactorHigh" />.
    /// </exception>
    public BuhlmannZhl16cAlgorithm(double gradientFactorLow, double gradientFactorHigh)
    {
        if (gradientFactorLow is <= 0.0 or > 1.0)
        {
            throw new ArgumentOutOfRangeException(nameof(gradientFactorLow), gradientFactorLow,
                "The low gradient factor must be greater than zero and at most one.");
        }

        if (gradientFactorHigh is <= 0.0 or > 1.0)
        {
            throw new ArgumentOutOfRangeException(nameof(gradientFactorHigh), gradientFactorHigh,
                "The high gradient factor must be greater than zero and at most one.");
        }

        if (gradientFactorLow > gradientFactorHigh)
        {
            throw new ArgumentOutOfRangeException(nameof(gradientFactorLow), gradientFactorLow,
                "The low gradient factor must not exceed the high gradient factor.");
        }

        _gradientFactorLow = gradientFactorLow;
        _gradientFactorHigh = gradientFactorHigh;
    }

    /// <summary>
    /// Begins a dive: the tissues are equilibrated to breathing air at the surface
    /// pressure of the first dive of the series, every prior dive in the request is
    /// replayed — its working phase, its generated final ascent, and its surface interval,
    /// each under that dive's own settings — and the state is switched to the environment
    /// of the requested dive.
    /// </summary>
    /// <param name="request">The planning request supplying the environment, the prior dives, and the model inputs.</param>
    /// <returns>The pooled model state, positioned at the surface of the requested dive.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="request" /> is <see langword="null" />.</exception>
    /// <exception cref="InvalidOperationException">
    /// A prior dive requires a breathing gas or a decompression schedule that its
    /// cylinders cannot provide.
    /// </exception>
    /// <remarks>The returned state is pooled: it is invalidated by the next call to this method.</remarks>
    public IDecompressionState BeginDive(DivePlanRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var priorDives = request.PriorDives;
        var initialSettings = priorDives.Count > 0 ? priorDives[0].Settings : request.Settings;

        SetEnvironment(initialSettings);
        EquilibrateToSurface();

        for (var i = 0; i < priorDives.Count; i++)
        {
            ReplayPriorDive(priorDives[i]);
        }

        SetEnvironment(request.Settings);
        ResetDiveTracking();
        return _state;
    }

    /// <summary>
    /// Loads the tissues over a single segment. Descent and ascent segments apply the
    /// Schreiner equation from the state's current depth to the segment's end depth; all
    /// other segment kinds apply the instantaneous exponential at the segment's depth.
    /// </summary>
    /// <param name="state">The state at the start of the segment; it is advanced in place.</param>
    /// <param name="segment">The segment over which to load the tissues.</param>
    /// <returns>The same state instance, advanced to the end of the segment.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="state" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentException"><paramref name="state" /> was not produced by this model.</exception>
    public IDecompressionState LoadSegment(IDecompressionState state, DiveSegment segment)
    {
        var buhlmannState = RequireOwnState(state);
        ApplySegment(buhlmannState, segment);
        return buhlmannState;
    }

    /// <summary>
    /// Returns the current decompression ceiling at the low gradient factor, being the
    /// shallowest depth whose ambient pressure every compartment tolerates.
    /// </summary>
    /// <param name="state">The state at which the ceiling is required.</param>
    /// <returns>The shallowest permissible depth; the surface when no obligation exists.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="state" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentException"><paramref name="state" /> was not produced by this model.</exception>
    public Depth CurrentCeiling(IDecompressionState state)
    {
        var buhlmannState = RequireOwnState(state);
        return Depth.FromMeter(Math.Max(0.0, CeilingMeter(buhlmannState, _gradientFactorLow)));
    }

    /// <summary>
    /// Computes the final ascent to the surface: the banded-rate ascent travel, the
    /// decompression stops at three-meter intervals with stop times rounded up to the
    /// settings' increment, the gas switches onto the richest permitted decompression gas,
    /// oxygen breaks when the settings enable them, and the safety stop when one is
    /// configured and no decompression stop is required. The state is advanced to the
    /// surface as a side effect.
    /// </summary>
    /// <param name="state">The state from which the ascent begins; it is advanced to the surface.</param>
    /// <param name="request">The planning request supplying the cylinders and settings that govern the ascent.</param>
    /// <returns>The ordered, contiguous ascent segments from the current depth to the surface.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="state" /> or <paramref name="request" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException"><paramref name="state" /> was not produced by this model.</exception>
    /// <exception cref="InvalidOperationException">
    /// No available gas is breathable at a required depth, or a decompression stop fails
    /// to clear within twenty-four hours.
    /// </exception>
    /// <remarks>The returned list is pooled: it is invalidated by the next call to this method.</remarks>
    public IReadOnlyList<DiveSegment> CalculateFinalAscent(IDecompressionState state, DivePlanRequest request)
    {
        var buhlmannState = RequireOwnState(state);
        ArgumentNullException.ThrowIfNull(request);

        _finalAscent.Clear();
        PlanFinalAscent(buhlmannState, request.Cylinders, request.Settings, _finalAscent);
        return _finalAscent;
    }

    private BuhlmannState RequireOwnState(IDecompressionState state)
    {
        ArgumentNullException.ThrowIfNull(state);

        if (!ReferenceEquals(state, _state))
        {
            throw new ArgumentException("The state was not produced by this model instance.", nameof(state));
        }

        return _state;
    }

    private void SetEnvironment(DivePlanSettings settings)
    {
        _state.SurfacePressureMillibar = settings.SurfacePressure.InMillibar;
        _state.MillibarPerMeter = PhysicalConstants.HydrostaticPressureMillibar(settings.Salinity, 1.0);
    }

    private void EquilibrateToSurface()
    {
        var air = GasMixture.Air;
        var alveolarNitrogen = (_state.SurfacePressureMillibar - WaterVaporPressureMillibar) * air.FractionN2;

        for (var i = 0; i < Zhl16cCoefficients.CompartmentCount; i++)
        {
            _state.Nitrogen[i] = alveolarNitrogen;
            _state.Helium[i] = 0.0;
        }

        _state.CurrentGas = air;
        ResetDiveTracking();
    }

    private void ResetDiveTracking()
    {
        _state.CurrentDepthMeter = 0.0;
        _state.RuntimeMinutes = 0.0;
        _state.DepthTimeIntegralMeterMinutes = 0.0;
    }

    private void ReplayPriorDive(PriorDive priorDive)
    {
        SetEnvironment(priorDive.Settings);
        ResetDiveTracking();
        ReplayWorkingPhase(priorDive.Profile, priorDive.Cylinders, priorDive.Settings);
        PlanFinalAscent(_state, priorDive.Cylinders, priorDive.Settings, null);

        LoadConstantDepth(_state, 0.0, priorDive.SurfaceGas, priorDive.SurfaceInterval.TotalMinutes);
        _state.CurrentGas = priorDive.SurfaceGas;
    }

    /// <summary>
    /// Replays the working phase of a prior dive's planned profile, mirroring the segment
    /// construction of the shared planner: descents at the descent rate, inter-level
    /// ascents at the deep ascent rate, and bottom time on the richest gas within the
    /// bottom oxygen limit.
    /// </summary>
    private void ReplayWorkingPhase(DiveProfile profile,
        IReadOnlyList<Cylinder> cylinders,
        DivePlanSettings settings)
    {
        var segments = profile.Segments;
        var currentDepthMeter = 0.0;

        for (var i = 0; i < segments.Count; i++)
        {
            var target = segments[i];
            var targetDepthMeter = target.Depth.InMeter;

            if (Math.Abs(targetDepthMeter - currentDepthMeter) > DepthToleranceMeter)
            {
                var isDescent = targetDepthMeter > currentDepthMeter;
                var rate = isDescent
                    ? settings.DescentRateMetersPerMinute
                    : settings.AscentRateBelow75PercentMetersPerMinute;
                var gas = SelectGasAt(cylinders, settings, targetDepthMeter, settings.BottomPo2);
                var travel = new DiveSegment(Depth.FromMeter(targetDepthMeter),
                    TimeSpan.FromMinutes(Math.Abs(targetDepthMeter - currentDepthMeter) / rate),
                    gas,
                    isDescent ? SegmentKind.Descent : SegmentKind.Ascent);
                ApplySegment(_state, travel);
                currentDepthMeter = targetDepthMeter;
            }

            if (target.Duration <= TimeSpan.Zero)
            {
                continue;
            }

            var bottomGas = SelectGasAt(cylinders, settings, targetDepthMeter, settings.BottomPo2);
            var bottom = new DiveSegment(Depth.FromMeter(targetDepthMeter), target.Duration, bottomGas,
                SegmentKind.Bottom);
            ApplySegment(_state, bottom);
        }
    }

    private static GasMixture SelectGasAt(IReadOnlyList<Cylinder> cylinders,
        DivePlanSettings settings,
        double depthMeter,
        Pressure maxPo2)
    {
        var ambient = AmbientConditions.PressureAtDepth(settings, Depth.FromMeter(depthMeter));
        return GasSelector.SelectRichestGas(cylinders, ambient, maxPo2).Gas;
    }

    /// <summary>
    /// Advances the tissues, the depth-time tally, the current depth, and the current gas
    /// over one segment.
    /// </summary>
    private static void ApplySegment(BuhlmannState state, in DiveSegment segment)
    {
        var minutes = segment.Duration.TotalMinutes;
        var endDepthMeter = segment.Depth.InMeter;

        if (minutes > 0.0)
        {
            if (segment.Kind is SegmentKind.Descent or SegmentKind.Ascent)
            {
                LoadTravel(state, state.CurrentDepthMeter, endDepthMeter, segment.Gas, minutes);
                state.DepthTimeIntegralMeterMinutes += (state.CurrentDepthMeter + endDepthMeter) / 2.0 * minutes;
            }
            else
            {
                LoadConstantDepth(state, endDepthMeter, segment.Gas, minutes);
                state.DepthTimeIntegralMeterMinutes += endDepthMeter * minutes;
            }

            state.RuntimeMinutes += minutes;
        }

        state.CurrentDepthMeter = endDepthMeter;
        state.CurrentGas = segment.Gas;
    }

    /// <summary>
    /// Loads the tissues at a constant depth with the instantaneous exponential:
    /// <c>P(t) = Palv + (P0 − Palv)·e^(−k·t)</c> with <c>k = ln 2 / halfTime</c>.
    /// </summary>
    private static void LoadConstantDepth(BuhlmannState state,
        double depthMeter,
        GasMixture gas,
        double minutes)
    {
        var ambient = state.SurfacePressureMillibar + state.MillibarPerMeter * depthMeter;
        var alveolar = ambient - WaterVaporPressureMillibar;
        var alveolarNitrogen = alveolar * gas.FractionN2;
        var alveolarHelium = alveolar * gas.FractionHe;

        var nitrogenHalfTimes = Zhl16cCoefficients.NitrogenHalfTimeMinutes;
        var heliumHalfTimes = Zhl16cCoefficients.HeliumHalfTimeMinutes;

        for (var i = 0; i < Zhl16cCoefficients.CompartmentCount; i++)
        {
            state.Nitrogen[i] = alveolarNitrogen
                                + (state.Nitrogen[i] - alveolarNitrogen)
                                * Math.Exp(-Ln2 * minutes / nitrogenHalfTimes[i]);
            state.Helium[i] = alveolarHelium
                              + (state.Helium[i] - alveolarHelium)
                              * Math.Exp(-Ln2 * minutes / heliumHalfTimes[i]);
        }
    }

    /// <summary>
    /// Loads the tissues over a constant-rate depth change with the Schreiner equation:
    /// <c>P(t) = Palv0 + R·(t − 1/k) − (Palv0 − P0 − R/k)·e^(−k·t)</c>, where
    /// <c>Palv0</c> is the alveolar inert pressure at the start depth and <c>R</c> the
    /// rate of change of that pressure.
    /// </summary>
    private static void LoadTravel(BuhlmannState state,
        double fromDepthMeter,
        double toDepthMeter,
        GasMixture gas,
        double minutes)
    {
        var ambientStart = state.SurfacePressureMillibar + state.MillibarPerMeter * fromDepthMeter;
        var alveolarStart = ambientStart - WaterVaporPressureMillibar;
        var ambientRate = state.MillibarPerMeter * (toDepthMeter - fromDepthMeter) / minutes;

        LoadTravelGas(ref state.Nitrogen, Zhl16cCoefficients.NitrogenHalfTimeMinutes,
            alveolarStart * gas.FractionN2, ambientRate * gas.FractionN2, minutes);
        LoadTravelGas(ref state.Helium, Zhl16cCoefficients.HeliumHalfTimeMinutes,
            alveolarStart * gas.FractionHe, ambientRate * gas.FractionHe, minutes);
    }

    private static void LoadTravelGas(ref TissuePressuresMillibar pressures,
        ReadOnlySpan<double> halfTimes,
        double alveolarStart,
        double alveolarRate,
        double minutes)
    {
        for (var i = 0; i < Zhl16cCoefficients.CompartmentCount; i++)
        {
            var k = Ln2 / halfTimes[i];
            pressures[i] = alveolarStart + alveolarRate * (minutes - 1.0 / k)
                           - (alveolarStart - pressures[i] - alveolarRate / k)
                           * Math.Exp(-k * minutes);
        }
    }

    /// <summary>
    /// Returns the ceiling in meters at the given gradient factor: the deepest tolerated
    /// ambient pressure over all compartments, <c>Ptol = (P − a·gf) / (gf/b + 1 − gf)</c>
    /// with <c>a</c> and <c>b</c> weighted by the compartment's nitrogen and helium
    /// loadings, converted to a depth. Negative when every compartment tolerates the
    /// surface.
    /// </summary>
    private static double CeilingMeter(BuhlmannState state, double gradientFactor)
    {
        var nitrogenA = Zhl16cCoefficients.NitrogenAMillibar;
        var nitrogenB = Zhl16cCoefficients.NitrogenB;
        var heliumA = Zhl16cCoefficients.HeliumAMillibar;
        var heliumB = Zhl16cCoefficients.HeliumB;

        var maxToleratedMillibar = double.MinValue;

        for (var i = 0; i < Zhl16cCoefficients.CompartmentCount; i++)
        {
            var nitrogen = state.Nitrogen[i];
            var helium = state.Helium[i];
            var total = nitrogen + helium;

            var a = (nitrogenA[i] * nitrogen + heliumA[i] * helium) / total;
            var b = (nitrogenB[i] * nitrogen + heliumB[i] * helium) / total;

            var tolerated = (total - a * gradientFactor) / (gradientFactor / b + 1.0 - gradientFactor);
            if (tolerated > maxToleratedMillibar)
            {
                maxToleratedMillibar = tolerated;
            }
        }

        return (maxToleratedMillibar - state.SurfacePressureMillibar) / state.MillibarPerMeter;
    }

    /// <summary>
    /// Plans the final ascent from the state's current depth to the surface, loading the
    /// tissues as it goes and, when <paramref name="output" /> is supplied, emitting the
    /// resulting segments.
    /// </summary>
    private void PlanFinalAscent(BuhlmannState state,
        IReadOnlyList<Cylinder> cylinders,
        DivePlanSettings settings,
        SegmentBuffer? output)
    {
        if (state.CurrentDepthMeter <= DepthToleranceMeter)
        {
            return;
        }

        var averageDepthMeter = state.RuntimeMinutes > 0.0
            ? state.DepthTimeIntegralMeterMinutes / state.RuntimeMinutes
            : state.CurrentDepthMeter;

        var lastStopMeter = settings.LastStopAtSixMeters ? SixMeterStopDepthMeter : ShallowStopDepthMeter;
        var ceilingMeter = CeilingMeter(state, _gradientFactorLow);
        var requiresStops = ceilingMeter > DepthToleranceMeter;

        if (!requiresStops)
        {
            AscendWithSafetyStop(state, settings, averageDepthMeter, output);
            return;
        }

        var firstStopMeter = Math.Max(CeilToStopGrid(ceilingMeter), lastStopMeter);
        var entryStopMeter = FloorToStopGrid(state.CurrentDepthMeter);
        if (firstStopMeter > entryStopMeter)
        {
            firstStopMeter = Math.Max(entryStopMeter, StopIntervalMeter);
        }

        // The gradient-factor slope is anchored at the first stop: gfLow there, gfHigh at
        // the surface.
        var anchorMeter = firstStopMeter;

        // Hold at the current depth first if even the first stop is not yet tolerated.
        if (firstStopMeter >= state.CurrentDepthMeter - DepthToleranceMeter
            || CeilingMeter(state, GradientFactorAt(firstStopMeter, anchorMeter)) > firstStopMeter)
        {
            HoldUntilClear(state, state.CurrentDepthMeter, firstStopMeter,
                GradientFactorAt(firstStopMeter, anchorMeter), cylinders, settings, output);
        }

        // Descend the stop ladder: travel to each stop, switch gas, hold until the next
        // stop is tolerated.
        var stopMeter = (double)firstStopMeter;
        while (state.CurrentDepthMeter > DepthToleranceMeter)
        {
            AscendThroughSwitches(state, stopMeter, averageDepthMeter, cylinders, settings, output);

            if (stopMeter <= DepthToleranceMeter)
            {
                break;
            }

            SwitchGasIfRicher(state, stopMeter, cylinders, settings, output);

            var nextStopMeter = NextStopBelow(stopMeter, lastStopMeter);
            HoldUntilClear(state, stopMeter, nextStopMeter,
                GradientFactorAt(nextStopMeter, anchorMeter), cylinders, settings, output);
            stopMeter = nextStopMeter;
        }
    }

    /// <summary>Ascends directly to the surface, inserting the configured safety stop when one applies.</summary>
    private static void AscendWithSafetyStop(BuhlmannState state,
        DivePlanSettings settings,
        double averageDepthMeter,
        SegmentBuffer? output)
    {
        if (settings.SafetyStop && state.CurrentDepthMeter > SafetyStopDepthMeter)
        {
            AscendTo(state, SafetyStopDepthMeter, averageDepthMeter, settings, output);
            var safetyStop = new DiveSegment(Depth.FromMeter(SafetyStopDepthMeter), SafetyStopDuration,
                state.CurrentGas, SegmentKind.Stop);
            ApplySegment(state, safetyStop);
            output?.Add(safetyStop);
        }

        AscendTo(state, 0.0, averageDepthMeter, settings, output);
    }

    /// <summary>
    /// Ascends from the current depth to the given stop, pausing to switch onto a richer
    /// decompression gas at its operating depth when the settings allow switching before
    /// a required stop.
    /// </summary>
    private static void AscendThroughSwitches(BuhlmannState state,
        double stopMeter,
        double averageDepthMeter,
        IReadOnlyList<Cylinder> cylinders,
        DivePlanSettings settings,
        SegmentBuffer? output)
    {
        if (!settings.SwitchAtRequiredStop)
        {
            while (state.CurrentDepthMeter - stopMeter > DepthToleranceMeter)
            {
                var switchMeter = DeepestSwitchDepth(state, cylinders, settings, stopMeter);
                if (switchMeter < 0.0)
                {
                    break;
                }

                AscendTo(state, switchMeter, averageDepthMeter, settings, output);
                SwitchGasIfRicher(state, switchMeter, cylinders, settings, output);
            }
        }

        AscendTo(state, stopMeter, averageDepthMeter, settings, output);
    }

    /// <summary>
    /// Returns the deepest grid depth strictly between the target stop and the current
    /// depth at which a gas richer than the current one becomes breathable within the
    /// decompression oxygen limit, or a negative value when there is none.
    /// </summary>
    private static double DeepestSwitchDepth(BuhlmannState state,
        IReadOnlyList<Cylinder> cylinders,
        DivePlanSettings settings,
        double targetStopMeter)
    {
        var best = -1.0;

        for (var i = 0; i < cylinders.Count; i++)
        {
            var gas = cylinders[i].Gas;
            if (gas.FractionO2 <= state.CurrentGas.FractionO2 + GasFractionTolerance)
            {
                continue;
            }

            var operatingPressure = GasSelector.MaxOperatingPressure(gas, settings.DecoPo2);
            var operatingDepthMeter = AmbientConditions.DepthAtPressure(settings, operatingPressure).InMeter;
            double switchMeter = FloorToStopGrid(operatingDepthMeter);

            if (switchMeter > targetStopMeter + DepthToleranceMeter
                && switchMeter < state.CurrentDepthMeter - DepthToleranceMeter
                && switchMeter > best)
            {
                best = switchMeter;
            }
        }

        return best;
    }

    /// <summary>
    /// Switches to the richest decompression gas breathable at the given depth when it is
    /// richer than the current gas, emitting a gas switch segment of the configured
    /// duration.
    /// </summary>
    private static void SwitchGasIfRicher(BuhlmannState state,
        double depthMeter,
        IReadOnlyList<Cylinder> cylinders,
        DivePlanSettings settings,
        SegmentBuffer? output)
    {
        var best = SelectGasAt(cylinders, settings, depthMeter, settings.DecoPo2);
        if (best.FractionO2 <= state.CurrentGas.FractionO2 + GasFractionTolerance)
        {
            return;
        }

        if (settings.MinimumGasSwitchDuration > TimeSpan.Zero)
        {
            var switchSegment = new DiveSegment(Depth.FromMeter(depthMeter), settings.MinimumGasSwitchDuration,
                best, SegmentKind.GasSwitch);
            ApplySegment(state, switchSegment);
            output?.Add(switchSegment);
        }
        else
        {
            state.CurrentGas = best;
        }
    }

    /// <summary>
    /// Holds at a stop, in increments of the settings' stop time rounding, until every
    /// compartment tolerates the next target depth at the given gradient factor,
    /// inserting oxygen breaks when the settings enable them. Consecutive increments on
    /// the same gas are emitted as a single stop segment.
    /// </summary>
    private static void HoldUntilClear(BuhlmannState state,
        double stopMeter,
        double targetMeter,
        double allowedGradientFactor,
        IReadOnlyList<Cylinder> cylinders,
        DivePlanSettings settings,
        SegmentBuffer? output)
    {
        var incrementMinutes = settings.StopTimeIncrement.TotalMinutes;
        var heldMinutes = 0.0;
        var oxygenMinutes = 0.0;
        var totalMinutes = 0.0;
        var stopDepth = Depth.FromMeter(stopMeter);

        while (CeilingMeter(state, allowedGradientFactor) > targetMeter + DepthToleranceMeter)
        {
            if (settings.OxygenBreaks
                && state.CurrentGas.FractionO2 >= PureOxygenFraction
                && oxygenMinutes >= settings.OxygenBreakInterval.TotalMinutes)
            {
                var breakGas = RichestNonOxygenGas(cylinders, settings, stopMeter);
                if (breakGas is { } gas)
                {
                    FlushHeldStop(state, stopDepth, ref heldMinutes, output);

                    var breakSegment = new DiveSegment(stopDepth, settings.OxygenBreakDuration, gas,
                        SegmentKind.Stop);
                    var oxygenGas = state.CurrentGas;
                    ApplySegment(state, breakSegment);
                    state.CurrentGas = oxygenGas;

                    output?.Add(breakSegment);
                    totalMinutes += settings.OxygenBreakDuration.TotalMinutes;
                    oxygenMinutes = 0.0;
                    continue;
                }
            }

            var increment = new DiveSegment(stopDepth, settings.StopTimeIncrement, state.CurrentGas,
                SegmentKind.Stop);
            ApplySegment(state, increment);
            heldMinutes += incrementMinutes;
            oxygenMinutes += incrementMinutes;
            totalMinutes += incrementMinutes;

            if (totalMinutes > MaximumStopMinutes)
            {
                throw new InvalidOperationException(
                    $"The decompression stop at {stopMeter} m does not clear within 24 hours; " +
                    "the gradient factors and available gases cannot decompress this dive.");
            }
        }

        FlushHeldStop(state, stopDepth, ref heldMinutes, output);
    }

    private static void FlushHeldStop(BuhlmannState state,
        Depth stopDepth,
        ref double heldMinutes,
        SegmentBuffer? output)
    {
        if (heldMinutes <= 0.0)
        {
            return;
        }

        output?.Add(new DiveSegment(stopDepth, TimeSpan.FromMinutes(heldMinutes), state.CurrentGas,
            SegmentKind.Stop));
        heldMinutes = 0.0;
    }

    /// <summary>
    /// Returns the richest gas with an oxygen fraction below one that is breathable
    /// within the decompression oxygen limit at the given depth, or <see langword="null" />
    /// when no such gas is carried.
    /// </summary>
    private static GasMixture? RichestNonOxygenGas(IReadOnlyList<Cylinder> cylinders,
        DivePlanSettings settings,
        double depthMeter)
    {
        var ambient = AmbientConditions.PressureAtDepth(settings, Depth.FromMeter(depthMeter));
        GasMixture? best = null;

        for (var i = 0; i < cylinders.Count; i++)
        {
            var gas = cylinders[i].Gas;
            if (gas.FractionO2 >= PureOxygenFraction
                || gas.PartialPressureO2(ambient).InMillibar > settings.DecoPo2.InMillibar)
            {
                continue;
            }

            if (best is null || gas.FractionO2 > best.Value.FractionO2)
            {
                best = gas;
            }
        }

        return best;
    }

    /// <summary>
    /// Ascends from the current depth to the target, split at the settings' rate-band
    /// boundaries: 75% and 50% of the average depth, and the final six meters.
    /// </summary>
    private static void AscendTo(BuhlmannState state,
        double targetMeter,
        double averageDepthMeter,
        DivePlanSettings settings,
        SegmentBuffer? output)
    {
        var band75Meter = 0.75 * averageDepthMeter;
        var band50Meter = 0.5 * averageDepthMeter;

        while (state.CurrentDepthMeter - targetMeter > DepthToleranceMeter)
        {
            var depth = state.CurrentDepthMeter;
            double rate;
            double boundaryMeter;

            if (depth > LastBandBoundaryMeter)
            {
                if (band75Meter > LastBandBoundaryMeter && depth > band75Meter + DepthToleranceMeter)
                {
                    rate = settings.AscentRateBelow75PercentMetersPerMinute;
                    boundaryMeter = Math.Max(targetMeter, band75Meter);
                }
                else if (band50Meter > LastBandBoundaryMeter && depth > band50Meter + DepthToleranceMeter)
                {
                    rate = settings.AscentRate75To50PercentMetersPerMinute;
                    boundaryMeter = Math.Max(targetMeter, band50Meter);
                }
                else
                {
                    rate = settings.AscentRate50PercentToStopsMetersPerMinute;
                    boundaryMeter = Math.Max(targetMeter, LastBandBoundaryMeter);
                }
            }
            else
            {
                rate = settings.AscentRateLastSixMetersMetersPerMinute;
                boundaryMeter = targetMeter;
            }

            var travel = new DiveSegment(Depth.FromMeter(boundaryMeter),
                TimeSpan.FromMinutes((depth - boundaryMeter) / rate),
                state.CurrentGas,
                SegmentKind.Ascent);
            ApplySegment(state, travel);
            output?.Add(travel);
        }
    }

    /// <summary>
    /// Returns the gradient factor at the given depth on the slope anchored at the first
    /// stop: the low factor at the anchor, the high factor at the surface.
    /// </summary>
    private double GradientFactorAt(double depthMeter, double anchorMeter)
    {
        // The anchor is a stop on the three-meter grid, never shallower than the stop
        // interval, so the division is always well defined.
        var fraction = Math.Clamp(depthMeter / anchorMeter, 0.0, 1.0);
        return _gradientFactorHigh - (_gradientFactorHigh - _gradientFactorLow) * fraction;
    }

    private static double NextStopBelow(double stopMeter, int lastStopMeter) =>
        stopMeter <= lastStopMeter + DepthToleranceMeter ? 0.0 : stopMeter - StopIntervalMeter;

    private static int CeilToStopGrid(double depthMeter) =>
        (int)Math.Ceiling((depthMeter - DepthToleranceMeter) / StopIntervalMeter) * StopIntervalMeter;

    private static int FloorToStopGrid(double depthMeter) =>
        (int)Math.Floor((depthMeter + DepthToleranceMeter) / StopIntervalMeter) * StopIntervalMeter;
}