using Bitenovac.DecompressionAlgorithms.Core.Environment;
using Bitenovac.DecompressionAlgorithms.Core.Equipment;
using Bitenovac.DecompressionAlgorithms.Core.Planning;
using Bitenovac.DecompressionAlgorithms.Units;

namespace Bitenovac.DecompressionAlgorithms.Zhl16c.Integration.Tests;

/// <summary>
/// Provides the valid, fully specified inputs shared by the planner integration tests.
/// </summary>
/// <remarks>
/// Settings use fresh water and a surface pressure of exactly one bar, so every ambient pressure
/// is exactly reproducible.
/// </remarks>
internal static class TestFactory
{
    public static DivePlanSettings CreateSettings(bool safetyStop = false) =>
        new(
            Pressure.FromBar(1),
            Salinity.Fresh,
            20,
            10,
            9,
            6,
            3,
            20,
            15,
            1,
            0.6,
            6,
            Pressure.FromBar(1.4),
            Pressure.FromBar(1.6),
            MaximumOperatingDepthModel.Realistic,
            Pressure.FromBar(50),
            1.5,
            2,
            TimeSpan.FromMinutes(1),
            TimeSpan.FromMinutes(1),
            TimeSpan.FromMinutes(1),
            TimeSpan.FromMinutes(20),
            TimeSpan.FromMinutes(5),
            safetyStop,
            false,
            false,
            false,
            false);

    public static Cylinder CreateCylinder(GasMixture? gas = null) =>
        new(gas ?? GasMixture.Air, Volume.FromLiter(24), Pressure.FromBar(200), CylinderPurpose.BottomGas);

    public static DiveProfile CreateProfile(params (double DepthMeter, double Minutes)[] levels) =>
        new(levels.Select(static level => new DiveSegment(Depth.FromMeter(level.DepthMeter),
            TimeSpan.FromMinutes(level.Minutes), GasMixture.Air, SegmentKind.Bottom)));

    /// <summary>
    /// Creates a profile whose every level is breathed through the same apparatus.
    /// </summary>
    public static DiveProfile CreateProfile(BreathingLoop loop,
        params (double DepthMeter, double Minutes)[] levels) =>
        new(levels.Select(level => new DiveSegment(Depth.FromMeter(level.DepthMeter),
            TimeSpan.FromMinutes(level.Minutes), GasMixture.Air, SegmentKind.Bottom, loop)));

    /// <summary>
    /// Creates a profile whose levels are breathed through different apparatus, such as a bailout
    /// onto open circuit part way through a dive.
    /// </summary>
    public static DiveProfile CreateProfile(
        params (double DepthMeter, double Minutes, BreathingLoop Loop)[] levels) =>
        new(levels.Select(static level => new DiveSegment(Depth.FromMeter(level.DepthMeter),
            TimeSpan.FromMinutes(level.Minutes), GasMixture.Air, SegmentKind.Bottom, level.Loop)));
}