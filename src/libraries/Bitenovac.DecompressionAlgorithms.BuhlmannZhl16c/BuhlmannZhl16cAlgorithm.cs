using Bitenovac.DecompressionAlgorithms.Core.Abstractions;
using Bitenovac.DecompressionAlgorithms.Core.Calculations;
using Bitenovac.DecompressionAlgorithms.Core.Environment;
using Bitenovac.DecompressionAlgorithms.Core.Equipment;
using Bitenovac.DecompressionAlgorithms.Core.Planning;
using Bitenovac.DecompressionAlgorithms.Units;

namespace Bitenovac.DecompressionAlgorithms.BuhlmannZhl16c;

/// <summary>
/// Represents the Bühlmann ZH-L16C dissolved-gas decompression model with gradient factors.
/// </summary>
/// <remarks>
/// <para>
/// Sixteen tissue compartments track dissolved nitrogen and helium. Constant-depth segments load
/// them with the instantaneous (Haldane) exponential, and depth-changing segments with the
/// Schreiner equation. The decompression ceiling is the deepest tolerated ambient pressure over
/// all compartments under the gradient-factor-reduced M-values. All arithmetic is in millibars
/// and meters.
/// </para>
/// <para>
/// Repetitive dives need no stored state. <see cref="BeginDive"/> replays the request's prior
/// dives to reconstruct the residual tissue loading. Each prior dive's working phase, generated
/// final ascent, and surface interval are replayed under that dive's own settings.
/// </para>
/// <para>
/// An instance plans one dive at a time and is not thread-safe. The model state and the returned
/// final-ascent list are pooled and reused, so <see cref="BeginDive"/> invalidates state
/// previously returned by this instance and <see cref="CalculateFinalAscent"/> invalidates its
/// previously returned list. A warmed instance allocates nothing on the heap while planning.
/// </para>
/// <para>
/// The gradient-factor slope is anchored at the first decompression stop: the low factor applies
/// there and the high factor at the surface, interpolated linearly by depth.
/// <see cref="CurrentCeiling"/> reports the ceiling at the low factor.
/// </para>
/// </remarks>
public sealed class BuhlmannZhl16cAlgorithm : IDecompressionAlgorithm
{
    /// <summary>
    /// Bühlmann's alveolar water vapor pressure convention, in millibars.
    /// </summary>
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

    /// <summary>
    /// Initializes a new instance of the <see cref="BuhlmannZhl16cAlgorithm"/> class with the
    /// specified gradient factors.
    /// </summary>
    /// <param name="gradientFactorLow">The gradient factor applied at the first decompression stop, in (0, 1].</param>
    /// <param name="gradientFactorHigh">The gradient factor applied at the surface, in (0, 1].</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="gradientFactorLow"/> or <paramref name="gradientFactorHigh"/> is outside (0, 1], or
    /// <paramref name="gradientFactorLow"/> exceeds <paramref name="gradientFactorHigh"/>.
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
    /// Begins a dive and returns the model state at the surface of the requested dive.
    /// </summary>
    /// <param name="request">
    /// The planning request supplying the environment, the prior dives, and the model inputs.
    /// </param>
    /// <returns>The pooled model state, positioned at the surface of the requested dive.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="request"/> is <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">
    /// A prior dive requires a breathing gas or a decompression schedule that its cylinders cannot
    /// provide.
    /// </exception>
    /// <remarks>
    /// <para>
    /// The tissues are equilibrated to breathing air at the surface pressure of the first dive of
    /// the series. Every prior dive in the request is then replayed: its working phase, its
    /// generated final ascent, and its surface interval, each under that dive's own settings.
    /// Finally, the state is switched to the environment of the requested dive.
    /// </para>
    /// <para>
    /// The returned state is pooled and is invalidated by the next call to this method.
    /// </para>
    /// </remarks>
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
    /// Loads the tissues over a single segment.
    /// </summary>
    /// <param name="state">The state at the start of the segment. It is advanced in place.</param>
    /// <param name="segment">The segment over which to load the tissues.</param>
    /// <returns>The same state instance, advanced to the end of the segment.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="state"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="state"/> was not produced by this model.</exception>
    /// <remarks>
    /// Descent and ascent segments apply the Schreiner equation from the state's current depth to
    /// the segment's end depth. All other segment kinds apply the instantaneous exponential at the
    /// segment's depth.
    /// </remarks>
    public IDecompressionState LoadSegment(IDecompressionState state, DiveSegment segment)
    {
        var buhlmannState = RequireOwnState(state);
        ApplySegment(buhlmannState, segment);
        return buhlmannState;
    }

    /// <summary>
    /// Returns the current decompression ceiling at the low gradient factor.
    /// </summary>
    /// <param name="state">The state at which the ceiling is required.</param>
    /// <returns>
    /// The shallowest depth whose ambient pressure every compartment tolerates, or the surface when
    /// no obligation exists.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="state"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="state"/> was not produced by this model.</exception>
    public Depth CurrentCeiling(IDecompressionState state)
    {
        var buhlmannState = RequireOwnState(state);
        return Depth.FromMeter(Math.Max(0.0, CeilingMeter(buhlmannState, _gradientFactorLow)));
    }

    /// <summary>
    /// Computes the final ascent from the current depth to the surface.
    /// </summary>
    /// <param name="state">The state from which the ascent begins. It is advanced to the surface.</param>
    /// <param name="request">The planning request supplying the cylinders and settings that govern the ascent.</param>
    /// <returns>The ordered, contiguous ascent segments from the current depth to the surface.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="state"/> or <paramref name="request"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="ArgumentException"><paramref name="state"/> was not produced by this model.</exception>
    /// <exception cref="InvalidOperationException">
    /// No available gas is breathable at a required depth, or a decompression stop fails to clear
    /// within twenty-four hours.
    /// </exception>
    /// <remarks>
    /// <para>
    /// The ascent consists of the banded-rate ascent travel, the decompression stops at three-meter
    /// intervals with stop times rounded up to the settings' increment, the gas switches onto the
    /// richest permitted decompression gas, oxygen breaks when the settings enable them, and the
    /// safety stop when one is configured and no decompression stop is required. A closed-circuit
    /// loop is raised to its decompression setpoint for the whole of the ascent.
    /// </para>
    /// <para>
    /// The state is advanced to the surface as a side effect. The returned list is pooled and is
    /// invalidated by the next call to this method.
    /// </para>
    /// </remarks>
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
        _state.CurrentLoop = BreathingLoop.OpenCircuit;
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

        LoadConstantDepth(_state, 0.0, priorDive.SurfaceGas, BreathingLoop.OpenCircuit,
            priorDive.SurfaceInterval.TotalMinutes);
        _state.CurrentGas = priorDive.SurfaceGas;
        _state.CurrentLoop = BreathingLoop.OpenCircuit;
    }

    /// <summary>
    /// Replays the working phase of a prior dive's planned profile.
    /// </summary>
    /// <remarks>
    /// The segments mirror the construction of the shared planner: descents at the descent rate,
    /// inter-level ascents at the deep ascent rate, and bottom time on the richest gas within the
    /// bottom oxygen limit.
    /// </remarks>
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
                var gas = SelectGasAt(cylinders, settings, targetDepthMeter, settings.BottomPo2, target.Loop);
                var travel = new DiveSegment(Depth.FromMeter(targetDepthMeter),
                    TimeSpan.FromMinutes(Math.Abs(targetDepthMeter - currentDepthMeter) / rate),
                    gas,
                    isDescent ? SegmentKind.Descent : SegmentKind.Ascent,
                    target.Loop);
                ApplySegment(_state, travel);
                currentDepthMeter = targetDepthMeter;
            }

            if (target.Duration <= TimeSpan.Zero)
            {
                continue;
            }

            var bottomGas = SelectGasAt(cylinders, settings, targetDepthMeter, settings.BottomPo2, target.Loop);
            var bottom = new DiveSegment(Depth.FromMeter(targetDepthMeter), target.Duration, bottomGas,
                SegmentKind.Bottom, target.Loop);
            ApplySegment(_state, bottom);
        }
    }

    /// <summary>
    /// Selects the richest supply gas breathable at the specified depth within the specified oxygen
    /// limit.
    /// </summary>
    /// <remarks>
    /// A rebreather draws only on its diluent supply, and the limit is applied to the diluent
    /// breathed open circuit, since that is the exposure a flush or a bailout at that depth would
    /// produce.
    /// </remarks>
    private static GasMixture SelectGasAt(IReadOnlyList<Cylinder> cylinders,
        DivePlanSettings settings,
        double depthMeter,
        Pressure maxPo2,
        BreathingLoop loop) =>
        GasSelector.SelectRichestGasAt(cylinders, depthMeter, maxPo2, settings, loop.SupplyPurpose).Gas;

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
                LoadTravel(state, state.CurrentDepthMeter, endDepthMeter, segment.Gas, segment.Loop, minutes);
                state.DepthTimeIntegralMeterMinutes += (state.CurrentDepthMeter + endDepthMeter) / 2.0 * minutes;
            }
            else
            {
                LoadConstantDepth(state, endDepthMeter, segment.Gas, segment.Loop, minutes);
                state.DepthTimeIntegralMeterMinutes += endDepthMeter * minutes;
            }

            state.RuntimeMinutes += minutes;
        }

        state.CurrentDepthMeter = endDepthMeter;
        state.CurrentGas = segment.Gas;
        state.CurrentLoop = segment.Loop;
    }

    /// <summary>
    /// Loads the tissues at a constant depth with the instantaneous exponential.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The loading follows <c>P(t) = Palv + (P0 − Palv)·e^(−k·t)</c> with <c>k = ln 2 / halfTime</c>.
    /// </para>
    /// <para>
    /// The alveolar pressures are those of the gas the apparatus delivers, which on a rebreather
    /// differs from the gas held in the cylinder.
    /// </para>
    /// </remarks>
    private static void LoadConstantDepth(BuhlmannState state,
        double depthMeter,
        GasMixture gas,
        BreathingLoop loop,
        double minutes)
    {
        var ambient = state.SurfacePressureMillibar + state.MillibarPerMeter * depthMeter;
        var alveolar = ambient - WaterVaporPressureMillibar;
        var ambientPressure = Pressure.FromMillibar(ambient);
        var alveolarNitrogen = alveolar * loop.InspiredNitrogenFraction(gas, ambientPressure);
        var alveolarHelium = alveolar * loop.InspiredHeliumFraction(gas, ambientPressure);

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
    /// Loads the tissues over a constant-rate depth change with the Schreiner equation.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The loading follows <c>P(t) = Palv0 + R·(t − 1/k) − (Palv0 − P0 − R/k)·e^(−k·t)</c>, where
    /// <c>Palv0</c> is the alveolar inert pressure at the start depth and <c>R</c> the rate of
    /// change of that pressure.
    /// </para>
    /// <para>
    /// The rate is taken from the alveolar pressures at the two ends of the travel, so it also
    /// covers a rebreather, on which the inspired fractions change with depth while the alveolar
    /// pressures remain linear in time.
    /// </para>
    /// </remarks>
    private static void LoadTravel(BuhlmannState state,
        double fromDepthMeter,
        double toDepthMeter,
        GasMixture gas,
        BreathingLoop loop,
        double minutes)
    {
        var ambientStart = state.SurfacePressureMillibar + state.MillibarPerMeter * fromDepthMeter;
        var ambientEnd = state.SurfacePressureMillibar + state.MillibarPerMeter * toDepthMeter;
        var alveolarStart = ambientStart - WaterVaporPressureMillibar;
        var alveolarEnd = ambientEnd - WaterVaporPressureMillibar;

        var startPressure = Pressure.FromMillibar(ambientStart);
        var endPressure = Pressure.FromMillibar(ambientEnd);

        var nitrogenStart = alveolarStart * loop.InspiredNitrogenFraction(gas, startPressure);
        var nitrogenEnd = alveolarEnd * loop.InspiredNitrogenFraction(gas, endPressure);
        var heliumStart = alveolarStart * loop.InspiredHeliumFraction(gas, startPressure);
        var heliumEnd = alveolarEnd * loop.InspiredHeliumFraction(gas, endPressure);

        LoadTravelGas(ref state.Nitrogen, Zhl16cCoefficients.NitrogenHalfTimeMinutes,
            nitrogenStart, (nitrogenEnd - nitrogenStart) / minutes, minutes);
        LoadTravelGas(ref state.Helium, Zhl16cCoefficients.HeliumHalfTimeMinutes,
            heliumStart, (heliumEnd - heliumStart) / minutes, minutes);
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
    /// Returns the ceiling, in meters, at the specified gradient factor.
    /// </summary>
    /// <remarks>
    /// The ceiling is the deepest tolerated ambient pressure over all compartments,
    /// <c>Ptol = (P − a·gf) / (gf/b + 1 − gf)</c> with <c>a</c> and <c>b</c> weighted by the
    /// compartment's nitrogen and helium loadings, converted to a depth. It is negative when every
    /// compartment tolerates the surface.
    /// </remarks>
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
    /// Plans the final ascent from the state's current depth to the surface.
    /// </summary>
    /// <remarks>
    /// The tissues are loaded along the way. When <paramref name="output"/> is supplied, the
    /// resulting segments are emitted to it.
    /// </remarks>
    private void PlanFinalAscent(BuhlmannState state,
        IReadOnlyList<Cylinder> cylinders,
        DivePlanSettings settings,
        SegmentBuffer? output)
    {
        if (state.CurrentDepthMeter <= DepthToleranceMeter)
        {
            return;
        }

        state.CurrentLoop = state.CurrentLoop.ForDecompression();

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

        var anchorMeter = firstStopMeter;

        if (firstStopMeter >= state.CurrentDepthMeter - DepthToleranceMeter
            || CeilingMeter(state, GradientFactorAt(firstStopMeter, anchorMeter)) > firstStopMeter)
        {
            HoldUntilClear(state, state.CurrentDepthMeter, firstStopMeter,
                GradientFactorAt(firstStopMeter, anchorMeter), cylinders, settings, output);
        }

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

    /// <summary>
    /// Ascends directly to the surface, inserting the configured safety stop when one applies.
    /// </summary>
    private static void AscendWithSafetyStop(BuhlmannState state,
        DivePlanSettings settings,
        double averageDepthMeter,
        SegmentBuffer? output)
    {
        if (settings.SafetyStop && state.CurrentDepthMeter > SafetyStopDepthMeter)
        {
            AscendTo(state, SafetyStopDepthMeter, averageDepthMeter, settings, output);
            var safetyStop = new DiveSegment(Depth.FromMeter(SafetyStopDepthMeter), SafetyStopDuration,
                state.CurrentGas, SegmentKind.Stop, state.CurrentLoop);
            ApplySegment(state, safetyStop);
            output?.Add(safetyStop);
        }

        AscendTo(state, 0.0, averageDepthMeter, settings, output);
    }

    /// <summary>
    /// Ascends from the current depth to the specified stop.
    /// </summary>
    /// <remarks>
    /// When the settings allow switching before a required stop, the ascent pauses to switch onto
    /// a richer decompression gas at its operating depth.
    /// </remarks>
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
    /// Returns the deepest grid depth at which a richer gas becomes breathable on the way to the
    /// target stop.
    /// </summary>
    /// <remarks>
    /// The depth lies strictly between the target stop and the current depth, and is the depth at
    /// which a gas richer than the current one becomes breathable within the decompression oxygen
    /// limit. The result is negative when there is no such depth.
    /// </remarks>
    private static double DeepestSwitchDepth(BuhlmannState state,
        IReadOnlyList<Cylinder> cylinders,
        DivePlanSettings settings,
        double targetStopMeter)
    {
        var best = -1.0;

        var supplyPurpose = state.CurrentLoop.SupplyPurpose;

        for (var i = 0; i < cylinders.Count; i++)
        {
            if (!GasSelector.IsAvailableFor(cylinders[i], supplyPurpose))
            {
                continue;
            }

            var gas = cylinders[i].Gas;
            if (gas.FractionO2 <= state.CurrentGas.FractionO2 + GasFractionTolerance)
            {
                continue;
            }

            var operatingDepthMeter = GasSelector.MaxOperatingDepthMeter(gas, settings.DecoPo2, settings);
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
    /// Switches to the richest decompression gas breathable at the specified depth when it is
    /// richer than the current gas.
    /// </summary>
    /// <remarks>
    /// The switch is emitted as a gas switch segment of the configured duration.
    /// </remarks>
    private static void SwitchGasIfRicher(BuhlmannState state,
        double depthMeter,
        IReadOnlyList<Cylinder> cylinders,
        DivePlanSettings settings,
        SegmentBuffer? output)
    {
        var best = SelectGasAt(cylinders, settings, depthMeter, settings.DecoPo2, state.CurrentLoop);
        if (best.FractionO2 <= state.CurrentGas.FractionO2 + GasFractionTolerance)
        {
            return;
        }

        if (settings.MinimumGasSwitchDuration > TimeSpan.Zero)
        {
            var switchSegment = new DiveSegment(Depth.FromMeter(depthMeter), settings.MinimumGasSwitchDuration,
                best, SegmentKind.GasSwitch, state.CurrentLoop);
            ApplySegment(state, switchSegment);
            output?.Add(switchSegment);
        }
        else
        {
            state.CurrentGas = best;
        }
    }

    /// <summary>
    /// Holds at a stop until every compartment tolerates the next target depth at the specified
    /// gradient factor.
    /// </summary>
    /// <remarks>
    /// The stop is held in increments of the settings' stop time rounding, with oxygen breaks
    /// inserted when the settings enable them. Consecutive increments on the same gas are emitted
    /// as a single stop segment.
    /// </remarks>
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
                var breakGas = RichestNonOxygenGas(cylinders, settings, stopMeter, state.CurrentLoop);
                if (breakGas is { } gas)
                {
                    FlushHeldStop(state, stopDepth, ref heldMinutes, output);

                    var breakSegment = new DiveSegment(stopDepth, settings.OxygenBreakDuration, gas,
                        SegmentKind.Stop, state.CurrentLoop);
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
                SegmentKind.Stop, state.CurrentLoop);
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
            SegmentKind.Stop, state.CurrentLoop));
        heldMinutes = 0.0;
    }

    /// <summary>
    /// Returns the richest gas with an oxygen fraction below one that is breathable within the
    /// decompression oxygen limit at the specified depth.
    /// </summary>
    /// <remarks>
    /// The result is <see langword="null"/> when no such gas is carried.
    /// </remarks>
    private static GasMixture? RichestNonOxygenGas(IReadOnlyList<Cylinder> cylinders,
        DivePlanSettings settings,
        double depthMeter,
        BreathingLoop loop)
    {
        var supplyPurpose = loop.SupplyPurpose;
        GasMixture? best = null;

        for (var i = 0; i < cylinders.Count; i++)
        {
            if (!GasSelector.IsAvailableFor(cylinders[i], supplyPurpose))
            {
                continue;
            }

            var gas = cylinders[i].Gas;
            if (gas.FractionO2 >= PureOxygenFraction
                || !GasSelector.IsBreathableAt(gas, depthMeter, settings.DecoPo2, settings))
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
    /// Ascends from the current depth to the target.
    /// </summary>
    /// <remarks>
    /// The ascent is split at the settings' rate-band boundaries: 75% and 50% of the average depth,
    /// and the final six meters.
    /// </remarks>
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
                SegmentKind.Ascent,
                state.CurrentLoop);
            ApplySegment(state, travel);
            output?.Add(travel);
        }
    }

    /// <summary>
    /// Returns the gradient factor at the specified depth on the slope anchored at the first stop.
    /// </summary>
    /// <remarks>
    /// The low factor applies at the anchor and the high factor at the surface.
    /// </remarks>
    private double GradientFactorAt(double depthMeter, double anchorMeter)
    {
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