using Bitenovac.DecompressionAlgorithms.Core.Equipment;
using Bitenovac.DecompressionAlgorithms.Core.Planning;
using Bitenovac.DecompressionAlgorithms.Units;

namespace Bitenovac.DecompressionAlgorithms.Zhl16c.E2E.Tests;

/// <summary>
/// Validates complete passive semi-closed rebreather plans against schedules produced by
/// other established implementations of the ZH-L16C model. Each test fixes every input of
/// one realistic technical dive and writes the full expanded schedule, gas usage, and
/// oxygen exposure to the test output so the numbers can be compared against the reference
/// implementation. The assertions are placeholders until the reference values are
/// transcribed.
/// <para>
/// Conventions shared by all scenarios: level durations are the time spent at the level;
/// the descent and any inter-level travel are generated in addition. The dives are planned
/// on the loop itself, with the loop supplies carried as
/// <see cref="CylinderPurpose.Diluent" /> cylinders and oxygen carried for bailout only;
/// configure the reference implementation with the same dump ratio. The loop's oxygen
/// shortfall is fixed at the bottom breathing rate for the whole dive, so configure the
/// reference implementation the same way.
/// </para>
/// <para>
/// The mode-comparison scenarios plan the same four dives that
/// <see cref="DivePlannerZhl16cOpenCircuitTests" /> and
/// <see cref="DivePlannerZhl16cClosedCircuitTests" /> plan on the other apparatus, so that
/// the only difference between the three schedules is the breathing apparatus. The loop
/// vents one part in ten of every breath and is fed from the ladder down to nitrox 50,
/// switching supply as each becomes breathable within the decompression oxygen limit.
/// Oxygen breaks are disabled throughout.
/// </para>
/// </summary>
public sealed class DivePlannerZhl16cSemiClosedTests
{
    private const double DumpRatio = 0.1;

    private static readonly GasMixture Trimix2135 = GasMixture.FromPercent(21, 35);
    private static readonly GasMixture Trimix1845 = GasMixture.FromPercent(18, 45);
    private static readonly GasMixture Trimix1555 = GasMixture.FromPercent(15, 55);
    private static readonly GasMixture Trimix1070 = GasMixture.FromPercent(10, 70);
    private static readonly GasMixture Trimix3525 = GasMixture.FromPercent(35, 25);
    private static readonly GasMixture Nitrox50 = GasMixture.FromPercent(50, 0);

    private readonly ITestOutputHelper _output;

    public DivePlannerZhl16cSemiClosedTests(ITestOutputHelper output) => _output = output;

    /// <summary>
    /// The settings shared by every mode-comparison scenario: salt water at standard
    /// sea-level pressure and otherwise the shared baseline.
    /// </summary>
    private static DivePlanSettings CreateComparisonSettings() =>
        TestFactory.CreateSettings(surfacePressureMillibar: 1013.25);

    /// <summary>
    /// Creates the semi-closed set: the bottom mix and the whole ladder as loop supplies, so
    /// the loop is fed from the richest gas the depth allows, with oxygen carried for bailout.
    /// </summary>
    private static Cylinder[] SemiClosedCylinders(GasMixture bottom, params GasMixture[] ladder)
    {
        var cylinders = new Cylinder[ladder.Length + 2];
        cylinders[0] = new Cylinder(bottom, Volume.FromLiter(24), Pressure.FromBar(232),
            CylinderPurpose.Diluent);
        for (var i = 0; i < ladder.Length; i++)
        {
            cylinders[i + 1] = new Cylinder(ladder[i], Volume.FromLiter(22.2), Pressure.FromBar(207),
                CylinderPurpose.Diluent);
        }

        cylinders[^1] = new Cylinder(GasMixture.Oxygen, Volume.FromLiter(22.2), Pressure.FromBar(207),
            CylinderPurpose.Bailout);
        return cylinders;
    }

    /// <summary>
    /// The semi-closed loop, venting one part in ten of every breath, with its oxygen
    /// shortfall taken at the working breathing rate for the whole dive.
    /// </summary>
    private static BreathingLoop SemiClosedLoop(DivePlanSettings settings) =>
        BreathingLoop.SemiClosed(DumpRatio,
            BreathingLoop.SemiClosedOxygenDropCoefficient(DumpRatio,
                settings.BottomMetabolicOxygenConsumptionLitersPerMinute,
                settings.BottomSacLitersPerMinute,
                settings.SurfacePressure));

    /// <summary>
    /// Passive semi-closed rebreather to 60 m for 30 minutes, venting one part in ten of
    /// every breath. Trimix 16/50 is the deep supply and nitrox 50 the shallow one, so the
    /// planner feeds the loop from the richer supply once it is within the decompression
    /// oxygen limit, as a diver would to keep the loop out of hypoxia on the stops. Oxygen
    /// is carried for bailout only. The loop's oxygen shortfall is fixed at the bottom
    /// breathing rate for the whole dive, so configure the reference implementation the same
    /// way. Deco SAC 13 L/min, last stop at three meters, GF 35/75.
    /// </summary>
    [Fact]
    public void CreatePlan_ShouldMatchReferenceSchedule_WhenSemiClosedAt60MetersOnTrimix1650SwitchingToNitrox50()
    {
        // Arrange
        Cylinder[] cylinders =
        [
            TestFactory.CreateCylinder(16, 50, 24, 232, CylinderPurpose.Diluent),
            TestFactory.CreateCylinder(50, 0, 11.1, 200, CylinderPurpose.Diluent),
            TestFactory.CreateCylinder(100, 0, 7, 200, CylinderPurpose.Bailout)
        ];
        var settings = TestFactory.CreateSettings(decoSacLitersPerMinute: 13,
            lastStopAtSixMeters: false);
        var loop = BreathingLoop.SemiClosed(0.1,
            BreathingLoop.SemiClosedOxygenDropCoefficient(0.1,
                settings.BottomMetabolicOxygenConsumptionLitersPerMinute,
                settings.BottomSacLitersPerMinute,
                settings.SurfacePressure));
        var request = new DivePlanRequest(TestFactory.CreateProfile(loop, (60, 30)), cylinders,
            settings);

        // Act
        var plan = TestFactory.CreatePlan(request, 0.35, 0.75);

        // Assert
        _output.WriteLine(plan.ToString());
        Assert.True(false);
    }

    // ---------------------------------------------------------------------------------
    // Mode comparison: 45 m for 25 minutes on normoxic trimix 21/35, ladder nitrox 50
    // and oxygen, GF 30/85.
    // ---------------------------------------------------------------------------------

    [Fact]
    public void CreatePlan_ShouldMatchReferenceSchedule_WhenSemiClosedAt45MetersOnTrimix2135()
    {
        // Arrange
        var settings = CreateComparisonSettings();
        var request = new DivePlanRequest(TestFactory.CreateProfile(SemiClosedLoop(settings), (45, 25)),
            SemiClosedCylinders(Trimix2135, Nitrox50), settings);

        // Act
        var plan = TestFactory.CreatePlan(request, 0.3, 0.85);

        // Assert
        _output.WriteLine(plan.ToString());
        Assert.True(false);
    }

    // ---------------------------------------------------------------------------------
    // Mode comparison: 60 m for 20 minutes on trimix 18/45, ladder trimix 35/25,
    // nitrox 50, and oxygen, GF 30/80.
    // ---------------------------------------------------------------------------------

    [Fact]
    public void CreatePlan_ShouldMatchReferenceSchedule_WhenSemiClosedAt60MetersOnTrimix1845()
    {
        // Arrange
        var settings = CreateComparisonSettings();
        var request = new DivePlanRequest(TestFactory.CreateProfile(SemiClosedLoop(settings), (60, 20)),
            SemiClosedCylinders(Trimix1845, Trimix3525, Nitrox50), settings);

        // Act
        var plan = TestFactory.CreatePlan(request, 0.3, 0.8);

        // Assert
        _output.WriteLine(plan.ToString());
        Assert.True(false);
    }

    // ---------------------------------------------------------------------------------
    // Mode comparison: 75 m for 20 minutes on trimix 15/55, ladder trimix 35/25,
    // nitrox 50, and oxygen, GF 25/80.
    // ---------------------------------------------------------------------------------

    [Fact]
    public void CreatePlan_ShouldMatchReferenceSchedule_WhenSemiClosedAt75MetersOnTrimix1555()
    {
        // Arrange
        var settings = CreateComparisonSettings();
        var request = new DivePlanRequest(TestFactory.CreateProfile(SemiClosedLoop(settings), (75, 20)),
            SemiClosedCylinders(Trimix1555, Trimix3525, Nitrox50), settings);

        // Act
        var plan = TestFactory.CreatePlan(request, 0.25, 0.8);

        // Assert
        _output.WriteLine(plan.ToString());
        Assert.True(false);
    }

    // ---------------------------------------------------------------------------------
    // Mode comparison: 100 m for 15 minutes on hypoxic trimix 10/70, ladder trimix
    // 21/35, trimix 35/25, nitrox 50, and oxygen, GF 20/80.
    // ---------------------------------------------------------------------------------

    [Fact]
    public void CreatePlan_ShouldMatchReferenceSchedule_WhenSemiClosedAt100MetersOnTrimix1070()
    {
        // Arrange
        var settings = CreateComparisonSettings();
        var request = new DivePlanRequest(TestFactory.CreateProfile(SemiClosedLoop(settings), (100, 15)),
            SemiClosedCylinders(Trimix1070, Trimix2135, Trimix3525, Nitrox50), settings);

        // Act
        var plan = TestFactory.CreatePlan(request, 0.2, 0.8);

        // Assert
        _output.WriteLine(plan.ToString());
        Assert.True(false);
    }
}
