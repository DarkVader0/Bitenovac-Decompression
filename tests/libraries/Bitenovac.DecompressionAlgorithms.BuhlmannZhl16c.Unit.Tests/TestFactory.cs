using Bitenovac.DecompressionAlgorithms.Core.Environment;
using Bitenovac.DecompressionAlgorithms.Core.Equipment;
using Bitenovac.DecompressionAlgorithms.Core.Planning;
using Bitenovac.DecompressionAlgorithms.Units;

namespace Bitenovac.DecompressionAlgorithms.Zhl16c.Unit.Tests;

/// <summary>
/// Builds the valid, fully specified inputs shared by the algorithm tests. Fresh water
/// and a surface pressure of exactly one bar are used so that the ambient pressure at a
/// given depth, and therefore every expected value derived from it, is exactly
/// reproducible: ambient(mbar) = 1000 + 98.0665 x depth(m).
/// </summary>
internal static class TestFactory
{
    public const double SurfacePressureMillibar = 1000.0;
    public const double MillibarPerMeter = 98.0665;
    public const double WaterVaporPressureMillibar = 62.7;

    public static DivePlanSettings CreateSettings(
        bool safetyStop = false,
        bool lastStopAtSixMeters = false,
        bool switchAtRequiredStop = false,
        bool oxygenBreaks = false,
        MaximumOperatingDepthModel maximumOperatingDepthModel = MaximumOperatingDepthModel.Realistic,
        Salinity salinity = Salinity.Fresh,
        TimeSpan? stopTimeIncrement = null,
        TimeSpan? minimumGasSwitchDuration = null,
        TimeSpan? oxygenBreakInterval = null,
        TimeSpan? oxygenBreakDuration = null)
    {
        return new DivePlanSettings(
            Pressure.FromBar(1),
            salinity,
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
            maximumOperatingDepthModel,
            Pressure.FromBar(50),
            1.5,
            2,
            stopTimeIncrement ?? TimeSpan.FromMinutes(1),
            TimeSpan.FromMinutes(1),
            minimumGasSwitchDuration ?? TimeSpan.FromMinutes(1),
            oxygenBreakInterval ?? TimeSpan.FromMinutes(20),
            oxygenBreakDuration ?? TimeSpan.FromMinutes(5),
            safetyStop,
            lastStopAtSixMeters,
            switchAtRequiredStop,
            oxygenBreaks,
            false);
    }

    public static Cylinder CreateCylinder(GasMixture? gas = null)
    {
        return new Cylinder(gas ?? GasMixture.Air, Volume.FromLiter(12), Pressure.FromBar(200),
            CylinderPurpose.BottomGas);
    }

    public static DivePlanRequest CreateRequest(
        double depthMeter,
        double bottomMinutes,
        IEnumerable<Cylinder>? cylinders = null,
        DivePlanSettings? settings = null,
        IEnumerable<PriorDive>? priorDives = null)
    {
        return new DivePlanRequest(
            new DiveProfile([
                new DiveSegment(Depth.FromMeter(depthMeter), TimeSpan.FromMinutes(bottomMinutes), GasMixture.Air,
                    SegmentKind.Bottom)
            ]),
            cylinders ?? [CreateCylinder()],
            settings ?? CreateSettings(),
            priorDives);
    }
}