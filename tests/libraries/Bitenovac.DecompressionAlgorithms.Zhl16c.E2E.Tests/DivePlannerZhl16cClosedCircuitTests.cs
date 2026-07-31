// using Bitenovac.DecompressionAlgorithms.Core.Environment;
// using Bitenovac.DecompressionAlgorithms.Core.Equipment;
// using Bitenovac.DecompressionAlgorithms.Core.Planning;
// using Bitenovac.DecompressionAlgorithms.Units;
//
// namespace Bitenovac.DecompressionAlgorithms.Zhl16c.E2E.Tests;
//
// /// <summary>
// /// Validates complete closed-circuit rebreather plans against schedules produced by other
// /// established implementations of the ZH-L16C model. Each test fixes every input of one
// /// realistic technical dive and writes the full expanded schedule, gas usage, and oxygen
// /// exposure to the test output so the numbers can be compared against the reference
// /// implementation. The assertions are placeholders until the reference values are
// /// transcribed.
// /// <para>
// /// Conventions shared by all scenarios: level durations are the time spent at the level;
// /// the descent and any inter-level travel are generated in addition. The dives are planned
// /// on the loop itself, with the loop gas carried as a
// /// <see cref="CylinderPurpose.Diluent" /> cylinder, the oxygen supply as
// /// <see cref="CylinderPurpose.Oxygen" />, and the open-circuit stages as
// /// <see cref="CylinderPurpose.Bailout" />; configure the reference implementation with the
// /// same setpoint.
// /// </para>
// /// <para>
// /// The mode-comparison scenarios plan the same four dives that
// /// <see cref="DivePlannerZhl16cOpenCircuitTests" /> and
// /// <see cref="DivePlannerZhl16cSemiClosedTests" /> plan on the other apparatus, so that the
// /// only difference between the three schedules is the breathing apparatus. The loop is fed
// /// from the bottom mix alone as its diluent, holds 1.2 bar on the bottom and 1.6 bar for
// /// the ascent, draws its oxygen from a 3 L supply, and carries the rest of the ladder as
// /// bailout it never breathes. Oxygen breaks are disabled throughout.
// /// </para>
// /// </summary>
// public sealed class DivePlannerZhl16cClosedCircuitTests
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
//     public DivePlannerZhl16cClosedCircuitTests(ITestOutputHelper output) => _output = output;
//
//     /// <summary>
//     /// The settings shared by every mode-comparison scenario: salt water at standard
//     /// sea-level pressure and otherwise the shared baseline.
//     /// </summary>
//     private static DivePlanSettings CreateComparisonSettings() =>
//         TestFactory.CreateSettings(1013.25);
//
//     /// <summary>
//     /// Creates the closed-circuit set: the bottom mix as the sole diluent, a small oxygen
//     /// supply for the loop, and the rest of the ladder as bailout.
//     /// </summary>
//     private static Cylinder[] ClosedCircuitCylinders(GasMixture diluent, params GasMixture[] ladder)
//     {
//         var cylinders = new Cylinder[ladder.Length + 2];
//         cylinders[0] = new Cylinder(diluent, Volume.FromLiter(24), Pressure.FromBar(232),
//             CylinderPurpose.Diluent);
//         cylinders[1] = new Cylinder(GasMixture.Oxygen, Volume.FromLiter(3), Pressure.FromBar(200),
//             CylinderPurpose.Oxygen);
//         for (var i = 0; i < ladder.Length; i++)
//         {
//             cylinders[i + 2] = new Cylinder(ladder[i], Volume.FromLiter(22.2), Pressure.FromBar(207),
//                 CylinderPurpose.Bailout);
//         }
//
//         return cylinders;
//     }
//
//     /// <summary>The loop held at 1.2 bar on the bottom and raised to 1.6 bar for the ascent.</summary>
//     private static BreathingLoop ClosedCircuitLoop() =>
//         BreathingLoop.ClosedCircuit(Pressure.FromBar(1.2), Pressure.FromBar(1.6));
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
//             TestFactory.CreateCylinder(12, 65, 24, 200, CylinderPurpose.Diluent),
//             TestFactory.CreateCylinder(100, 0, 3, 200, CylinderPurpose.Oxygen),
//             TestFactory.CreateCylinder(21, 35, 11.1, 207, CylinderPurpose.Bailout),
//             TestFactory.CreateCylinder(50, 0, 11.1, 207, CylinderPurpose.Bailout)
//         ];
//         var loop = BreathingLoop.ClosedCircuit(Pressure.FromBar(1.3));
//         var settings = TestFactory.CreateSettings(1013.25);
//         var request = new DivePlanRequest(TestFactory.CreateProfile(loop, (80, 20)), cylinders,
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
//             TestFactory.CreateCylinder(12, 60, 24, 200, CylinderPurpose.Diluent),
//             TestFactory.CreateCylinder(100, 0, 3, 200, CylinderPurpose.Oxygen),
//             TestFactory.CreateCylinder(15, 55, 24, 232, CylinderPurpose.Bailout),
//             TestFactory.CreateCylinder(50, 0, 22.2, 207, CylinderPurpose.Bailout)
//         ];
//         var loop = BreathingLoop.ClosedCircuit(Pressure.FromBar(1.2));
//         var settings = TestFactory.CreateSettings(
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
//         var plan = TestFactory.CreatePlan(request, 0.2, 0.8);
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
//     public void CreatePlan_ShouldMatchReferenceSchedule_WhenClosedCircuitAt45MetersOnTrimix2135()
//     {
//         // Arrange
//         var settings = CreateComparisonSettings();
//         var request = new DivePlanRequest(TestFactory.CreateProfile(ClosedCircuitLoop(), (45, 25)),
//             ClosedCircuitCylinders(Trimix2135, Nitrox50), settings);
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
//     public void CreatePlan_ShouldMatchReferenceSchedule_WhenClosedCircuitAt60MetersOnTrimix1845()
//     {
//         // Arrange
//         var settings = CreateComparisonSettings();
//         var request = new DivePlanRequest(TestFactory.CreateProfile(ClosedCircuitLoop(), (60, 20)),
//             ClosedCircuitCylinders(Trimix1845, Trimix3525, Nitrox50), settings);
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
//     public void CreatePlan_ShouldMatchReferenceSchedule_WhenClosedCircuitAt75MetersOnTrimix1555()
//     {
//         // Arrange
//         var settings = CreateComparisonSettings();
//         var request = new DivePlanRequest(TestFactory.CreateProfile(ClosedCircuitLoop(), (75, 20)),
//             ClosedCircuitCylinders(Trimix1555, Trimix3525, Nitrox50), settings);
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
//     public void CreatePlan_ShouldMatchReferenceSchedule_WhenClosedCircuitAt100MetersOnTrimix1070()
//     {
//         // Arrange
//         var settings = CreateComparisonSettings();
//         var request = new DivePlanRequest(TestFactory.CreateProfile(ClosedCircuitLoop(), (100, 15)),
//             ClosedCircuitCylinders(Trimix1070, Trimix2135, Trimix3525, Nitrox50), settings);
//
//         // Act
//         var plan = TestFactory.CreatePlan(request, 0.2, 0.8);
//
//         // Assert
//         _output.WriteLine(plan.ToString());
//         Assert.True(false);
//     }
// }