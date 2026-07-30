using BenchmarkDotNet.Attributes;
using Bitenovac.DecompressionAlgorithms.BuhlmannZhl16c;
using Bitenovac.DecompressionAlgorithms.Core.Environment;
using Bitenovac.DecompressionAlgorithms.Core.Equipment;
using Bitenovac.DecompressionAlgorithms.Core.Planning;
using Bitenovac.DecompressionAlgorithms.Units;

namespace Bitenovac.DecompressionAlgorithms.Zhl16c.Benchmarks;

/// <summary>
/// Measures the steady-state cost of the ZH-L16C model on a warmed algorithm instance:
/// beginning the dive, loading the working phase, querying the ceiling, and generating
/// the final ascent. The <see cref="MemoryDiagnoserAttribute" /> column is the contract
/// under test — a warmed instance must allocate zero bytes per planned dive.
/// </summary>
[MemoryDiagnoser]
[ShortRunJob]
public class PlanningBenchmarks
{
    private BuhlmannZhl16cAlgorithm _algorithm = null!;
    private DiveSegment _decoBottom;
    private DiveSegment _decoDescent;
    private DivePlanRequest _decoRequest = null!;
    private DiveSegment _noDecoBottom;
    private DiveSegment _noDecoDescent;
    private DivePlanRequest _noDecoRequest = null!;
    private DivePlanRequest _repetitiveRequest = null!;

    [GlobalSetup]
    public void Setup()
    {
        var settings = CreateSettings();
        var air = new Cylinder(GasMixture.Air, Volume.FromLiter(24), Pressure.FromBar(200),
            CylinderPurpose.BottomGas);
        var nitrox50 = new Cylinder(GasMixture.FromPercent(50, 0), Volume.FromLiter(11), Pressure.FromBar(200),
            CylinderPurpose.DecoGas);

        var decoProfile = new DiveProfile([
            new DiveSegment(Depth.FromMeter(45), TimeSpan.FromMinutes(25), GasMixture.Air, SegmentKind.Bottom)
        ]);
        var noDecoProfile = new DiveProfile([
            new DiveSegment(Depth.FromMeter(18), TimeSpan.FromMinutes(15), GasMixture.Air, SegmentKind.Bottom)
        ]);

        _algorithm = new BuhlmannZhl16cAlgorithm(0.3, 0.7);
        _decoRequest = new DivePlanRequest(decoProfile, [air, nitrox50], settings);
        _noDecoRequest = new DivePlanRequest(noDecoProfile, [air], settings);
        _repetitiveRequest = new DivePlanRequest(decoProfile, [air, nitrox50], settings,
            [new PriorDive(decoProfile, [air, nitrox50], settings, TimeSpan.FromMinutes(60), GasMixture.Air)]);

        _decoDescent = new DiveSegment(Depth.FromMeter(45), TimeSpan.FromMinutes(2.25), GasMixture.Air,
            SegmentKind.Descent);
        _decoBottom = new DiveSegment(Depth.FromMeter(45), TimeSpan.FromMinutes(25), GasMixture.Air,
            SegmentKind.Bottom);
        _noDecoDescent = new DiveSegment(Depth.FromMeter(18), TimeSpan.FromMinutes(0.9), GasMixture.Air,
            SegmentKind.Descent);
        _noDecoBottom = new DiveSegment(Depth.FromMeter(18), TimeSpan.FromMinutes(15), GasMixture.Air,
            SegmentKind.Bottom);
    }

    [Benchmark(Baseline = true)]
    public int PlanNoDecoDive() => Plan(_noDecoRequest, _noDecoDescent, _noDecoBottom);

    [Benchmark]
    public int PlanDecoDive() => Plan(_decoRequest, _decoDescent, _decoBottom);

    [Benchmark]
    public int PlanRepetitiveDecoDive() => Plan(_repetitiveRequest, _decoDescent, _decoBottom);

    private int Plan(DivePlanRequest request,
        in DiveSegment descent,
        in DiveSegment bottom)
    {
        var state = _algorithm.BeginDive(request);
        state = _algorithm.LoadSegment(state, descent);
        state = _algorithm.LoadSegment(state, bottom);
        _ = _algorithm.CurrentCeiling(state);
        return _algorithm.CalculateFinalAscent(state, request).Count;
    }

    private static DivePlanSettings CreateSettings() =>
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
            false,
            false,
            false,
            false,
            false);
}