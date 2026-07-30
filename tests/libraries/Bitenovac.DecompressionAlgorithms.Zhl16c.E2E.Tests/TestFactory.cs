using Bitenovac.DecompressionAlgorithms.BuhlmannZhl16c;
using Bitenovac.DecompressionAlgorithms.Core;
using Bitenovac.DecompressionAlgorithms.Core.Environment;
using Bitenovac.DecompressionAlgorithms.Core.Equipment;
using Bitenovac.DecompressionAlgorithms.Core.Planning;
using Bitenovac.DecompressionAlgorithms.Units;

namespace Bitenovac.DecompressionAlgorithms.Zhl16c.E2E.Tests;

/// <summary>
/// Builds the fully specified inputs shared by the real-world planner tests, so that every
/// scenario states only its deviations from a common technical baseline.
/// </summary>
internal static class TestFactory
{
    /// <summary>
    /// Creates the settings for one scenario. The defaults describe a common technical
    /// baseline — sea level at 1 bar, salt water, 18 m/min descent, 9/9/6/3 m/min ascent
    /// bands, SAC 18/14 L/min, pO2 limits 1.4/1.6 bar, 40 bar reserve at stress factor 2
    /// for a two-diver team, one-minute stop rounding, two-minute gas switches, last stop
    /// at six meters — and every test states only its deviations.
    /// </summary>
    public static DivePlanSettings CreateSettings(double surfacePressureMillibar = 1000,
        Salinity salinity = Salinity.Salt,
        double descentRateMetersPerMinute = 18,
        double ascentRateBelow75PercentMetersPerMinute = 9,
        double ascentRate75To50PercentMetersPerMinute = 9,
        double ascentRate50PercentToStopsMetersPerMinute = 6,
        double ascentRateLastSixMetersMetersPerMinute = 3,
        double bottomSacLitersPerMinute = 18,
        double decoSacLitersPerMinute = 14,
        double bottomMetabolicOxygenConsumptionLitersPerMinute = 1,
        double decoMetabolicOxygenConsumptionLitersPerMinute = 0.6,
        double loopVolumeLiters = 6,
        double bottomPo2Bar = 1.4,
        double decoPo2Bar = 1.6,
        double reservePressureBar = 40,
        double reserveStressFactor = 2,
        int reserveTeamSize = 2,
        double stopTimeIncrementMinutes = 1,
        double problemSolvingMinutes = 1,
        double gasSwitchMinutes = 2,
        double oxygenBreakIntervalMinutes = 20,
        double oxygenBreakDurationMinutes = 5,
        bool safetyStop = false,
        bool lastStopAtSixMeters = true,
        bool switchAtRequiredStop = false,
        bool oxygenBreaks = false,
        bool oxygenIsNarcotic = true) =>
        new(
            Pressure.FromMillibar(surfacePressureMillibar),
            salinity,
            descentRateMetersPerMinute,
            ascentRateBelow75PercentMetersPerMinute,
            ascentRate75To50PercentMetersPerMinute,
            ascentRate50PercentToStopsMetersPerMinute,
            ascentRateLastSixMetersMetersPerMinute,
            bottomSacLitersPerMinute,
            decoSacLitersPerMinute,
            bottomMetabolicOxygenConsumptionLitersPerMinute,
            decoMetabolicOxygenConsumptionLitersPerMinute,
            loopVolumeLiters,
            Pressure.FromBar(bottomPo2Bar),
            Pressure.FromBar(decoPo2Bar),
            Pressure.FromBar(reservePressureBar),
            reserveStressFactor,
            reserveTeamSize,
            TimeSpan.FromMinutes(stopTimeIncrementMinutes),
            TimeSpan.FromMinutes(problemSolvingMinutes),
            TimeSpan.FromMinutes(gasSwitchMinutes),
            TimeSpan.FromMinutes(oxygenBreakIntervalMinutes),
            TimeSpan.FromMinutes(oxygenBreakDurationMinutes),
            safetyStop,
            lastStopAtSixMeters,
            switchAtRequiredStop,
            oxygenBreaks,
            oxygenIsNarcotic);

    /// <summary>Creates one cylinder from the mix in percent and the size and fill.</summary>
    public static Cylinder CreateCylinder(double percentO2,
        double percentHe,
        double sizeLiter,
        double startPressureBar,
        CylinderPurpose purpose) =>
        new(GasMixture.FromPercent(percentO2, percentHe), Volume.FromLiter(sizeLiter),
            Pressure.FromBar(startPressureBar), purpose);

    /// <summary>Plans the requested dive with a fresh algorithm instance.</summary>
    public static DecoPlan CreatePlan(DivePlanRequest request,
        double gradientFactorLow,
        double gradientFactorHigh) =>
        new DivePlanner(new BuhlmannZhl16cAlgorithm(gradientFactorLow, gradientFactorHigh))
            .CreatePlan(request);

    public static DiveProfile CreateProfile(params (double DepthMeter, double Minutes)[] levels) =>
        new(levels.Select(static level => new DiveSegment(Depth.FromMeter(level.DepthMeter),
            TimeSpan.FromMinutes(level.Minutes), GasMixture.Air, SegmentKind.Bottom)));

    /// <summary>Builds a profile whose every level is breathed through the same apparatus.</summary>
    public static DiveProfile CreateProfile(BreathingLoop loop,
        params (double DepthMeter, double Minutes)[] levels) =>
        new(levels.Select(level => new DiveSegment(Depth.FromMeter(level.DepthMeter),
            TimeSpan.FromMinutes(level.Minutes), GasMixture.Air, SegmentKind.Bottom, loop)));

    /// <summary>
    /// Builds a profile whose levels are breathed through different apparatus, so that a
    /// bailout onto open circuit part way through a dive can be planned.
    /// </summary>
    public static DiveProfile CreateProfile(
        params (double DepthMeter, double Minutes, BreathingLoop Loop)[] levels) =>
        new(levels.Select(static level => new DiveSegment(Depth.FromMeter(level.DepthMeter),
            TimeSpan.FromMinutes(level.Minutes), GasMixture.Air, SegmentKind.Bottom, level.Loop)));
}