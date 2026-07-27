using Bitenovac.DecompressionAlgorithms.Core.Environment;
using Bitenovac.DecompressionAlgorithms.Core.Equipment;
using Bitenovac.DecompressionAlgorithms.Core.Planning;
using Bitenovac.DecompressionAlgorithms.Units;

namespace Bitenovac.DecompressionAlgorithms.Core.Unit.Tests;

/// <summary>
/// Builds the valid, fully specified inputs shared by the calculation tests. Fresh water
/// and a surface pressure of exactly one bar are used by default so that the ambient
/// pressure at a given depth, and therefore every expected value derived from it, is
/// exactly reproducible.
/// </summary>
internal static class TestFactory
{
    public static DivePlanSettings CreateSettings(
        Pressure? surfacePressure = null,
        Salinity salinity = Salinity.Fresh,
        double descentRateMetersPerMinute = 20,
        double ascentRateBelow75PercentMetersPerMinute = 10,
        double ascentRate75To50PercentMetersPerMinute = 9,
        double ascentRate50PercentToStopsMetersPerMinute = 6,
        double ascentRateLastSixMetersMetersPerMinute = 1,
        double bottomSacLitersPerMinute = 20,
        double decoSacLitersPerMinute = 15,
        Pressure? bottomPo2 = null,
        Pressure? decoPo2 = null,
        double reserveStressFactor = 1.5,
        int reserveTeamSize = 2) =>
        new(
            surfacePressure ?? Pressure.FromBar(1),
            salinity,
            descentRateMetersPerMinute,
            ascentRateBelow75PercentMetersPerMinute,
            ascentRate75To50PercentMetersPerMinute,
            ascentRate50PercentToStopsMetersPerMinute,
            ascentRateLastSixMetersMetersPerMinute,
            bottomSacLitersPerMinute,
            decoSacLitersPerMinute,
            bottomPo2 ?? Pressure.FromBar(1.4),
            decoPo2 ?? Pressure.FromBar(1.6),
            Pressure.FromBar(50),
            reserveStressFactor,
            reserveTeamSize,
            TimeSpan.FromMinutes(1),
            TimeSpan.FromMinutes(1),
            TimeSpan.FromSeconds(30),
            true,
            false,
            false,
            false,
            false);

    public static Cylinder CreateCylinder(
        GasMixture? gas = null,
        double sizeLiter = 12,
        double startPressureBar = 200,
        CylinderPurpose purpose = CylinderPurpose.BottomGas) =>
        new(gas ?? GasMixture.Air,
            Volume.FromLiter(sizeLiter),
            Pressure.FromBar(startPressureBar),
            purpose);

    public static DiveSegment CreateSegment(
        double depthMeter,
        double minutes,
        GasMixture? gas = null,
        SegmentKind kind = SegmentKind.Bottom) =>
        new(Depth.FromMeter(depthMeter),
            TimeSpan.FromMinutes(minutes),
            gas ?? GasMixture.Air,
            kind);
}