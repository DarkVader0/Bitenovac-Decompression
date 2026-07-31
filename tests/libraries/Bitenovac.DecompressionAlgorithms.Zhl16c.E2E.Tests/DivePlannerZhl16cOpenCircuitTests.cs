// using Bitenovac.DecompressionAlgorithms.Core.Environment;
// using Bitenovac.DecompressionAlgorithms.Core.Equipment;
// using Bitenovac.DecompressionAlgorithms.Core.Planning;
// using Bitenovac.DecompressionAlgorithms.Units;
//
// namespace Bitenovac.DecompressionAlgorithms.Zhl16c.E2E.Tests;
//
// /// <summary>
// /// Validates complete open-circuit plans against schedules produced by other established
// /// implementations of the ZH-L16C model. Each test fixes every input of one realistic
// /// technical dive and writes the full expanded schedule, gas usage, and oxygen exposure to
// /// the test output so the numbers can be compared against the reference implementation. The
// /// assertions are placeholders until the reference values are transcribed.
// /// <para>
// /// Conventions shared by all scenarios: level durations are the time spent at the level;
// /// the descent and any inter-level travel are generated in addition. The oxygen-is-narcotic
// /// flag is recorded so the reference implementation can be configured identically even
// /// though it does not alter the schedule here.
// /// </para>
// /// <para>
// /// The mode-comparison scenarios plan the same four dives that
// /// <see cref="DivePlannerZhl16cClosedCircuitTests" /> and
// /// <see cref="DivePlannerZhl16cSemiClosedTests" /> plan on the loop, with the ladder breathed
// /// down as decompression gas, so that the only difference between the three schedules is the
// /// breathing apparatus. Oxygen breaks are disabled on those scenarios: they would fire only
// /// on the open-circuit plans, where the diver reaches pure oxygen, and would make the three
// /// schedules harder to compare. Enable them on the reference implementation only if you
// /// enable them here.
// /// </para>
// /// </summary>
// public sealed class DivePlannerZhl16cOpenCircuitTests
// {
//     private static readonly GasMixture Trimix2135 = GasMixture.FromPercent(21, 35);
//     private static readonly GasMixture Trimix1845 = GasMixture.FromPercent(18, 45);
//     private static readonly GasMixture Trimix1555 = GasMixture.FromPercent(15, 55);
//     private static readonly GasMixture Trimix1070 = GasMixture.FromPercent(10, 70);
//     private static readonly GasMixture Trimix3525 = GasMixture.FromPercent(35, 25);
//     private static readonly GasMixture Nitrox50 = GasMixture.FromPercent(50, 0);
//
//     private readonly ITestOutputHelper _output;
//
//     public DivePlannerZhl16cOpenCircuitTests(ITestOutputHelper output) => _output = output;
//
//     /// <summary>
//     /// The settings shared by every mode-comparison scenario: salt water at standard
//     /// sea-level pressure and otherwise the shared baseline.
//     /// </summary>
//     private static DivePlanSettings CreateComparisonSettings() =>
//         TestFactory.CreateSettings(1013.25);
//
//     /// <summary>Creates the open-circuit set: the bottom mix, then the ladder as decompression gas.</summary>
//     private static Cylinder[] OpenCircuitCylinders(GasMixture bottom, params GasMixture[] ladder)
//     {
//         var cylinders = new Cylinder[ladder.Length + 2];
//         cylinders[0] = new Cylinder(bottom, Volume.FromLiter(24), Pressure.FromBar(232),
//             CylinderPurpose.BottomGas);
//         for (var i = 0; i < ladder.Length; i++)
//         {
//             cylinders[i + 1] = StageCylinder(ladder[i]);
//         }
//
//         cylinders[^1] = StageCylinder(GasMixture.Oxygen);
//         return cylinders;
//     }
//
//     private static Cylinder StageCylinder(GasMixture gas) =>
//         new(gas, Volume.FromLiter(22.2), Pressure.FromBar(207), CylinderPurpose.DecoGas);
//
//     /// <summary>
//     /// Open-circuit wreck dive: twin 24 L of air to 42 m for 25 minutes on the seabed, then
//     /// 15 minutes on the deck at 27 m, decompressing on an 11.1 L stage of nitrox 50.
//     /// Baseline settings, GF 30/70.
//     /// </summary>
//     [Fact]
//     public void CreatePlan_ShouldMatchReferenceSchedule_WhenAirWreckAt42MetersWithNitrox50Deco()
//     {
//         // Arrange
//         Cylinder[] cylinders =
//         [
//             TestFactory.CreateCylinder(21, 0, 24, 232, CylinderPurpose.BottomGas),
//             TestFactory.CreateCylinder(50, 0, 11.1, 200, CylinderPurpose.DecoGas)
//         ];
//         var request = new DivePlanRequest(TestFactory.CreateProfile((42, 25), (27, 15)),
//             cylinders, TestFactory.CreateSettings());
//
//         // Act
//         var plan = TestFactory.CreatePlan(request, 0.3, 0.7);
//
//         // Assert
//         _output.WriteLine(plan.ToString());
//         Assert.True(false);
//     }
//
//     /// <summary>
//     /// Old-school deep air: 55 m for 20 minutes on twin 24 L of air with nitrox 50 and
//     /// oxygen carried for decompression, planned on raw Bühlmann (GF 100/100). Uniform
//     /// 10 m/min ascent to the stops, oxygen breaks every 20 minutes for 5, oxygen counted
//     /// as narcotic.
//     /// </summary>
//     [Fact]
//     public void CreatePlan_ShouldMatchReferenceSchedule_WhenDeepAirAt55MetersWithRawBuhlmannGradientFactors()
//     {
//         // Arrange
//         Cylinder[] cylinders =
//         [
//             TestFactory.CreateCylinder(21, 0, 24, 232, CylinderPurpose.BottomGas),
//             TestFactory.CreateCylinder(50, 0, 11.1, 200, CylinderPurpose.DecoGas),
//             TestFactory.CreateCylinder(100, 0, 7, 200, CylinderPurpose.DecoGas)
//         ];
//         var settings = TestFactory.CreateSettings(
//             ascentRateBelow75PercentMetersPerMinute: 10,
//             ascentRate75To50PercentMetersPerMinute: 10,
//             ascentRate50PercentToStopsMetersPerMinute: 10,
//             oxygenBreaks: true);
//         var request = new DivePlanRequest(TestFactory.CreateProfile((55, 20)), cylinders,
//             settings);
//
//         // Act
//         var plan = TestFactory.CreatePlan(request, 1, 1);
//
//         // Assert
//         _output.WriteLine(plan.ToString());
//         Assert.True(false);
//     }
//
//     /// <summary>
//     /// Multi-level nitrox reef dive: 15 L of nitrox 32 for 20 minutes at 33 m followed by
//     /// 20 minutes at 21 m, with nitrox 50 for decompression. Slow 12 m/min descent,
//     /// instant gas switches, last stop at three meters, a safety stop requested should no
//     /// decompression be required, GF 45/95.
//     /// </summary>
//     [Fact]
//     public void CreatePlan_ShouldMatchReferenceSchedule_WhenMultiLevelNitrox32ReefWithInstantGasSwitches()
//     {
//         // Arrange
//         Cylinder[] cylinders =
//         [
//             TestFactory.CreateCylinder(32, 0, 15, 232, CylinderPurpose.BottomGas),
//             TestFactory.CreateCylinder(50, 0, 11.1, 200, CylinderPurpose.DecoGas)
//         ];
//         var settings = TestFactory.CreateSettings(
//             descentRateMetersPerMinute: 12,
//             gasSwitchMinutes: 0,
//             safetyStop: true,
//             lastStopAtSixMeters: false);
//         var request = new DivePlanRequest(TestFactory.CreateProfile((33, 20), (21, 20)),
//             cylinders, settings);
//
//         // Act
//         var plan = TestFactory.CreatePlan(request, 0.45, 0.95);
//
//         // Assert
//         _output.WriteLine(plan.ToString());
//         Assert.True(false);
//     }
//
//     /// <summary>
//     /// Solo normoxic trimix in a fresh-water lake: twin 24 L of trimix 21/35 to 45 m for
//     /// 30 minutes with a single nitrox 50 deco stage. 15 m/min descent, last stop at three
//     /// meters, reserve for a single diver at stress factor 1.5, GF 35/75.
//     /// </summary>
//     [Fact]
//     public void CreatePlan_ShouldMatchReferenceSchedule_WhenNormoxicTrimix2135At45MetersInAFreshWaterLake()
//     {
//         // Arrange
//         Cylinder[] cylinders =
//         [
//             TestFactory.CreateCylinder(21, 35, 24, 232, CylinderPurpose.BottomGas),
//             TestFactory.CreateCylinder(50, 0, 11.1, 200, CylinderPurpose.DecoGas)
//         ];
//         var settings = TestFactory.CreateSettings(
//             salinity: Salinity.Fresh,
//             descentRateMetersPerMinute: 15,
//             reserveStressFactor: 1.5,
//             reserveTeamSize: 1,
//             lastStopAtSixMeters: false);
//         var request = new DivePlanRequest(TestFactory.CreateProfile((45, 30)), cylinders,
//             settings);
//
//         // Act
//         var plan = TestFactory.CreatePlan(request, 0.35, 0.75);
//
//         // Assert
//         _output.WriteLine(plan.ToString());
//         Assert.True(false);
//     }
//
//     /// <summary>
//     /// The canonical trimix dive: twin 24 L of trimix 18/45 to 60 m for 25 minutes with
//     /// nitrox 50 and oxygen for decompression, in the sea at standard 1013.25 mbar surface
//     /// pressure. Three-minute gas switches, GF 30/85.
//     /// </summary>
//     [Fact]
//     public void CreatePlan_ShouldMatchReferenceSchedule_WhenTrimix1845At60MetersWithNitrox50AndOxygen()
//     {
//         // Arrange
//         Cylinder[] cylinders =
//         [
//             TestFactory.CreateCylinder(18, 45, 24, 232, CylinderPurpose.BottomGas),
//             TestFactory.CreateCylinder(50, 0, 11.1, 200, CylinderPurpose.DecoGas),
//             TestFactory.CreateCylinder(100, 0, 7, 200, CylinderPurpose.DecoGas)
//         ];
//         var settings = TestFactory.CreateSettings(1013.25,
//             gasSwitchMinutes: 3);
//         var request = new DivePlanRequest(TestFactory.CreateProfile((60, 25)), cylinders,
//             settings);
//
//         // Act
//         var plan = TestFactory.CreatePlan(request, 0.3, 0.85);
//
//         // Assert
//         _output.WriteLine(plan.ToString());
//         Assert.True(false);
//     }
//
//     /// <summary>
//     /// Deep trimix with a trimix decompression gas: twin 24 L of trimix 15/55 to 75 m for
//     /// 20 minutes, decompressing on trimix 35/25, nitrox 50, and oxygen. Gas switches only
//     /// at required stops, 20 m/min descent, 30-second stop rounding, oxygen breaks every
//     /// 20 minutes for 5, oxygen not counted as narcotic, GF 30/80.
//     /// </summary>
//     [Fact]
//     public void CreatePlan_ShouldMatchReferenceSchedule_WhenTrimix1555At75MetersSwitchingOnlyAtRequiredStops()
//     {
//         // Arrange
//         Cylinder[] cylinders =
//         [
//             TestFactory.CreateCylinder(15, 55, 24, 232, CylinderPurpose.BottomGas),
//             TestFactory.CreateCylinder(35, 25, 11.1, 200, CylinderPurpose.DecoGas),
//             TestFactory.CreateCylinder(50, 0, 11.1, 200, CylinderPurpose.DecoGas),
//             TestFactory.CreateCylinder(100, 0, 7, 200, CylinderPurpose.DecoGas)
//         ];
//         var settings = TestFactory.CreateSettings(
//             descentRateMetersPerMinute: 20,
//             stopTimeIncrementMinutes: 0.5,
//             switchAtRequiredStop: true,
//             oxygenBreaks: true,
//             oxygenIsNarcotic: false);
//         var request = new DivePlanRequest(TestFactory.CreateProfile((75, 20)), cylinders,
//             settings);
//
//         // Act
//         var plan = TestFactory.CreatePlan(request, 0.3, 0.8);
//
//         // Assert
//         _output.WriteLine(plan.ToString());
//         Assert.True(false);
//     }
//
//     /// <summary>
//     /// Hypoxic trimix to 100 m for 15 minutes on twin 24 L of trimix 10/70, decompressing
//     /// on trimix 21/35, nitrox 50, and oxygen. 20 m/min descent, SAC 20/15 L/min, reserve
//     /// stress factor 3, oxygen breaks every 25 minutes for 5, oxygen not counted as
//     /// narcotic, GF 25/80.
//     /// </summary>
//     [Fact]
//     public void CreatePlan_ShouldMatchReferenceSchedule_WhenHypoxicTrimix1070At100MetersWithThreeDecoGases()
//     {
//         // Arrange
//         Cylinder[] cylinders =
//         [
//             TestFactory.CreateCylinder(10, 70, 24, 232, CylinderPurpose.BottomGas),
//             TestFactory.CreateCylinder(21, 35, 11.1, 207, CylinderPurpose.DecoGas),
//             TestFactory.CreateCylinder(50, 0, 22.2, 207, CylinderPurpose.DecoGas),
//             TestFactory.CreateCylinder(100, 0, 11.1, 200, CylinderPurpose.DecoGas)
//         ];
//         var settings = TestFactory.CreateSettings(
//             descentRateMetersPerMinute: 20,
//             bottomSacLitersPerMinute: 20,
//             decoSacLitersPerMinute: 15,
//             reserveStressFactor: 3,
//             oxygenBreakIntervalMinutes: 25,
//             oxygenBreaks: true,
//             oxygenIsNarcotic: false);
//         var request = new DivePlanRequest(TestFactory.CreateProfile((100, 15)), cylinders,
//             settings);
//
//         // Act
//         var plan = TestFactory.CreatePlan(request, 0.25, 0.8);
//
//         // Assert
//         _output.WriteLine(plan.ToString());
//         Assert.True(false);
//     }
//
//     /// <summary>
//     /// Baltic wreck in brackish water: twin 24 L of trimix 12/60 to 90 m for 15 minutes,
//     /// decompressing on trimix 21/35, nitrox 50, and nitrox 80 instead of pure oxygen.
//     /// Deco pO2 capped at 1.5 bar, SAC 16/12 L/min, 50 bar reserve, last stop at three
//     /// meters, oxygen not counted as narcotic, GF 20/85.
//     /// </summary>
//     [Fact]
//     public void CreatePlan_ShouldMatchReferenceSchedule_WhenTrimix1260At90MetersInBrackishWaterWithNitrox80()
//     {
//         // Arrange
//         Cylinder[] cylinders =
//         [
//             TestFactory.CreateCylinder(12, 60, 24, 232, CylinderPurpose.BottomGas),
//             TestFactory.CreateCylinder(21, 35, 11.1, 207, CylinderPurpose.DecoGas),
//             TestFactory.CreateCylinder(50, 0, 11.1, 207, CylinderPurpose.DecoGas),
//             TestFactory.CreateCylinder(80, 0, 11.1, 200, CylinderPurpose.DecoGas)
//         ];
//         var settings = TestFactory.CreateSettings(
//             salinity: Salinity.Brackish,
//             bottomSacLitersPerMinute: 16,
//             decoSacLitersPerMinute: 12,
//             decoPo2Bar: 1.5,
//             reservePressureBar: 50,
//             lastStopAtSixMeters: false,
//             oxygenIsNarcotic: false);
//         var request = new DivePlanRequest(TestFactory.CreateProfile((90, 15)), cylinders,
//             settings);
//
//         // Act
//         var plan = TestFactory.CreatePlan(request, 0.2, 0.85);
//
//         // Assert
//         _output.WriteLine(plan.ToString());
//         Assert.True(false);
//     }
//
//     /// <summary>
//     /// Mountain-lake trimix at 2200 m altitude in fresh water: twin 24 L of trimix 18/45
//     /// to 52 m for 20 minutes with nitrox 50 and oxygen for decompression. Surface
//     /// pressure from the barometric formula, 15 m/min descent, last stop at three meters,
//     /// GF 30/75.
//     /// </summary>
//     [Fact]
//     public void CreatePlan_ShouldMatchReferenceSchedule_WhenTrimix1845At52MetersInAMountainLakeAt2200Meters()
//     {
//         // Arrange
//         Cylinder[] cylinders =
//         [
//             TestFactory.CreateCylinder(18, 45, 24, 232, CylinderPurpose.BottomGas),
//             TestFactory.CreateCylinder(50, 0, 11.1, 200, CylinderPurpose.DecoGas),
//             TestFactory.CreateCylinder(100, 0, 7, 200, CylinderPurpose.DecoGas)
//         ];
//         var settings = TestFactory.CreateSettings(
//             PhysicalConstants.AtmosphericPressureAtAltitudeMillibar(2200),
//             Salinity.Fresh,
//             15,
//             lastStopAtSixMeters: false);
//         var request = new DivePlanRequest(TestFactory.CreateProfile((52, 20)), cylinders,
//             settings);
//
//         // Act
//         var plan = TestFactory.CreatePlan(request, 0.3, 0.75);
//
//         // Assert
//         _output.WriteLine(plan.ToString());
//         Assert.True(false);
//     }
//
//     /// <summary>
//     /// Repetitive trimix: a first dive to 45 m for 25 minutes on trimix 21/35 with nitrox
//     /// 50, a two-hour surface interval breathing air, then the planned dive to 40 m for
//     /// 20 minutes on the same gases. Baseline settings for both dives, GF 30/85.
//     /// </summary>
//     [Fact]
//     public void CreatePlan_ShouldMatchReferenceSchedule_WhenRepetitiveTrimixDiveFollowsATwoHourSurfaceInterval()
//     {
//         // Arrange
//         Cylinder[] cylinders =
//         [
//             TestFactory.CreateCylinder(21, 35, 24, 232, CylinderPurpose.BottomGas),
//             TestFactory.CreateCylinder(50, 0, 11.1, 200, CylinderPurpose.DecoGas)
//         ];
//         var priorDive = new PriorDive(TestFactory.CreateProfile((45, 25)), cylinders,
//             TestFactory.CreateSettings(), TimeSpan.FromMinutes(120), GasMixture.Air);
//         var request = new DivePlanRequest(TestFactory.CreateProfile((40, 20)), cylinders,
//             TestFactory.CreateSettings(), [priorDive]);
//
//         // Act
//         var plan = TestFactory.CreatePlan(request, 0.3, 0.85);
//
//         // Assert
//         _output.WriteLine(plan.ToString());
//         Assert.True(false);
//     }
//
//     /// <summary>
//     /// Third air dive of the day: 40 m for 20 minutes followed by 150 minutes on air at
//     /// the surface, 32 m for 25 minutes followed by 90 minutes on pure oxygen at the
//     /// surface, then the planned dive to 28 m for 30 minutes. Every dive carries 15 L of
//     /// air with a nitrox 50 stage, reserve for a three-diver team, last stop at three
//     /// meters, GF 40/85.
//     /// </summary>
//     [Fact]
//     public void CreatePlan_ShouldMatchReferenceSchedule_WhenThirdAirDiveOfTheDayAfterOxygenDuringTheSecondInterval()
//     {
//         // Arrange
//         Cylinder[] cylinders =
//         [
//             TestFactory.CreateCylinder(21, 0, 15, 232, CylinderPurpose.BottomGas),
//             TestFactory.CreateCylinder(50, 0, 11.1, 200, CylinderPurpose.DecoGas)
//         ];
//         var settings = TestFactory.CreateSettings(reserveTeamSize: 3, lastStopAtSixMeters: false);
//         var firstDive = new PriorDive(TestFactory.CreateProfile((40, 20)), cylinders,
//             settings, TimeSpan.FromMinutes(150), GasMixture.Air);
//         var secondDive = new PriorDive(TestFactory.CreateProfile((32, 25)), cylinders,
//             settings, TimeSpan.FromMinutes(90), GasMixture.Oxygen);
//         var request = new DivePlanRequest(TestFactory.CreateProfile((28, 30)), cylinders,
//             settings, [firstDive, secondDive]);
//
//         // Act
//         var plan = TestFactory.CreatePlan(request, 0.4, 0.85);
//
//         // Assert
//         _output.WriteLine(plan.ToString());
//         Assert.True(false);
//     }
//
//     /// <summary>
//     /// Expedition dive to 120 m for 12 minutes on 32 L of trimix 8/80, decompressing on
//     /// trimix 18/45, trimix 35/25, nitrox 50, and oxygen. 20 m/min descent, ascent bands
//     /// 9/6/6 m/min with the final six meters at 1 m/min, deco SAC 12 L/min, three minutes
//     /// of problem-solving time, oxygen breaks every 25 minutes for 6, oxygen not counted
//     /// as narcotic, GF 20/75.
//     /// </summary>
//     [Fact]
//     public void CreatePlan_ShouldMatchReferenceSchedule_WhenExpeditionTrimix880At120MetersWithFourDecoGases()
//     {
//         // Arrange
//         Cylinder[] cylinders =
//         [
//             TestFactory.CreateCylinder(8, 80, 32, 232, CylinderPurpose.BottomGas),
//             TestFactory.CreateCylinder(18, 45, 11.1, 207, CylinderPurpose.DecoGas),
//             TestFactory.CreateCylinder(35, 25, 11.1, 207, CylinderPurpose.DecoGas),
//             TestFactory.CreateCylinder(50, 0, 22.2, 207, CylinderPurpose.DecoGas),
//             TestFactory.CreateCylinder(100, 0, 22.2, 200, CylinderPurpose.DecoGas)
//         ];
//         var settings = TestFactory.CreateSettings(
//             descentRateMetersPerMinute: 20,
//             ascentRate75To50PercentMetersPerMinute: 6,
//             ascentRateLastSixMetersMetersPerMinute: 1,
//             decoSacLitersPerMinute: 12,
//             problemSolvingMinutes: 3,
//             oxygenBreakIntervalMinutes: 25,
//             oxygenBreakDurationMinutes: 6,
//             oxygenBreaks: true,
//             oxygenIsNarcotic: false);
//         var request = new DivePlanRequest(TestFactory.CreateProfile((120, 12)), cylinders,
//             settings);
//
//         // Act
//         var plan = TestFactory.CreatePlan(request, 0.2, 0.75);
//
//         // Assert
//         _output.WriteLine(plan.ToString());
//         Assert.True(false);
//     }
//
//     // ---------------------------------------------------------------------------------
//     // Mode comparison: 45 m for 25 minutes on normoxic trimix 21/35, ladder nitrox 50
//     // and oxygen, GF 30/85.
//     // ---------------------------------------------------------------------------------
//
//     [Fact]
//     public void CreatePlan_ShouldMatchReferenceSchedule_WhenOpenCircuitAt45MetersOnTrimix2135()
//     {
//         // Arrange
//         var settings = CreateComparisonSettings();
//         var request = new DivePlanRequest(TestFactory.CreateProfile((45, 25)),
//             OpenCircuitCylinders(Trimix2135, Nitrox50), settings);
//
//         // Act
//         var plan = TestFactory.CreatePlan(request, 0.3, 0.85);
//
//         // Assert
//         _output.WriteLine(plan.ToString());
//         Assert.True(false);
//     }
//
//     // ---------------------------------------------------------------------------------
//     // Mode comparison: 60 m for 20 minutes on trimix 18/45, ladder trimix 35/25,
//     // nitrox 50, and oxygen, GF 30/80.
//     // ---------------------------------------------------------------------------------
//
//     [Fact]
//     public void CreatePlan_ShouldMatchReferenceSchedule_WhenOpenCircuitAt60MetersOnTrimix1845()
//     {
//         // Arrange
//         var settings = CreateComparisonSettings();
//         var request = new DivePlanRequest(TestFactory.CreateProfile((60, 20)),
//             OpenCircuitCylinders(Trimix1845, Trimix3525, Nitrox50), settings);
//
//         // Act
//         var plan = TestFactory.CreatePlan(request, 0.3, 0.8);
//
//         // Assert
//         _output.WriteLine(plan.ToString());
//         Assert.True(false);
//     }
//
//     // ---------------------------------------------------------------------------------
//     // Mode comparison: 75 m for 20 minutes on trimix 15/55, ladder trimix 35/25,
//     // nitrox 50, and oxygen, GF 25/80.
//     // ---------------------------------------------------------------------------------
//
//     [Fact]
//     public void CreatePlan_ShouldMatchReferenceSchedule_WhenOpenCircuitAt75MetersOnTrimix1555()
//     {
//         // Arrange
//         var settings = CreateComparisonSettings();
//         var request = new DivePlanRequest(TestFactory.CreateProfile((75, 20)),
//             OpenCircuitCylinders(Trimix1555, Trimix3525, Nitrox50), settings);
//
//         // Act
//         var plan = TestFactory.CreatePlan(request, 0.25, 0.8);
//
//         // Assert
//         _output.WriteLine(plan.ToString());
//         Assert.True(false);
//     }
//
//     // ---------------------------------------------------------------------------------
//     // Mode comparison: 100 m for 15 minutes on hypoxic trimix 10/70, ladder trimix
//     // 21/35, trimix 35/25, nitrox 50, and oxygen, GF 20/80.
//     // ---------------------------------------------------------------------------------
//
//     [Fact]
//     public void CreatePlan_ShouldMatchReferenceSchedule_WhenOpenCircuitAt100MetersOnTrimix1070()
//     {
//         // Arrange
//         var settings = CreateComparisonSettings();
//         var request = new DivePlanRequest(TestFactory.CreateProfile((100, 15)),
//             OpenCircuitCylinders(Trimix1070, Trimix2135, Trimix3525, Nitrox50), settings);
//
//         // Act
//         var plan = TestFactory.CreatePlan(request, 0.2, 0.8);
//
//         // Assert
//         _output.WriteLine(plan.ToString());
//         Assert.True(false);
//     }
// }