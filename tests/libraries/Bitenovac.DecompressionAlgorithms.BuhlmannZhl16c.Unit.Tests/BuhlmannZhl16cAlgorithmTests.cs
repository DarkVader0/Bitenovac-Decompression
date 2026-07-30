using Bitenovac.DecompressionAlgorithms.BuhlmannZhl16c;
using Bitenovac.DecompressionAlgorithms.Core.Abstractions;
using Bitenovac.DecompressionAlgorithms.Core.Equipment;
using Bitenovac.DecompressionAlgorithms.Core.Planning;
using Bitenovac.DecompressionAlgorithms.Units;

namespace Bitenovac.DecompressionAlgorithms.Zhl16c.Unit.Tests;

public sealed class BuhlmannZhl16cAlgorithmTests
{
    private const int Precision = 6;
    private const int CompartmentCount = 16;

    // The published ZH-L16C tables (compartment 1 as the 1b variant), retyped here so
    // that expected values are derived from the documented coefficients and formulas
    // rather than from the implementation.
    private static readonly double[] NitrogenHalfTimeMinutes =
    [
        5.0, 8.0, 12.5, 18.5, 27.0, 38.3, 54.3, 77.0,
        109.0, 146.0, 187.0, 239.0, 305.0, 390.0, 498.0, 635.0
    ];

    private static readonly double[] NitrogenAMillibar =
    [
        1169.6, 1000.0, 861.8, 756.2, 620.0, 504.3, 441.0, 400.0,
        375.0, 350.0, 329.5, 306.5, 283.5, 261.0, 248.0, 232.7
    ];

    private static readonly double[] NitrogenB =
    [
        0.5578, 0.6514, 0.7222, 0.7825, 0.8126, 0.8434, 0.8693, 0.8910,
        0.9092, 0.9222, 0.9319, 0.9403, 0.9477, 0.9544, 0.9602, 0.9653
    ];

    private static double AmbientMillibar(double depthMeter) =>
        TestFactory.SurfacePressureMillibar + TestFactory.MillibarPerMeter * depthMeter;

    private static double SurfaceEquilibriumNitrogenMillibar() =>
        (TestFactory.SurfacePressureMillibar - TestFactory.WaterVaporPressureMillibar) * GasMixture.Air.FractionN2;

    /// <summary>The instantaneous exponential: P(t) = Palv + (P0 - Palv) * e^(-ln2 * t / halfTime).</summary>
    private static double Haldane(double initial,
        double alveolar,
        double halfTimeMinutes,
        double minutes) =>
        alveolar + (initial - alveolar) * Math.Exp(-Math.Log(2.0) * minutes / halfTimeMinutes);

    [Theory]
    [InlineData(0.0)]
    [InlineData(-0.1)]
    [InlineData(1.1)]
    public void Constructor_ShouldThrowArgumentOutOfRangeException_WhenGradientFactorLowIsOutsideRange(double low)
    {
        // Arrange

        // Act
        Action act = () => new BuhlmannZhl16cAlgorithm(low, 1.0);

        // Assert
        Assert.Throws<ArgumentOutOfRangeException>(act);
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(1.5)]
    public void Constructor_ShouldThrowArgumentOutOfRangeException_WhenGradientFactorHighIsOutsideRange(double high)
    {
        // Arrange

        // Act
        Action act = () => new BuhlmannZhl16cAlgorithm(0.3, high);

        // Assert
        Assert.Throws<ArgumentOutOfRangeException>(act);
    }

    [Fact]
    public void Constructor_ShouldThrowArgumentOutOfRangeException_WhenLowGradientFactorExceedsHigh()
    {
        // Arrange

        // Act
        Action act = () => new BuhlmannZhl16cAlgorithm(0.9, 0.5);

        // Assert
        Assert.Throws<ArgumentOutOfRangeException>(act);
    }

    [Fact]
    public void BeginDive_ShouldThrowArgumentNullException_WhenRequestIsNull()
    {
        // Arrange
        var algorithm = new BuhlmannZhl16cAlgorithm(1.0, 1.0);

        // Act
        Action act = () => algorithm.BeginDive(null!);

        // Assert
        Assert.Throws<ArgumentNullException>(act);
    }

    [Fact]
    public void BeginDive_ShouldEquilibrateTissuesToAirAtSurface_WhenThereAreNoPriorDives()
    {
        // Arrange
        var algorithm = new BuhlmannZhl16cAlgorithm(1.0, 1.0);
        var expectedNitrogen = SurfaceEquilibriumNitrogenMillibar();

        // Act
        var state = Assert.IsType<BuhlmannState>(algorithm.BeginDive(TestFactory.CreateRequest(30, 20)));

        // Assert
        for (var i = 0; i < CompartmentCount; i++)
        {
            Assert.Equal(expectedNitrogen, state.Nitrogen[i], Precision);
            Assert.Equal(0.0, state.Helium[i], Precision);
        }
    }

    [Fact]
    public void LoadSegment_ShouldThrowArgumentNullException_WhenStateIsNull()
    {
        // Arrange
        var algorithm = new BuhlmannZhl16cAlgorithm(1.0, 1.0);
        var segment = new DiveSegment(Depth.FromMeter(10), TimeSpan.FromMinutes(1), GasMixture.Air,
            SegmentKind.Bottom);

        // Act
        Action act = () => algorithm.LoadSegment(null!, segment);

        // Assert
        Assert.Throws<ArgumentNullException>(act);
    }

    [Fact]
    public void LoadSegment_ShouldThrowArgumentException_WhenStateWasNotProducedByThisModel()
    {
        // Arrange
        var algorithm = new BuhlmannZhl16cAlgorithm(1.0, 1.0);
        var segment = new DiveSegment(Depth.FromMeter(10), TimeSpan.FromMinutes(1), GasMixture.Air,
            SegmentKind.Bottom);
        var foreignState = new FakeDecompressionState();

        // Act
        Action act = () => algorithm.LoadSegment(foreignState, segment);

        // Assert
        Assert.Throws<ArgumentException>(act);
    }

    [Fact]
    public void LoadSegment_ShouldApplyInstantaneousExponential_WhenSegmentIsAtConstantDepth()
    {
        // Arrange
        var algorithm = new BuhlmannZhl16cAlgorithm(1.0, 1.0);
        var state = Assert.IsType<BuhlmannState>(algorithm.BeginDive(TestFactory.CreateRequest(25, 10)));
        var initial = SurfaceEquilibriumNitrogenMillibar();
        var alveolar = (AmbientMillibar(25) - TestFactory.WaterVaporPressureMillibar) * GasMixture.Air.FractionN2;
        var segment = new DiveSegment(Depth.FromMeter(25), TimeSpan.FromMinutes(10), GasMixture.Air,
            SegmentKind.Bottom);

        // Act
        algorithm.LoadSegment(state, segment);

        // Assert
        for (var i = 0; i < CompartmentCount; i++)
        {
            var expected = Haldane(initial, alveolar, NitrogenHalfTimeMinutes[i], 10.0);
            Assert.Equal(expected, state.Nitrogen[i], Precision);
        }
    }

    [Fact]
    public void LoadSegment_ShouldApplySchreinerEquation_WhenSegmentIsDescent()
    {
        // Arrange
        // Schreiner: P(t) = Palv0 + R*(t - 1/k) - (Palv0 - P0 - R/k) * e^(-k*t), with
        // Palv0 the alveolar inert pressure at the start depth and R its rate of change.
        var algorithm = new BuhlmannZhl16cAlgorithm(1.0, 1.0);
        var state = Assert.IsType<BuhlmannState>(algorithm.BeginDive(TestFactory.CreateRequest(30, 20)));
        const double minutes = 3.0;
        var initial = SurfaceEquilibriumNitrogenMillibar();
        var alveolarStart = (AmbientMillibar(0) - TestFactory.WaterVaporPressureMillibar) * GasMixture.Air.FractionN2;
        var rate = TestFactory.MillibarPerMeter * 30.0 / minutes * GasMixture.Air.FractionN2;
        var segment = new DiveSegment(Depth.FromMeter(30), TimeSpan.FromMinutes(minutes), GasMixture.Air,
            SegmentKind.Descent);

        // Act
        algorithm.LoadSegment(state, segment);

        // Assert
        for (var i = 0; i < CompartmentCount; i++)
        {
            var k = Math.Log(2.0) / NitrogenHalfTimeMinutes[i];
            var expected = alveolarStart + rate * (minutes - 1.0 / k)
                           - (alveolarStart - initial - rate / k) * Math.Exp(-k * minutes);
            Assert.Equal(expected, state.Nitrogen[i], Precision);
        }
    }

    [Fact]
    public void CurrentCeiling_ShouldReturnZero_WhenTissuesAreAtSurfaceEquilibrium()
    {
        // Arrange
        var algorithm = new BuhlmannZhl16cAlgorithm(1.0, 1.0);
        var state = algorithm.BeginDive(TestFactory.CreateRequest(30, 20));

        // Act
        var ceiling = algorithm.CurrentCeiling(state);

        // Assert
        Assert.Equal(0.0, ceiling.InMeter, Precision);
    }

    [Fact]
    public void CurrentCeiling_ShouldMatchMValueFormula_AfterLoadedBottomSegment()
    {
        // Arrange
        // With pure nitrogen loading and gf = 1 the tolerated ambient pressure of a
        // compartment is (P - a) * b; the ceiling is the deepest compartment ceiling.
        var algorithm = new BuhlmannZhl16cAlgorithm(1.0, 1.0);
        var state = algorithm.BeginDive(TestFactory.CreateRequest(40, 30));
        var initial = SurfaceEquilibriumNitrogenMillibar();
        var alveolar = (AmbientMillibar(40) - TestFactory.WaterVaporPressureMillibar) * GasMixture.Air.FractionN2;
        var segment = new DiveSegment(Depth.FromMeter(40), TimeSpan.FromMinutes(30), GasMixture.Air,
            SegmentKind.Bottom);

        var expectedCeilingMeter = 0.0;
        for (var i = 0; i < CompartmentCount; i++)
        {
            var loaded = Haldane(initial, alveolar, NitrogenHalfTimeMinutes[i], 30.0);
            var tolerated = (loaded - NitrogenAMillibar[i]) * NitrogenB[i];
            var ceilingMeter = (tolerated - TestFactory.SurfacePressureMillibar) / TestFactory.MillibarPerMeter;
            expectedCeilingMeter = Math.Max(expectedCeilingMeter, ceilingMeter);
        }

        // Act
        algorithm.LoadSegment(state, segment);
        var ceiling = algorithm.CurrentCeiling(state);

        // Assert
        Assert.True(expectedCeilingMeter > 0.0);
        Assert.Equal(expectedCeilingMeter, ceiling.InMeter, Precision);
    }

    [Fact]
    public void CurrentCeiling_ShouldBeDeeper_WhenLowGradientFactorIsMoreConservative()
    {
        // Arrange
        var conservative = new BuhlmannZhl16cAlgorithm(0.3, 0.7);
        var permissive = new BuhlmannZhl16cAlgorithm(1.0, 1.0);
        var segment = new DiveSegment(Depth.FromMeter(40), TimeSpan.FromMinutes(30), GasMixture.Air,
            SegmentKind.Bottom);

        // Act
        var conservativeState = conservative.LoadSegment(conservative.BeginDive(TestFactory.CreateRequest(40, 30)),
            segment);
        var permissiveState = permissive.LoadSegment(permissive.BeginDive(TestFactory.CreateRequest(40, 30)),
            segment);

        // Assert
        Assert.True(conservative.CurrentCeiling(conservativeState).InMeter
                    > permissive.CurrentCeiling(permissiveState).InMeter);
    }

    [Fact]
    public void BeginDive_ShouldRetainResidualNitrogen_WhenPriorDiveIsSupplied()
    {
        // Arrange
        var algorithm = new BuhlmannZhl16cAlgorithm(0.3, 0.7);
        var priorDive = new PriorDive(
            new DiveProfile([
                new DiveSegment(Depth.FromMeter(30), TimeSpan.FromMinutes(25), GasMixture.Air, SegmentKind.Bottom)
            ]),
            [TestFactory.CreateCylinder()],
            TestFactory.CreateSettings(),
            TimeSpan.FromMinutes(60),
            GasMixture.Air);
        var equilibrium = SurfaceEquilibriumNitrogenMillibar();

        // Act
        var state = Assert.IsType<BuhlmannState>(
            algorithm.BeginDive(TestFactory.CreateRequest(30, 20, priorDives: [priorDive])));

        // Assert
        Assert.True(state.Nitrogen[CompartmentCount - 1] > equilibrium + 1.0);
    }

    [Fact]
    public void BeginDive_ShouldCarryLessResidualNitrogen_WhenSurfaceIntervalIsLonger()
    {
        // Arrange
        var algorithm = new BuhlmannZhl16cAlgorithm(0.3, 0.7);
        var profile = new DiveProfile([
            new DiveSegment(Depth.FromMeter(30), TimeSpan.FromMinutes(25), GasMixture.Air, SegmentKind.Bottom)
        ]);
        var settings = TestFactory.CreateSettings();

        PriorDive CreatePrior(int surfaceMinutes) => new(profile, [TestFactory.CreateCylinder()], settings,
            TimeSpan.FromMinutes(surfaceMinutes), GasMixture.Air);

        // Act
        var shortIntervalState = Assert.IsType<BuhlmannState>(
            algorithm.BeginDive(TestFactory.CreateRequest(30, 20, priorDives: [CreatePrior(30)])));
        var shortIntervalNitrogen = shortIntervalState.Nitrogen[CompartmentCount - 1];

        var longIntervalState = Assert.IsType<BuhlmannState>(
            algorithm.BeginDive(TestFactory.CreateRequest(30, 20, priorDives: [CreatePrior(240)])));
        var longIntervalNitrogen = longIntervalState.Nitrogen[CompartmentCount - 1];

        // Assert
        Assert.True(shortIntervalNitrogen > longIntervalNitrogen);
    }

    [Fact]
    public void CalculateFinalAscent_ShouldThrowArgumentNullException_WhenRequestIsNull()
    {
        // Arrange
        var algorithm = new BuhlmannZhl16cAlgorithm(1.0, 1.0);
        var state = algorithm.BeginDive(TestFactory.CreateRequest(30, 20));

        // Act
        Action act = () => algorithm.CalculateFinalAscent(state, null!);

        // Assert
        Assert.Throws<ArgumentNullException>(act);
    }

    [Fact]
    public void CalculateFinalAscent_ShouldContainOnlyAscents_WhenNoDecompressionIsRequired()
    {
        // Arrange
        var algorithm = new BuhlmannZhl16cAlgorithm(0.85, 0.85);
        var request = TestFactory.CreateRequest(12, 10);
        var state = algorithm.BeginDive(request);
        algorithm.LoadSegment(state, new DiveSegment(Depth.FromMeter(12), TimeSpan.FromMinutes(10), GasMixture.Air,
            SegmentKind.Bottom));

        // Act
        var ascent = algorithm.CalculateFinalAscent(state, request);

        // Assert
        Assert.NotEmpty(ascent);
        for (var i = 0; i < ascent.Count; i++)
        {
            Assert.Equal(SegmentKind.Ascent, ascent[i].Kind);
        }

        Assert.Equal(0.0, ascent[^1].Depth.InMeter, Precision);
    }

    [Fact]
    public void CalculateFinalAscent_ShouldInsertSafetyStop_WhenEnabledAndNoDecompressionIsRequired()
    {
        // Arrange
        var algorithm = new BuhlmannZhl16cAlgorithm(0.85, 0.85);
        var request = TestFactory.CreateRequest(12, 10, settings: TestFactory.CreateSettings(true));
        var state = algorithm.BeginDive(request);
        algorithm.LoadSegment(state, new DiveSegment(Depth.FromMeter(12), TimeSpan.FromMinutes(10), GasMixture.Air,
            SegmentKind.Bottom));

        // Act
        var ascent = algorithm.CalculateFinalAscent(state, request);

        // Assert
        var safetyStops = 0;
        for (var i = 0; i < ascent.Count; i++)
        {
            if (ascent[i].Kind == SegmentKind.Stop)
            {
                Assert.Equal(5.0, ascent[i].Depth.InMeter, Precision);
                Assert.Equal(TimeSpan.FromMinutes(3), ascent[i].Duration);
                safetyStops++;
            }
        }

        Assert.Equal(1, safetyStops);
        Assert.Equal(0.0, ascent[^1].Depth.InMeter, Precision);
    }

    [Fact]
    public void CalculateFinalAscent_ShouldPlaceStopsOnThreeMeterGrid_WhenDecompressionIsRequired()
    {
        // Arrange
        var algorithm = new BuhlmannZhl16cAlgorithm(0.3, 0.7);
        var request = TestFactory.CreateRequest(45, 25);
        var state = algorithm.BeginDive(request);
        algorithm.LoadSegment(state, new DiveSegment(Depth.FromMeter(45), TimeSpan.FromMinutes(25), GasMixture.Air,
            SegmentKind.Bottom));

        // Act
        var ascent = algorithm.CalculateFinalAscent(state, request);

        // Assert
        var stops = 0;
        var previousDepthMeter = 45.0;
        for (var i = 0; i < ascent.Count; i++)
        {
            var depthMeter = ascent[i].Depth.InMeter;
            Assert.True(depthMeter <= previousDepthMeter + 1e-9);
            previousDepthMeter = depthMeter;

            if (ascent[i].Kind == SegmentKind.Stop)
            {
                stops++;
                Assert.Equal(0.0, depthMeter % 3.0, Precision);
            }
        }

        Assert.True(stops > 0);
        Assert.Equal(0.0, ascent[^1].Depth.InMeter, Precision);
    }

    [Fact]
    public void CalculateFinalAscent_ShouldNotStopAtThreeMeters_WhenLastStopIsAtSixMeters()
    {
        // Arrange
        var algorithm = new BuhlmannZhl16cAlgorithm(0.3, 0.7);
        var request = TestFactory.CreateRequest(45, 25,
            settings: TestFactory.CreateSettings(lastStopAtSixMeters: true));
        var state = algorithm.BeginDive(request);
        algorithm.LoadSegment(state, new DiveSegment(Depth.FromMeter(45), TimeSpan.FromMinutes(25), GasMixture.Air,
            SegmentKind.Bottom));

        // Act
        var ascent = algorithm.CalculateFinalAscent(state, request);

        // Assert
        var sixMeterStops = 0;
        for (var i = 0; i < ascent.Count; i++)
        {
            if (ascent[i].Kind != SegmentKind.Stop)
            {
                continue;
            }

            Assert.NotEqual(3.0, ascent[i].Depth.InMeter, Precision);
            if (Math.Abs(ascent[i].Depth.InMeter - 6.0) < 1e-9)
            {
                sixMeterStops++;
            }
        }

        Assert.True(sixMeterStops > 0);
    }

    [Fact]
    public void CalculateFinalAscent_ShouldSwitchToRicherGas_AtItsOperatingDepth()
    {
        // Arrange
        // EAN50 at a deco PO2 of 1.6 bar has its operating limit at an ambient pressure of
        // 3200 mbar, i.e. 22.43 m in fresh water, so the switch lands on the 21 m grid depth.
        var nitrox50 = GasMixture.FromPercent(50, 0);
        var cylinders = new[] { TestFactory.CreateCylinder(), TestFactory.CreateCylinder(nitrox50) };
        var algorithm = new BuhlmannZhl16cAlgorithm(0.3, 0.7);
        var request = TestFactory.CreateRequest(40, 20, cylinders);
        var state = algorithm.BeginDive(request);
        algorithm.LoadSegment(state, new DiveSegment(Depth.FromMeter(40), TimeSpan.FromMinutes(20), GasMixture.Air,
            SegmentKind.Bottom));

        // Act
        var ascent = algorithm.CalculateFinalAscent(state, request);

        // Assert
        var switches = 0;
        for (var i = 0; i < ascent.Count; i++)
        {
            if (ascent[i].Kind != SegmentKind.GasSwitch)
            {
                continue;
            }

            switches++;
            Assert.Equal(nitrox50, ascent[i].Gas);
            Assert.True(ascent[i].Depth.InMeter <= 22.5);
        }

        Assert.Equal(1, switches);
    }

    [Fact]
    public void CalculateFinalAscent_ShouldInsertOxygenBreaks_WhenEnabled()
    {
        // Arrange
        var cylinders = new[] { TestFactory.CreateCylinder(), TestFactory.CreateCylinder(GasMixture.Oxygen) };
        var settings = TestFactory.CreateSettings(lastStopAtSixMeters: true, oxygenBreaks: true,
            oxygenBreakInterval: TimeSpan.FromMinutes(5), oxygenBreakDuration: TimeSpan.FromMinutes(3));
        var algorithm = new BuhlmannZhl16cAlgorithm(0.3, 0.7);
        var request = TestFactory.CreateRequest(45, 30, cylinders, settings);
        var state = algorithm.BeginDive(request);
        algorithm.LoadSegment(state, new DiveSegment(Depth.FromMeter(45), TimeSpan.FromMinutes(30), GasMixture.Air,
            SegmentKind.Bottom));

        // Act
        var ascent = algorithm.CalculateFinalAscent(state, request);

        // Assert
        var oxygenStops = 0;
        var breaks = 0;
        for (var i = 0; i < ascent.Count; i++)
        {
            if (ascent[i].Kind != SegmentKind.Stop || Math.Abs(ascent[i].Depth.InMeter - 6.0) > 1e-9)
            {
                continue;
            }

            if (ascent[i].Gas == GasMixture.Oxygen)
            {
                oxygenStops++;
            }
            else if (ascent[i].Gas == GasMixture.Air)
            {
                Assert.Equal(TimeSpan.FromMinutes(3), ascent[i].Duration);
                breaks++;
            }
        }

        Assert.True(oxygenStops > 0);
        Assert.True(breaks > 0);
    }

    [Fact]
    public void CalculateFinalAscent_ShouldRoundStopTimesUpToIncrement_WhenDecompressionIsRequired()
    {
        // Arrange
        var settings = TestFactory.CreateSettings(stopTimeIncrement: TimeSpan.FromMinutes(2));
        var algorithm = new BuhlmannZhl16cAlgorithm(0.3, 0.7);
        var request = TestFactory.CreateRequest(45, 25, settings: settings);
        var state = algorithm.BeginDive(request);
        algorithm.LoadSegment(state, new DiveSegment(Depth.FromMeter(45), TimeSpan.FromMinutes(25), GasMixture.Air,
            SegmentKind.Bottom));

        // Act
        var ascent = algorithm.CalculateFinalAscent(state, request);

        // Assert
        var stops = 0;
        for (var i = 0; i < ascent.Count; i++)
        {
            if (ascent[i].Kind != SegmentKind.Stop)
            {
                continue;
            }

            stops++;
            Assert.Equal(0, ascent[i].Duration.Ticks % TimeSpan.FromMinutes(2).Ticks);
        }

        Assert.True(stops > 0);
    }

    [Fact]
    public void CalculateFinalAscent_ShouldBeDeterministic_WhenRunTwiceFromIdenticalInputs()
    {
        // Arrange
        var nitrox50 = GasMixture.FromPercent(50, 0);

        static DiveSegment[] Run(GasMixture deco)
        {
            var cylinders = new[] { TestFactory.CreateCylinder(), TestFactory.CreateCylinder(deco) };
            var algorithm = new BuhlmannZhl16cAlgorithm(0.3, 0.7);
            var request = TestFactory.CreateRequest(45, 25, cylinders);
            var state = algorithm.BeginDive(request);
            algorithm.LoadSegment(state, new DiveSegment(Depth.FromMeter(45), TimeSpan.FromMinutes(25),
                GasMixture.Air, SegmentKind.Bottom));
            return [.. algorithm.CalculateFinalAscent(state, request)];
        }

        // Act
        var first = Run(nitrox50);
        var second = Run(nitrox50);

        // Assert
        Assert.Equal(first.Length, second.Length);
        for (var i = 0; i < first.Length; i++)
        {
            Assert.Equal(first[i].Depth.InMillimeter, second[i].Depth.InMillimeter);
            Assert.Equal(first[i].Duration, second[i].Duration);
            Assert.Equal(first[i].Gas, second[i].Gas);
            Assert.Equal(first[i].Kind, second[i].Kind);
        }
    }

    [Fact]
    public void BeginDive_ShouldSkipZeroDurationSegments_WhenReplayingPriorDiveProfile()
    {
        // Arrange
        static PriorDive CreatePrior(params DiveSegment[] segments) =>
            new(new DiveProfile(segments), [TestFactory.CreateCylinder()], TestFactory.CreateSettings(),
                TimeSpan.FromMinutes(60), GasMixture.Air);

        var bottom = new DiveSegment(Depth.FromMeter(30), TimeSpan.FromMinutes(20), GasMixture.Air,
            SegmentKind.Bottom);
        var waypoint = new DiveSegment(Depth.FromMeter(30), TimeSpan.Zero, GasMixture.Air, SegmentKind.Bottom);
        var without = new BuhlmannZhl16cAlgorithm(0.3, 0.85);
        var with = new BuhlmannZhl16cAlgorithm(0.3, 0.85);

        // Act
        var withoutState = Assert.IsType<BuhlmannState>(
            without.BeginDive(TestFactory.CreateRequest(30, 20, priorDives: [CreatePrior(bottom)])));
        var withState = Assert.IsType<BuhlmannState>(
            with.BeginDive(TestFactory.CreateRequest(30, 20, priorDives: [CreatePrior(bottom, waypoint)])));

        // Assert
        for (var i = 0; i < CompartmentCount; i++)
        {
            Assert.Equal(withoutState.Nitrogen[i], withState.Nitrogen[i], Precision);
        }
    }

    [Fact]
    public void CalculateFinalAscent_ShouldReturnNoSegments_WhenStateIsAtSurface()
    {
        // Arrange
        var algorithm = new BuhlmannZhl16cAlgorithm(0.3, 0.85);
        var request = TestFactory.CreateRequest(30, 20);
        var state = algorithm.BeginDive(request);

        // Act
        var ascent = algorithm.CalculateFinalAscent(state, request);

        // Assert
        Assert.Empty(ascent);
    }

    [Fact]
    public void CalculateFinalAscent_ShouldHoldAtCurrentDepth_WhenCeilingIsDeeperThanFirstStop()
    {
        // Arrange
        var algorithm = new BuhlmannZhl16cAlgorithm(0.3, 0.85);
        var request = TestFactory.CreateRequest(45, 30);
        var state = algorithm.BeginDive(request);
        algorithm.LoadSegment(state, new DiveSegment(Depth.FromMeter(45), TimeSpan.FromMinutes(30),
            GasMixture.Air, SegmentKind.Bottom));
        algorithm.LoadSegment(state, new DiveSegment(Depth.FromMeter(4), TimeSpan.FromMinutes(5),
            GasMixture.Air, SegmentKind.Ascent));

        // Act
        var ascent = algorithm.CalculateFinalAscent(state, request);

        // Assert
        Assert.Equal(SegmentKind.Stop, ascent[0].Kind);
        Assert.Equal(4.0, ascent[0].Depth.InMeter, Precision);
        Assert.Equal(0.0, ascent[^1].Depth.InMeter, Precision);
    }

    [Fact]
    public void CalculateFinalAscent_ShouldSwitchGasInPlace_WhenMinimumGasSwitchDurationIsZero()
    {
        // Arrange
        var nitrox50 = GasMixture.FromPercent(50, 0);
        var settings = TestFactory.CreateSettings(minimumGasSwitchDuration: TimeSpan.Zero);
        var cylinders = new[] { TestFactory.CreateCylinder(), TestFactory.CreateCylinder(nitrox50) };
        var algorithm = new BuhlmannZhl16cAlgorithm(0.3, 0.7);
        var request = TestFactory.CreateRequest(45, 25, cylinders, settings);
        var state = algorithm.BeginDive(request);
        algorithm.LoadSegment(state, new DiveSegment(Depth.FromMeter(45), TimeSpan.FromMinutes(25),
            GasMixture.Air, SegmentKind.Bottom));

        // Act
        var ascent = algorithm.CalculateFinalAscent(state, request);

        // Assert
        Assert.DoesNotContain(ascent, segment => segment.Kind == SegmentKind.GasSwitch);
        Assert.Contains(ascent, segment => segment.Gas == nitrox50);
    }

    [Fact]
    public void CalculateFinalAscent_ShouldContinueOxygenStop_WhenNoOxygenBreakGasIsAvailable()
    {
        // Arrange
        var settings = TestFactory.CreateSettings(oxygenBreaks: true,
            oxygenBreakInterval: TimeSpan.FromMinutes(1), oxygenBreakDuration: TimeSpan.FromMinutes(1));
        var cylinders = new[] { TestFactory.CreateCylinder(GasMixture.Oxygen) };
        var algorithm = new BuhlmannZhl16cAlgorithm(0.5, 0.5);
        var request = TestFactory.CreateRequest(33, 35, cylinders, settings);
        var state = algorithm.BeginDive(request);
        algorithm.LoadSegment(state, new DiveSegment(Depth.FromMeter(33), TimeSpan.FromMinutes(35),
            GasMixture.Air, SegmentKind.Bottom));
        algorithm.LoadSegment(state, new DiveSegment(Depth.FromMeter(5), TimeSpan.FromMinutes(3),
            GasMixture.Air, SegmentKind.Ascent));

        // Act
        var ascent = algorithm.CalculateFinalAscent(state, request);

        // Assert
        var shallowStops = ascent
            .Where(segment => segment.Kind == SegmentKind.Stop && Math.Abs(segment.Depth.InMeter - 3.0) < 1e-9)
            .ToList();
        Assert.All(shallowStops, stop => Assert.Equal(GasMixture.Oxygen, stop.Gas));
        Assert.Contains(shallowStops, stop => stop.Duration >= TimeSpan.FromMinutes(2));
        Assert.Equal(0.0, ascent[^1].Depth.InMeter, Precision);
    }

    [Fact]
    public void CalculateFinalAscent_ShouldThrowInvalidOperationException_WhenStopDoesNotClearWithinTwentyFourHours()
    {
        // Arrange
        var algorithm = new BuhlmannZhl16cAlgorithm(0.05, 0.05);
        var request = TestFactory.CreateRequest(60, 300);
        var state = algorithm.BeginDive(request);
        algorithm.LoadSegment(state, new DiveSegment(Depth.FromMeter(60), TimeSpan.FromMinutes(300),
            GasMixture.Air, SegmentKind.Bottom));

        // Act
        Action act = () => algorithm.CalculateFinalAscent(state, request);

        // Assert
        Assert.Throws<InvalidOperationException>(act);
    }

    [Fact]
    public void BeginDive_ShouldRetainLessNitrogen_WhenPriorDiveProfileAscendsToShallowerLevel()
    {
        // Arrange
        static PriorDive CreatePrior(double secondLevelMeter) =>
            new(new DiveProfile([
                    new DiveSegment(Depth.FromMeter(30), TimeSpan.FromMinutes(10), GasMixture.Air,
                        SegmentKind.Bottom),
                    new DiveSegment(Depth.FromMeter(secondLevelMeter), TimeSpan.FromMinutes(10), GasMixture.Air,
                        SegmentKind.Bottom)
                ]),
                [TestFactory.CreateCylinder()], TestFactory.CreateSettings(), TimeSpan.FromMinutes(60),
                GasMixture.Air);

        var multilevel = new BuhlmannZhl16cAlgorithm(0.3, 0.85);
        var square = new BuhlmannZhl16cAlgorithm(0.3, 0.85);

        // Act
        var multilevelState = Assert.IsType<BuhlmannState>(
            multilevel.BeginDive(TestFactory.CreateRequest(30, 20, priorDives: [CreatePrior(15)])));
        var squareState = Assert.IsType<BuhlmannState>(
            square.BeginDive(TestFactory.CreateRequest(30, 20, priorDives: [CreatePrior(30)])));

        // Assert
        Assert.True(multilevelState.Nitrogen[0] < squareState.Nitrogen[0]);
    }

    [Fact]
    public void LoadSegment_ShouldNotLoadTissues_WhenSegmentDurationIsZero()
    {
        // Arrange
        var algorithm = new BuhlmannZhl16cAlgorithm(0.3, 0.85);
        var state = Assert.IsType<BuhlmannState>(algorithm.BeginDive(TestFactory.CreateRequest(30, 20)));
        var equilibrium = SurfaceEquilibriumNitrogenMillibar();

        // Act
        algorithm.LoadSegment(state, new DiveSegment(Depth.FromMeter(30), TimeSpan.Zero, GasMixture.Air,
            SegmentKind.Bottom));

        // Assert
        for (var i = 0; i < CompartmentCount; i++)
        {
            Assert.Equal(equilibrium, state.Nitrogen[i], Precision);
        }
    }

    [Fact]
    public void CalculateFinalAscent_ShouldAscendDirectly_WhenNoTimeHasElapsedAtDepth()
    {
        // Arrange
        var algorithm = new BuhlmannZhl16cAlgorithm(0.3, 0.85);
        var request = TestFactory.CreateRequest(30, 20);
        var state = algorithm.BeginDive(request);
        algorithm.LoadSegment(state, new DiveSegment(Depth.FromMeter(30), TimeSpan.Zero, GasMixture.Air,
            SegmentKind.Bottom));

        // Act
        var ascent = algorithm.CalculateFinalAscent(state, request);

        // Assert
        Assert.All(ascent, segment => Assert.Equal(SegmentKind.Ascent, segment.Kind));
        Assert.Equal(0.0, ascent[^1].Depth.InMeter, Precision);
    }

    [Fact]
    public void CalculateFinalAscent_ShouldHoldAtCurrentDepth_WhenCurrentDepthIsTheFirstStop()
    {
        // Arrange
        var algorithm = new BuhlmannZhl16cAlgorithm(0.3, 0.85);
        var request = TestFactory.CreateRequest(45, 30);
        var state = algorithm.BeginDive(request);
        algorithm.LoadSegment(state, new DiveSegment(Depth.FromMeter(45), TimeSpan.FromMinutes(30),
            GasMixture.Air, SegmentKind.Bottom));
        algorithm.LoadSegment(state, new DiveSegment(Depth.FromMeter(6), TimeSpan.FromMinutes(5),
            GasMixture.Air, SegmentKind.Ascent));

        // Act
        var ascent = algorithm.CalculateFinalAscent(state, request);

        // Assert
        Assert.Equal(SegmentKind.Stop, ascent[0].Kind);
        Assert.Equal(6.0, ascent[0].Depth.InMeter, Precision);
        Assert.Equal(0.0, ascent[^1].Depth.InMeter, Precision);
    }

    [Fact]
    public void CalculateFinalAscent_ShouldNotSwitchAtOperatingDepth_WhenSwitchAtRequiredStopIsEnabled()
    {
        // Arrange
        // Nitrox 50 at the deco limit of 1.6 bar has an operating pressure of 3.2 bar,
        // which is 22.4 m in fresh water at one bar, flooring to 21 m on the stop grid.
        // With the switch deferred to the required stop, no switch may occur there.
        const double OperatingGridDepthMeter = 21.0;
        var nitrox50 = GasMixture.FromPercent(50, 0);
        var settings = TestFactory.CreateSettings(switchAtRequiredStop: true);
        var cylinders = new[] { TestFactory.CreateCylinder(), TestFactory.CreateCylinder(nitrox50) };
        var algorithm = new BuhlmannZhl16cAlgorithm(0.3, 0.7);
        var request = TestFactory.CreateRequest(40, 20, cylinders, settings);
        var state = algorithm.BeginDive(request);
        algorithm.LoadSegment(state, new DiveSegment(Depth.FromMeter(40), TimeSpan.FromMinutes(20),
            GasMixture.Air, SegmentKind.Bottom));

        // Act
        var ascent = algorithm.CalculateFinalAscent(state, request);

        // Assert
        var switches = ascent.Where(segment => segment.Kind == SegmentKind.GasSwitch).ToList();
        Assert.NotEmpty(switches);
        Assert.All(switches, gasSwitch =>
            Assert.True(gasSwitch.Depth.InMeter < OperatingGridDepthMeter));
    }

    [Fact]
    public void CalculateFinalAscent_ShouldInsertOxygenBreakOnRichestNonOxygenGas_WhenBreakIsDue()
    {
        // Arrange
        var nitrox50 = GasMixture.FromPercent(50, 0);
        var nitrox32 = GasMixture.FromPercent(32, 0);
        var settings = TestFactory.CreateSettings(oxygenBreaks: true,
            oxygenBreakInterval: TimeSpan.FromMinutes(1), oxygenBreakDuration: TimeSpan.FromMinutes(1));
        var cylinders = new[]
        {
            TestFactory.CreateCylinder(),
            TestFactory.CreateCylinder(nitrox50),
            TestFactory.CreateCylinder(nitrox32),
            TestFactory.CreateCylinder(GasMixture.Oxygen)
        };
        var algorithm = new BuhlmannZhl16cAlgorithm(0.5, 0.5);
        var request = TestFactory.CreateRequest(33, 35, cylinders, settings);
        var state = algorithm.BeginDive(request);
        algorithm.LoadSegment(state, new DiveSegment(Depth.FromMeter(33), TimeSpan.FromMinutes(35),
            GasMixture.Air, SegmentKind.Bottom));

        // Act
        var ascent = algorithm.CalculateFinalAscent(state, request);

        // Assert
        // Regular stops at six meters and shallower are breathed on pure oxygen, so a stop
        // on the richest non-oxygen gas at those depths can only be an oxygen break.
        Assert.Contains(ascent, segment => segment.Kind == SegmentKind.Stop
                                           && segment.Depth.InMeter <= 6.0 + 1e-9
                                           && segment.Gas == nitrox50);
        Assert.Equal(0.0, ascent[^1].Depth.InMeter, Precision);
    }

    [Fact]
    public void CalculateFinalAscent_ShouldAscendAtShallowBandRate_WhenAverageDepthIsShallow()
    {
        // Arrange
        var algorithm = new BuhlmannZhl16cAlgorithm(0.3, 0.85);
        var request = TestFactory.CreateRequest(8, 30);
        var state = algorithm.BeginDive(request);
        algorithm.LoadSegment(state, new DiveSegment(Depth.FromMeter(8), TimeSpan.FromMinutes(30),
            GasMixture.Air, SegmentKind.Bottom));

        // Act
        var ascent = algorithm.CalculateFinalAscent(state, request);

        // Assert
        Assert.All(ascent, segment => Assert.Equal(SegmentKind.Ascent, segment.Kind));
        Assert.Equal(0.0, ascent[^1].Depth.InMeter, Precision);
    }

    [Fact]
    public void CalculateFinalAscent_ShouldTakeTheOxygenBreakOnTheBottomGas_WhenItIsTheOnlyGasThatIsNotOxygen()
    {
        // Arrange
        // Only air and oxygen are carried, so the leanest gas on the diver is also the only
        // one the break can fall back to.
        var settings = TestFactory.CreateSettings(oxygenBreaks: true,
            oxygenBreakInterval: TimeSpan.FromMinutes(1), oxygenBreakDuration: TimeSpan.FromMinutes(1));
        var cylinders = new[] { TestFactory.CreateCylinder(), TestFactory.CreateCylinder(GasMixture.Oxygen) };
        var algorithm = new BuhlmannZhl16cAlgorithm(0.5, 0.5);
        var request = TestFactory.CreateRequest(33, 35, cylinders, settings);
        var state = algorithm.BeginDive(request);
        algorithm.LoadSegment(state, new DiveSegment(Depth.FromMeter(33), TimeSpan.FromMinutes(35),
            GasMixture.Air, SegmentKind.Bottom));

        // Act
        var ascent = algorithm.CalculateFinalAscent(state, request);

        // Assert
        // A stop breathed on air at six meters or shallower can only be an oxygen break,
        // since the regular stops there are breathed on oxygen.
        Assert.Contains(ascent, segment => segment.Kind == SegmentKind.Stop
                                           && segment.Depth.InMeter <= 6.0 + 1e-9
                                           && segment.Gas == GasMixture.Air);
        Assert.Equal(0.0, ascent[^1].Depth.InMeter, Precision);
    }

    [Fact]
    public void CalculateFinalAscent_ShouldNotTakeTheOxygenBreakOnTheLoopOxygenSupply_WhenOneIsCarried()
    {
        // Arrange
        // The rebreather's oxygen supply carries no second stage to breathe from, so the
        // break must fall back to the air even though the supply holds a richer break gas.
        var settings = TestFactory.CreateSettings(oxygenBreaks: true,
            oxygenBreakInterval: TimeSpan.FromMinutes(1), oxygenBreakDuration: TimeSpan.FromMinutes(1));
        var cylinders = new[]
        {
            TestFactory.CreateCylinder(),
            new Cylinder(GasMixture.FromPercent(50, 0), Volume.FromLiter(3), Pressure.FromBar(200),
                CylinderPurpose.Oxygen),
            TestFactory.CreateCylinder(GasMixture.Oxygen)
        };
        var algorithm = new BuhlmannZhl16cAlgorithm(0.5, 0.5);
        var request = TestFactory.CreateRequest(33, 35, cylinders, settings);
        var state = algorithm.BeginDive(request);
        algorithm.LoadSegment(state, new DiveSegment(Depth.FromMeter(33), TimeSpan.FromMinutes(35),
            GasMixture.Air, SegmentKind.Bottom));

        // Act
        var ascent = algorithm.CalculateFinalAscent(state, request);

        // Assert
        var breaks = ascent
            .Where(segment => segment.Kind == SegmentKind.Stop
                              && segment.Depth.InMeter <= 6.0 + 1e-9
                              && segment.Gas != GasMixture.Oxygen)
            .ToList();
        Assert.NotEmpty(breaks);
        Assert.All(breaks, oxygenBreak => Assert.Equal(GasMixture.Air, oxygenBreak.Gas));
    }

    [Fact]
    public void CalculateFinalAscent_ShouldNotTakeTheOxygenBreakOnAMixAtThePureOxygenThreshold()
    {
        // Arrange
        // A mix of 99.9% oxygen counts as oxygen, so it cannot serve as a break from oxygen
        // and the break falls back to the air.
        var nearlyPureOxygen = GasMixture.FromPercent(99.9, 0);
        var settings = TestFactory.CreateSettings(oxygenBreaks: true,
            oxygenBreakInterval: TimeSpan.FromMinutes(1), oxygenBreakDuration: TimeSpan.FromMinutes(1));
        var cylinders = new[]
        {
            TestFactory.CreateCylinder(),
            TestFactory.CreateCylinder(nearlyPureOxygen),
            TestFactory.CreateCylinder(GasMixture.Oxygen)
        };
        var algorithm = new BuhlmannZhl16cAlgorithm(0.5, 0.5);
        var request = TestFactory.CreateRequest(33, 35, cylinders, settings);
        var state = algorithm.BeginDive(request);
        algorithm.LoadSegment(state, new DiveSegment(Depth.FromMeter(33), TimeSpan.FromMinutes(35),
            GasMixture.Air, SegmentKind.Bottom));

        // Act
        var ascent = algorithm.CalculateFinalAscent(state, request);

        // Assert
        var breaks = ascent
            .Where(segment => segment.Kind == SegmentKind.Stop
                              && segment.Depth.InMeter <= 6.0 + 1e-9
                              && segment.Gas != GasMixture.Oxygen)
            .ToList();
        Assert.NotEmpty(breaks);
        Assert.All(breaks, oxygenBreak => Assert.Equal(GasMixture.Air, oxygenBreak.Gas));
    }

    [Fact]
    public void CalculateFinalAscent_ShouldReturnToOxygenAfterTheBreak_WhenTheStopContinues()
    {
        // Arrange
        var nitrox50 = GasMixture.FromPercent(50, 0);
        var settings = TestFactory.CreateSettings(oxygenBreaks: true,
            oxygenBreakInterval: TimeSpan.FromMinutes(1), oxygenBreakDuration: TimeSpan.FromMinutes(1));
        var cylinders = new[]
        {
            TestFactory.CreateCylinder(),
            TestFactory.CreateCylinder(nitrox50),
            TestFactory.CreateCylinder(GasMixture.Oxygen)
        };
        var algorithm = new BuhlmannZhl16cAlgorithm(0.5, 0.5);
        var request = TestFactory.CreateRequest(33, 35, cylinders, settings);
        var state = algorithm.BeginDive(request);
        algorithm.LoadSegment(state, new DiveSegment(Depth.FromMeter(33), TimeSpan.FromMinutes(35),
            GasMixture.Air, SegmentKind.Bottom));

        // Act
        var ascent = algorithm.CalculateFinalAscent(state, request);

        // Assert
        var shallowStops = ascent
            .Where(segment => segment.Kind == SegmentKind.Stop && segment.Depth.InMeter <= 6.0 + 1e-9)
            .ToList();
        var firstBreak = shallowStops.FindIndex(stop => stop.Gas == nitrox50);
        Assert.True(firstBreak >= 0);
        Assert.True(firstBreak + 1 < shallowStops.Count);
        Assert.Equal(GasMixture.Oxygen, shallowStops[firstBreak + 1].Gas);
    }

    [Fact]
    public void CalculateFinalAscent_ShouldInsertAFurtherOxygenBreak_WhenTheStopOutlastsASecondInterval()
    {
        // Arrange
        // The interval and the break are one minute each, and the six meter stop on this
        // profile runs far longer than the two intervals a second break requires.
        var nitrox50 = GasMixture.FromPercent(50, 0);
        var settings = TestFactory.CreateSettings(lastStopAtSixMeters: true, oxygenBreaks: true,
            oxygenBreakInterval: TimeSpan.FromMinutes(1), oxygenBreakDuration: TimeSpan.FromMinutes(1));
        var cylinders = new[]
        {
            TestFactory.CreateCylinder(),
            TestFactory.CreateCylinder(nitrox50),
            TestFactory.CreateCylinder(GasMixture.Oxygen)
        };
        var algorithm = new BuhlmannZhl16cAlgorithm(0.3, 0.7);
        var request = TestFactory.CreateRequest(45, 30, cylinders, settings);
        var state = algorithm.BeginDive(request);
        algorithm.LoadSegment(state, new DiveSegment(Depth.FromMeter(45), TimeSpan.FromMinutes(30),
            GasMixture.Air, SegmentKind.Bottom));

        // Act
        var ascent = algorithm.CalculateFinalAscent(state, request);

        // Assert
        var breaks = ascent.Count(segment => segment.Kind == SegmentKind.Stop
                                             && segment.Depth.InMeter <= 6.0 + 1e-9
                                             && segment.Gas == nitrox50);
        Assert.True(breaks >= 2);
    }

    [Fact]
    public void CalculateFinalAscent_ShouldNotInsertOxygenBreaks_WhenTheyAreDisabled()
    {
        // Arrange
        var nitrox50 = GasMixture.FromPercent(50, 0);
        var settings = TestFactory.CreateSettings(lastStopAtSixMeters: true);
        var cylinders = new[]
        {
            TestFactory.CreateCylinder(),
            TestFactory.CreateCylinder(nitrox50),
            TestFactory.CreateCylinder(GasMixture.Oxygen)
        };
        var algorithm = new BuhlmannZhl16cAlgorithm(0.3, 0.7);
        var request = TestFactory.CreateRequest(45, 30, cylinders, settings);
        var state = algorithm.BeginDive(request);
        algorithm.LoadSegment(state, new DiveSegment(Depth.FromMeter(45), TimeSpan.FromMinutes(30),
            GasMixture.Air, SegmentKind.Bottom));

        // Act
        var ascent = algorithm.CalculateFinalAscent(state, request);

        // Assert
        Assert.All(ascent.Where(segment => segment.Kind == SegmentKind.Stop
                                           && segment.Depth.InMeter <= 6.0 + 1e-9),
            stop => Assert.Equal(GasMixture.Oxygen, stop.Gas));
    }

    [Fact]
    public void CalculateFinalAscent_ShouldNotInsertOxygenBreaks_WhenBreathingARebreatherLoop()
    {
        // Arrange
        // The loop is breathed on its diluent, never on pure oxygen, so no break is ever due
        // however rich the loop runs.
        var settings = TestFactory.CreateSettings(lastStopAtSixMeters: true, oxygenBreaks: true,
            oxygenBreakInterval: TimeSpan.FromMinutes(1), oxygenBreakDuration: TimeSpan.FromMinutes(1));
        var cylinders = new[]
        {
            new Cylinder(GasMixture.Air, Volume.FromLiter(12), Pressure.FromBar(200), CylinderPurpose.Diluent),
            new Cylinder(GasMixture.Oxygen, Volume.FromLiter(3), Pressure.FromBar(200), CylinderPurpose.Oxygen)
        };
        var loop = BreathingLoop.ClosedCircuit(Pressure.FromBar(1.3));
        var algorithm = new BuhlmannZhl16cAlgorithm(0.3, 0.7);
        var request = TestFactory.CreateRequest(45, 30, cylinders, settings);
        var state = algorithm.BeginDive(request);
        algorithm.LoadSegment(state, new DiveSegment(Depth.FromMeter(45), TimeSpan.FromMinutes(30),
            GasMixture.Air, SegmentKind.Bottom, loop));

        // Act
        var ascent = algorithm.CalculateFinalAscent(state, request);

        // Assert
        Assert.All(ascent, segment => Assert.Equal(GasMixture.Air, segment.Gas));
        Assert.Equal(0.0, ascent[^1].Depth.InMeter, Precision);
    }

    [Fact]
    public void CalculateFinalAscent_ShouldRaiseTheLoopToTheDecompressionSetpoint_WhenTheAscentBegins()
    {
        // Arrange
        var cylinders = new[]
        {
            new Cylinder(GasMixture.Air, Volume.FromLiter(12), Pressure.FromBar(200), CylinderPurpose.Diluent),
            new Cylinder(GasMixture.Oxygen, Volume.FromLiter(3), Pressure.FromBar(200), CylinderPurpose.Oxygen)
        };
        var loop = BreathingLoop.ClosedCircuit(Pressure.FromBar(1.0), Pressure.FromBar(1.6));
        var algorithm = new BuhlmannZhl16cAlgorithm(0.3, 0.7);
        var request = TestFactory.CreateRequest(45, 30, cylinders);
        var state = algorithm.BeginDive(request);
        algorithm.LoadSegment(state, new DiveSegment(Depth.FromMeter(45), TimeSpan.FromMinutes(30),
            GasMixture.Air, SegmentKind.Bottom, loop));

        // Act
        var ascent = algorithm.CalculateFinalAscent(state, request);

        // Assert
        Assert.NotEmpty(ascent);
        Assert.All(ascent, segment => Assert.Equal(1600.0, segment.Loop.Setpoint.InMillibar, Precision));
    }

    [Fact]
    public void CalculateFinalAscent_ShouldShortenDecompression_WhenTheDecompressionSetpointIsRaised()
    {
        // Arrange
        // A richer loop during the ascent leaves less room for inert gas, so the tissues
        // offgas faster and the stops are shorter.
        var cylinders = new[]
        {
            new Cylinder(GasMixture.Air, Volume.FromLiter(12), Pressure.FromBar(200), CylinderPurpose.Diluent),
            new Cylinder(GasMixture.Oxygen, Volume.FromLiter(3), Pressure.FromBar(200), CylinderPurpose.Oxygen)
        };

        static TimeSpan TotalStopTime(IReadOnlyList<DiveSegment> ascent)
        {
            var total = TimeSpan.Zero;
            for (var i = 0; i < ascent.Count; i++)
            {
                if (ascent[i].Kind == SegmentKind.Stop)
                {
                    total += ascent[i].Duration;
                }
            }

            return total;
        }

        TimeSpan Plan(BreathingLoop loop)
        {
            var algorithm = new BuhlmannZhl16cAlgorithm(0.3, 0.7);
            var request = TestFactory.CreateRequest(45, 30, cylinders);
            var state = algorithm.BeginDive(request);
            algorithm.LoadSegment(state, new DiveSegment(Depth.FromMeter(45), TimeSpan.FromMinutes(30),
                GasMixture.Air, SegmentKind.Bottom, loop));
            return TotalStopTime(algorithm.CalculateFinalAscent(state, request));
        }

        // Act
        var held = Plan(BreathingLoop.ClosedCircuit(Pressure.FromBar(1.0)));
        var raised = Plan(BreathingLoop.ClosedCircuit(Pressure.FromBar(1.0), Pressure.FromBar(1.6)));

        // Assert
        Assert.True(raised < held);
    }

    private sealed class FakeDecompressionState : IDecompressionState;
}