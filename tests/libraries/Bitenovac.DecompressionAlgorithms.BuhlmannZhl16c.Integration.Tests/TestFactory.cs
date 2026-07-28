using Bitenovac.DecompressionAlgorithms.Core.Environment;
using Bitenovac.DecompressionAlgorithms.Core.Equipment;
using Bitenovac.DecompressionAlgorithms.Core.Planning;
using Bitenovac.DecompressionAlgorithms.Units;

namespace Bitenovac.DecompressionAlgorithms.Zhl16c.Integration.Tests;

/// <summary>
/// Builds the valid, fully specified inputs shared by the planner integration tests.
/// Fresh water and a surface pressure of exactly one bar are used so that every ambient
/// pressure, and therefore the generated schedule, is exactly reproducible.
/// </summary>
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
            Pressure.FromBar(1.4),
            Pressure.FromBar(1.6),
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
}