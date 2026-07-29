// using Bitenovac.DecompressionAlgorithms.BuhlmannZhl16c;
// using Bitenovac.DecompressionAlgorithms.Core;
// using Bitenovac.DecompressionAlgorithms.Core.Environment;
// using Bitenovac.DecompressionAlgorithms.Core.Equipment;
// using Bitenovac.DecompressionAlgorithms.Core.Planning;
// using Bitenovac.DecompressionAlgorithms.Units;
//
// namespace Bitenovac.DecompressionAlgorithms.Zhl16c.Integration.Tests;
//
// /// <summary>
// /// Validates complete plans against schedules produced by other established implementations
// /// of the ZH-L16C model. Each test fixes every input of one realistic technical dive and
// /// writes the full expanded schedule, gas usage, and oxygen exposure to the test output so
// /// the numbers can be compared against the reference implementation. The assertions are
// /// placeholders until the reference values are transcribed.
// /// <para>
// /// Conventions shared by all scenarios: level durations are the time spent at the level;
// /// the descent and any inter-level travel are generated in addition. Rebreather scenarios
// /// are planned on the loop itself, with the loop gas carried as a
// /// <see cref="CylinderPurpose.Diluent" /> cylinder, the oxygen supply as
// /// <see cref="CylinderPurpose.Oxygen" />, and the open-circuit stages as
// /// <see cref="CylinderPurpose.Bailout" />; configure the reference implementation with the
// /// same setpoint or dump ratio. The oxygen-is-narcotic flag is recorded so the reference
// /// implementation can be configured identically even though it does not alter the schedule
// /// here.
// /// </para>
// /// </summary>
// public sealed class DivePlannerZhl16cKnownValueTests
// {
//     private readonly ITestOutputHelper _output;
//
//     public DivePlannerZhl16cKnownValueTests(ITestOutputHelper output) => _output = output;
//
//     /// <summary>
//     /// Creates the settings for one scenario. The defaults describe a common technical
//     /// baseline — sea level at 1 bar, salt water, 18 m/min descent, 9/9/6/3 m/min ascent
//     /// bands, SAC 18/14 L/min, pO2 limits 1.4/1.6 bar, 40 bar reserve at stress factor 2
//     /// for a two-diver team, one-minute stop rounding, two-minute gas switches, last stop
//     /// at six meters — and every test states only its deviations.
//     /// </summary>
//     private static DivePlanSettings CreateSettings(double surfacePressureMillibar = 1000,
//         Salinity salinity = Salinity.Salt,
//         double descentRateMetersPerMinute = 18,
//         double ascentRateBelow75PercentMetersPerMinute = 9,
//         double ascentRate75To50PercentMetersPerMinute = 9,
//         double ascentRate50PercentToStopsMetersPerMinute = 6,
//         double ascentRateLastSixMetersMetersPerMinute = 3,
//         double bottomSacLitersPerMinute = 18,
//         double decoSacLitersPerMinute = 14,
//         double bottomMetabolicOxygenConsumptionLitersPerMinute = 1,
//         double decoMetabolicOxygenConsumptionLitersPerMinute = 0.6,
//         double loopVolumeLiters = 6,
//         double bottomPo2Bar = 1.4,
//         double decoPo2Bar = 1.6,
//         double reservePressureBar = 40,
//         double reserveStressFactor = 2,
//         int reserveTeamSize = 2,
//         double stopTimeIncrementMinutes = 1,
//         double problemSolvingMinutes = 1,
//         double gasSwitchMinutes = 2,
//         double oxygenBreakIntervalMinutes = 20,
//         double oxygenBreakDurationMinutes = 5,
//         bool safetyStop = false,
//         bool lastStopAtSixMeters = true,
//         bool switchAtRequiredStop = false,
//         bool oxygenBreaks = false,
//         bool oxygenIsNarcotic = true) =>
//         new(
//             Pressure.FromMillibar(surfacePressureMillibar),
//             salinity,
//             descentRateMetersPerMinute,
//             ascentRateBelow75PercentMetersPerMinute,
//             ascentRate75To50PercentMetersPerMinute,
//             ascentRate50PercentToStopsMetersPerMinute,
//             ascentRateLastSixMetersMetersPerMinute,
//             bottomSacLitersPerMinute,
//             decoSacLitersPerMinute,
//             bottomMetabolicOxygenConsumptionLitersPerMinute,
//             decoMetabolicOxygenConsumptionLitersPerMinute,
//             loopVolumeLiters,
//             Pressure.FromBar(bottomPo2Bar),
//             Pressure.FromBar(decoPo2Bar),
//             Pressure.FromBar(reservePressureBar),
//             reserveStressFactor,
//             reserveTeamSize,
//             TimeSpan.FromMinutes(stopTimeIncrementMinutes),
//             TimeSpan.FromMinutes(problemSolvingMinutes),
//             TimeSpan.FromMinutes(gasSwitchMinutes),
//             TimeSpan.FromMinutes(oxygenBreakIntervalMinutes),
//             TimeSpan.FromMinutes(oxygenBreakDurationMinutes),
//             safetyStop,
//             lastStopAtSixMeters,
//             switchAtRequiredStop,
//             oxygenBreaks,
//             oxygenIsNarcotic);
//
//     /// <summary>Creates one cylinder from the mix in percent and the size and fill.</summary>
//     private static Cylinder CreateCylinder(double percentO2,
//         double percentHe,
//         double sizeLiter,
//         double startPressureBar,
//         CylinderPurpose purpose) =>
//         new(GasMixture.FromPercent(percentO2, percentHe), Volume.FromLiter(sizeLiter),
//             Pressure.FromBar(startPressureBar), purpose);
//
//     /// <summary>Plans the requested dive with a fresh algorithm instance.</summary>
//     private static DecoPlan CreatePlan(DivePlanRequest request,
//         double gradientFactorLow,
//         double gradientFactorHigh) =>
//         new DivePlanner(new BuhlmannZhl16cAlgorithm(gradientFactorLow, gradientFactorHigh))
//             .CreatePlan(request);
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
//             CreateCylinder(21, 0, 24, 232, CylinderPurpose.BottomGas),
//             CreateCylinder(50, 0, 11.1, 200, CylinderPurpose.DecoGas)
//         ];
//         var request = new DivePlanRequest(TestFactory.CreateProfile((42, 25), (27, 15)),
//             cylinders, CreateSettings());
//
//         // Act
//         var plan = CreatePlan(request, 0.3, 0.7);
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
//             CreateCylinder(21, 0, 24, 232, CylinderPurpose.BottomGas),
//             CreateCylinder(50, 0, 11.1, 200, CylinderPurpose.DecoGas),
//             CreateCylinder(100, 0, 7, 200, CylinderPurpose.DecoGas)
//         ];
//         var settings = CreateSettings(
//             ascentRateBelow75PercentMetersPerMinute: 10,
//             ascentRate75To50PercentMetersPerMinute: 10,
//             ascentRate50PercentToStopsMetersPerMinute: 10,
//             oxygenBreaks: true);
//         var request = new DivePlanRequest(TestFactory.CreateProfile((55, 20)), cylinders,
//             settings);
//
//         // Act
//         var plan = CreatePlan(request, 1, 1);
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
//             CreateCylinder(32, 0, 15, 232, CylinderPurpose.BottomGas),
//             CreateCylinder(50, 0, 11.1, 200, CylinderPurpose.DecoGas)
//         ];
//         var settings = CreateSettings(
//             descentRateMetersPerMinute: 12,
//             gasSwitchMinutes: 0,
//             safetyStop: true,
//             lastStopAtSixMeters: false);
//         var request = new DivePlanRequest(TestFactory.CreateProfile((33, 20), (21, 20)),
//             cylinders, settings);
//
//         // Act
//         var plan = CreatePlan(request, 0.45, 0.95);
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
//             CreateCylinder(21, 35, 24, 232, CylinderPurpose.BottomGas),
//             CreateCylinder(50, 0, 11.1, 200, CylinderPurpose.DecoGas)
//         ];
//         var settings = CreateSettings(
//             salinity: Salinity.Fresh,
//             descentRateMetersPerMinute: 15,
//             reserveStressFactor: 1.5,
//             reserveTeamSize: 1,
//             lastStopAtSixMeters: false);
//         var request = new DivePlanRequest(TestFactory.CreateProfile((45, 30)), cylinders,
//             settings);
//
//         // Act
//         var plan = CreatePlan(request, 0.35, 0.75);
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
//             CreateCylinder(18, 45, 24, 232, CylinderPurpose.BottomGas),
//             CreateCylinder(50, 0, 11.1, 200, CylinderPurpose.DecoGas),
//             CreateCylinder(100, 0, 7, 200, CylinderPurpose.DecoGas)
//         ];
//         var settings = CreateSettings(surfacePressureMillibar: 1013.25, gasSwitchMinutes: 3);
//         var request = new DivePlanRequest(TestFactory.CreateProfile((60, 25)), cylinders,
//             settings);
//
//         // Act
//         var plan = CreatePlan(request, 0.3, 0.85);
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
//             CreateCylinder(15, 55, 24, 232, CylinderPurpose.BottomGas),
//             CreateCylinder(35, 25, 11.1, 200, CylinderPurpose.DecoGas),
//             CreateCylinder(50, 0, 11.1, 200, CylinderPurpose.DecoGas),
//             CreateCylinder(100, 0, 7, 200, CylinderPurpose.DecoGas)
//         ];
//         var settings = CreateSettings(
//             descentRateMetersPerMinute: 20,
//             stopTimeIncrementMinutes: 0.5,
//             switchAtRequiredStop: true,
//             oxygenBreaks: true,
//             oxygenIsNarcotic: false);
//         var request = new DivePlanRequest(TestFactory.CreateProfile((75, 20)), cylinders,
//             settings);
//
//         // Act
//         var plan = CreatePlan(request, 0.3, 0.8);
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
//             CreateCylinder(10, 70, 24, 232, CylinderPurpose.BottomGas),
//             CreateCylinder(21, 35, 11.1, 207, CylinderPurpose.DecoGas),
//             CreateCylinder(50, 0, 22.2, 207, CylinderPurpose.DecoGas),
//             CreateCylinder(100, 0, 11.1, 200, CylinderPurpose.DecoGas)
//         ];
//         var settings = CreateSettings(
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
//         var plan = CreatePlan(request, 0.25, 0.8);
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
//             CreateCylinder(12, 60, 24, 232, CylinderPurpose.BottomGas),
//             CreateCylinder(21, 35, 11.1, 207, CylinderPurpose.DecoGas),
//             CreateCylinder(50, 0, 11.1, 207, CylinderPurpose.DecoGas),
//             CreateCylinder(80, 0, 11.1, 200, CylinderPurpose.DecoGas)
//         ];
//         var settings = CreateSettings(
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
//         var plan = CreatePlan(request, 0.2, 0.85);
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
//             CreateCylinder(18, 45, 24, 232, CylinderPurpose.BottomGas),
//             CreateCylinder(50, 0, 11.1, 200, CylinderPurpose.DecoGas),
//             CreateCylinder(100, 0, 7, 200, CylinderPurpose.DecoGas)
//         ];
//         var settings = CreateSettings(
//             surfacePressureMillibar: PhysicalConstants.AtmosphericPressureAtAltitudeMillibar(2200),
//             salinity: Salinity.Fresh,
//             descentRateMetersPerMinute: 15,
//             lastStopAtSixMeters: false);
//         var request = new DivePlanRequest(TestFactory.CreateProfile((52, 20)), cylinders,
//             settings);
//
//         // Act
//         var plan = CreatePlan(request, 0.3, 0.75);
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
//             CreateCylinder(21, 35, 24, 232, CylinderPurpose.BottomGas),
//             CreateCylinder(50, 0, 11.1, 200, CylinderPurpose.DecoGas)
//         ];
//         var priorDive = new PriorDive(TestFactory.CreateProfile((45, 25)), cylinders,
//             CreateSettings(), TimeSpan.FromMinutes(120), GasMixture.Air);
//         var request = new DivePlanRequest(TestFactory.CreateProfile((40, 20)), cylinders,
//             CreateSettings(), [priorDive]);
//
//         // Act
//         var plan = CreatePlan(request, 0.3, 0.85);
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
//             CreateCylinder(21, 0, 15, 232, CylinderPurpose.BottomGas),
//             CreateCylinder(50, 0, 11.1, 200, CylinderPurpose.DecoGas)
//         ];
//         var settings = CreateSettings(reserveTeamSize: 3, lastStopAtSixMeters: false);
//         var firstDive = new PriorDive(TestFactory.CreateProfile((40, 20)), cylinders,
//             settings, TimeSpan.FromMinutes(150), GasMixture.Air);
//         var secondDive = new PriorDive(TestFactory.CreateProfile((32, 25)), cylinders,
//             settings, TimeSpan.FromMinutes(90), GasMixture.Oxygen);
//         var request = new DivePlanRequest(TestFactory.CreateProfile((28, 30)), cylinders,
//             settings, [firstDive, secondDive]);
//
//         // Act
//         var plan = CreatePlan(request, 0.4, 0.85);
//
//         // Assert
//         _output.WriteLine(plan.ToString());
//         Assert.True(false);
//     }
//
//     /// <summary>
//     /// Closed-circuit rebreather to 80 m for 20 minutes on trimix 12/65 diluent held at a
//     /// 1.3 bar setpoint, with the whole decompression run on the loop. A 3 L oxygen supply
//     /// feeds the loop and open-circuit trimix 21/35, nitrox 50, and oxygen are carried for
//     /// bailout. Standard 1013.25 mbar surface pressure, GF 30/80.
//     /// </summary>
//     [Fact]
//     public void CreatePlan_ShouldMatchReferenceSchedule_WhenClosedCircuitAt80MetersOnDiluent1265AtSetpoint13()
//     {
//         // Arrange
//         Cylinder[] cylinders =
//         [
//             CreateCylinder(12, 65, 24, 200, CylinderPurpose.Diluent),
//             CreateCylinder(100, 0, 3, 200, CylinderPurpose.Oxygen),
//             CreateCylinder(21, 35, 11.1, 207, CylinderPurpose.Bailout),
//             CreateCylinder(50, 0, 11.1, 207, CylinderPurpose.Bailout)
//         ];
//         var loop = BreathingLoop.ClosedCircuit(Pressure.FromBar(1.3));
//         var settings = CreateSettings(surfacePressureMillibar: 1013.25);
//         var request = new DivePlanRequest(TestFactory.CreateProfile(loop, (80, 20)), cylinders,
//             settings);
//
//         // Act
//         var plan = CreatePlan(request, 0.3, 0.8);
//
//         // Assert
//         _output.WriteLine(plan.ToString());
//         Assert.True(false);
//     }
//
//     /// <summary>
//     /// Bailout from a closed-circuit rebreather at 75 m in EN 13319 reference water: 20
//     /// minutes on trimix 12/60 diluent at a 1.2 bar setpoint, then a two-minute loop
//     /// failure at depth that puts the diver on open circuit for the whole ascent, breathing
//     /// the trimix 15/55 and nitrox 50 bailout stages. The bailout stages are sized for the
//     /// full open-circuit ascent they must cover, not for the loop that failed, and are
//     /// assessed for self-rescue rather than for a shared team ascent, as bailout normally is.
//     /// Gas switches only at required stops, last stop at three meters, GF 20/80.
//     /// </summary>
//     [Fact]
//     public void CreatePlan_ShouldMatchReferenceSchedule_WhenBailingOutToOpenCircuitAt75MetersInEn13319Water()
//     {
//         // Arrange
//         Cylinder[] cylinders =
//         [
//             CreateCylinder(12, 60, 24, 200, CylinderPurpose.Diluent),
//             CreateCylinder(100, 0, 3, 200, CylinderPurpose.Oxygen),
//             CreateCylinder(15, 55, 24, 232, CylinderPurpose.Bailout),
//             CreateCylinder(50, 0, 22.2, 207, CylinderPurpose.Bailout)
//         ];
//         var loop = BreathingLoop.ClosedCircuit(Pressure.FromBar(1.2));
//         var settings = CreateSettings(
//             salinity: Salinity.EN13319,
//             reserveTeamSize: 1,
//             switchAtRequiredStop: true,
//             lastStopAtSixMeters: false);
//         var profile = TestFactory.CreateProfile(
//             (75, 20, loop),
//             (75, 2, BreathingLoop.OpenCircuit));
//         var request = new DivePlanRequest(profile, cylinders, settings);
//
//         // Act
//         var plan = CreatePlan(request, 0.2, 0.8);
//
//         // Assert
//         _output.WriteLine(plan.ToString());
//         Assert.True(false);
//     }
//
//     /// <summary>
//     /// Passive semi-closed rebreather to 60 m for 30 minutes, venting one part in ten of
//     /// every breath. Trimix 16/50 is the deep supply and nitrox 50 the shallow one, so the
//     /// planner feeds the loop from the richer supply once it is within the decompression
//     /// oxygen limit, as a diver would to keep the loop out of hypoxia on the stops. Oxygen
//     /// is carried for bailout only. The loop's oxygen shortfall is fixed at the bottom
//     /// breathing rate for the whole dive, so configure the reference implementation the same
//     /// way. Deco SAC 13 L/min, last stop at three meters, GF 35/75.
//     /// </summary>
//     [Fact]
//     public void CreatePlan_ShouldMatchReferenceSchedule_WhenSemiClosedAt60MetersOnTrimix1650SwitchingToNitrox50()
//     {
//         // Arrange
//         Cylinder[] cylinders =
//         [
//             CreateCylinder(16, 50, 24, 232, CylinderPurpose.Diluent),
//             CreateCylinder(50, 0, 11.1, 200, CylinderPurpose.Diluent),
//             CreateCylinder(100, 0, 7, 200, CylinderPurpose.Bailout)
//         ];
//         var settings = CreateSettings(decoSacLitersPerMinute: 13, lastStopAtSixMeters: false);
//         var loop = BreathingLoop.SemiClosed(0.1,
//             BreathingLoop.SemiClosedOxygenDropCoefficient(0.1,
//                 settings.BottomMetabolicOxygenConsumptionLitersPerMinute,
//                 settings.BottomSacLitersPerMinute,
//                 settings.SurfacePressure));
//         var request = new DivePlanRequest(TestFactory.CreateProfile(loop, (60, 30)), cylinders,
//             settings);
//
//         // Act
//         var plan = CreatePlan(request, 0.35, 0.75);
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
//             CreateCylinder(8, 80, 32, 232, CylinderPurpose.BottomGas),
//             CreateCylinder(18, 45, 11.1, 207, CylinderPurpose.DecoGas),
//             CreateCylinder(35, 25, 11.1, 207, CylinderPurpose.DecoGas),
//             CreateCylinder(50, 0, 22.2, 207, CylinderPurpose.DecoGas),
//             CreateCylinder(100, 0, 22.2, 200, CylinderPurpose.DecoGas)
//         ];
//         var settings = CreateSettings(
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
//         var plan = CreatePlan(request, 0.2, 0.75);
//
//         // Assert
//         _output.WriteLine(plan.ToString());
//         Assert.True(false);
//     }
// }
