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
// /// Validates the same four dives planned three ways — open circuit, closed circuit, and
// /// passive semi-closed — against schedules produced by other established implementations of
// /// the ZH-L16C model. Each group of three fixes the depth, the bottom time, the gas ladder,
// /// the environment, the gradient factors, and every consumption and ascent setting, so that
// /// the only difference between the three schedules is the breathing apparatus. Each test
// /// writes the full expanded schedule, gas usage, and oxygen exposure to the test output so
// /// the numbers can be compared against the reference implementation. The assertions are
// /// placeholders until the reference values are transcribed.
// /// <para>
// /// The gas ladder is identical across the three modes; only the role each cylinder plays
// /// changes. Open circuit carries the bottom mix and breathes the ladder down as
// /// decompression gas. The closed-circuit loop is fed from the bottom mix alone as its
// /// diluent, holds 1.2 bar on the bottom and 1.6 bar for the ascent, draws its oxygen from a
// /// 3 L supply, and carries the rest of the ladder as bailout it never breathes. The
// /// semi-closed loop vents one part in ten of every breath and is fed from the ladder down to
// /// nitrox 50, switching supply as each becomes breathable within the decompression oxygen
// /// limit, with oxygen carried for bailout only.
// /// </para>
// /// <para>
// /// Oxygen breaks are disabled throughout: they would fire only on the open-circuit plans,
// /// where the diver reaches pure oxygen, and would make the three schedules harder to compare.
// /// Enable them on the reference implementation only if you enable them here.
// /// </para>
// /// </summary>
// public sealed class DivePlannerZhl16cModeComparisonTests
// {
//     private const double DumpRatio = 0.1;
//
//     private static readonly GasMixture Trimix2135 = GasMixture.FromPercent(21, 35);
//     private static readonly GasMixture Trimix1845 = GasMixture.FromPercent(18, 45);
//     private static readonly GasMixture Trimix1555 = GasMixture.FromPercent(15, 55);
//     private static readonly GasMixture Trimix1070 = GasMixture.FromPercent(10, 70);
//     private static readonly GasMixture Trimix3525 = GasMixture.FromPercent(35, 25);
//     private static readonly GasMixture Nitrox50 = GasMixture.FromPercent(50, 0);
//
//     private readonly ITestOutputHelper _output;
//
//     public DivePlannerZhl16cModeComparisonTests(ITestOutputHelper output) => _output = output;
//
//     /// <summary>
//     /// The settings shared by every mode: salt water at standard sea-level pressure, 18 m/min
//     /// descent, 9/9/6/3 m/min ascent bands, SAC 18/14 L/min, metabolic oxygen 1.0/0.6 L/min,
//     /// a 6 L breathing loop, pO2 limits 1.4/1.6 bar, 40 bar reserve at stress factor 2 for a
//     /// two-diver team, one-minute stop rounding, two-minute gas switches, last stop at six
//     /// meters, and oxygen treated as narcotic.
//     /// </summary>
//     private static DivePlanSettings CreateSettings() =>
//         new(
//             Pressure.FromMillibar(1013.25),
//             Salinity.Salt,
//             18,
//             9,
//             9,
//             6,
//             3,
//             18,
//             14,
//             1.0,
//             0.6,
//             6,
//             Pressure.FromBar(1.4),
//             Pressure.FromBar(1.6),
//             Pressure.FromBar(40),
//             2,
//             2,
//             TimeSpan.FromMinutes(1),
//             TimeSpan.FromMinutes(1),
//             TimeSpan.FromMinutes(2),
//             TimeSpan.FromMinutes(20),
//             TimeSpan.FromMinutes(5),
//             false,
//             true,
//             false,
//             false,
//             true);
//
//     /// <summary>Creates the open-circuit set: the bottom mix, then the ladder as decompression gas.</summary>
//     private static Cylinder[] OpenCircuitCylinders(GasMixture bottom, params GasMixture[] ladder)
//     {
//         var cylinders = new Cylinder[ladder.Length + 2];
//         cylinders[0] = BottomCylinder(bottom, CylinderPurpose.BottomGas);
//         for (var i = 0; i < ladder.Length; i++)
//         {
//             cylinders[i + 1] = StageCylinder(ladder[i], CylinderPurpose.DecoGas);
//         }
//
//         cylinders[^1] = StageCylinder(GasMixture.Oxygen, CylinderPurpose.DecoGas);
//         return cylinders;
//     }
//
//     /// <summary>
//     /// Creates the closed-circuit set: the bottom mix as the sole diluent, a small oxygen
//     /// supply for the loop, and the rest of the ladder as bailout.
//     /// </summary>
//     private static Cylinder[] ClosedCircuitCylinders(GasMixture diluent, params GasMixture[] ladder)
//     {
//         var cylinders = new Cylinder[ladder.Length + 2];
//         cylinders[0] = BottomCylinder(diluent, CylinderPurpose.Diluent);
//         cylinders[1] = new Cylinder(GasMixture.Oxygen, Volume.FromLiter(3), Pressure.FromBar(200),
//             CylinderPurpose.Oxygen);
//         for (var i = 0; i < ladder.Length; i++)
//         {
//             cylinders[i + 2] = StageCylinder(ladder[i], CylinderPurpose.Bailout);
//         }
//
//         return cylinders;
//     }
//
//     /// <summary>
//     /// Creates the semi-closed set: the bottom mix and the whole ladder as loop supplies, so
//     /// the loop is fed from the richest gas the depth allows, with oxygen carried for bailout.
//     /// </summary>
//     private static Cylinder[] SemiClosedCylinders(GasMixture bottom, params GasMixture[] ladder)
//     {
//         var cylinders = new Cylinder[ladder.Length + 2];
//         cylinders[0] = BottomCylinder(bottom, CylinderPurpose.Diluent);
//         for (var i = 0; i < ladder.Length; i++)
//         {
//             cylinders[i + 1] = StageCylinder(ladder[i], CylinderPurpose.Diluent);
//         }
//
//         cylinders[^1] = StageCylinder(GasMixture.Oxygen, CylinderPurpose.Bailout);
//         return cylinders;
//     }
//
//     private static Cylinder BottomCylinder(GasMixture gas, CylinderPurpose purpose) =>
//         new(gas, Volume.FromLiter(24), Pressure.FromBar(232), purpose);
//
//     private static Cylinder StageCylinder(GasMixture gas, CylinderPurpose purpose) =>
//         new(gas, Volume.FromLiter(22.2), Pressure.FromBar(207), purpose);
//
//     /// <summary>The loop held at 1.2 bar on the bottom and raised to 1.6 bar for the ascent.</summary>
//     private static BreathingLoop ClosedCircuitLoop() =>
//         BreathingLoop.ClosedCircuit(Pressure.FromBar(1.2), Pressure.FromBar(1.6));
//
//     /// <summary>
//     /// The semi-closed loop, venting one part in ten of every breath, with its oxygen
//     /// shortfall taken at the working breathing rate for the whole dive.
//     /// </summary>
//     private static BreathingLoop SemiClosedLoop(DivePlanSettings settings) =>
//         BreathingLoop.SemiClosed(DumpRatio,
//             BreathingLoop.SemiClosedOxygenDropCoefficient(DumpRatio,
//                 settings.BottomMetabolicOxygenConsumptionLitersPerMinute,
//                 settings.BottomSacLitersPerMinute,
//                 settings.SurfacePressure));
//
//     /// <summary>Plans the requested dive with a fresh algorithm instance.</summary>
//     private static DecoPlan CreatePlan(DivePlanRequest request,
//         double gradientFactorLow,
//         double gradientFactorHigh) =>
//         new DivePlanner(new BuhlmannZhl16cAlgorithm(gradientFactorLow, gradientFactorHigh))
//             .CreatePlan(request);
//
//     // ---------------------------------------------------------------------------------
//     // 45 m for 25 minutes on normoxic trimix 21/35, ladder nitrox 50 and oxygen, GF 30/85.
//     // ---------------------------------------------------------------------------------
//
//     [Fact]
//     public void CreatePlan_ShouldMatchReferenceSchedule_WhenOpenCircuitAt45MetersOnTrimix2135()
//     {
//         // Arrange
//         var settings = CreateSettings();
//         var request = new DivePlanRequest(TestFactory.CreateProfile((45, 25)),
//             OpenCircuitCylinders(Trimix2135, Nitrox50), settings);
//
//         // Act
//         var plan = CreatePlan(request, 0.3, 0.85);
//
//         // Assert
//         _output.WriteLine(plan.ToString());
//         Assert.True(false);
//     }
//
//     [Fact]
//     public void CreatePlan_ShouldMatchReferenceSchedule_WhenClosedCircuitAt45MetersOnTrimix2135()
//     {
//         // Arrange
//         var settings = CreateSettings();
//         var request = new DivePlanRequest(TestFactory.CreateProfile(ClosedCircuitLoop(), (45, 25)),
//             ClosedCircuitCylinders(Trimix2135, Nitrox50), settings);
//
//         // Act
//         var plan = CreatePlan(request, 0.3, 0.85);
//
//         // Assert
//         _output.WriteLine(plan.ToString());
//         Assert.True(false);
//     }
//
//     [Fact]
//     public void CreatePlan_ShouldMatchReferenceSchedule_WhenSemiClosedAt45MetersOnTrimix2135()
//     {
//         // Arrange
//         var settings = CreateSettings();
//         var request = new DivePlanRequest(TestFactory.CreateProfile(SemiClosedLoop(settings), (45, 25)),
//             SemiClosedCylinders(Trimix2135, Nitrox50), settings);
//
//         // Act
//         var plan = CreatePlan(request, 0.3, 0.85);
//
//         // Assert
//         _output.WriteLine(plan.ToString());
//         Assert.True(false);
//     }
//
//     // ---------------------------------------------------------------------------------
//     // 60 m for 20 minutes on trimix 18/45, ladder trimix 35/25, nitrox 50, and oxygen,
//     // GF 30/80.
//     // ---------------------------------------------------------------------------------
//
//     [Fact]
//     public void CreatePlan_ShouldMatchReferenceSchedule_WhenOpenCircuitAt60MetersOnTrimix1845()
//     {
//         // Arrange
//         var settings = CreateSettings();
//         var request = new DivePlanRequest(TestFactory.CreateProfile((60, 20)),
//             OpenCircuitCylinders(Trimix1845, Trimix3525, Nitrox50), settings);
//
//         // Act
//         var plan = CreatePlan(request, 0.3, 0.8);
//
//         // Assert
//         _output.WriteLine(plan.ToString());
//         Assert.True(false);
//     }
//
//     [Fact]
//     public void CreatePlan_ShouldMatchReferenceSchedule_WhenClosedCircuitAt60MetersOnTrimix1845()
//     {
//         // Arrange
//         var settings = CreateSettings();
//         var request = new DivePlanRequest(TestFactory.CreateProfile(ClosedCircuitLoop(), (60, 20)),
//             ClosedCircuitCylinders(Trimix1845, Trimix3525, Nitrox50), settings);
//
//         // Act
//         var plan = CreatePlan(request, 0.3, 0.8);
//
//         // Assert
//         _output.WriteLine(plan.ToString());
//         Assert.True(false);
//     }
//
//     [Fact]
//     public void CreatePlan_ShouldMatchReferenceSchedule_WhenSemiClosedAt60MetersOnTrimix1845()
//     {
//         // Arrange
//         var settings = CreateSettings();
//         var request = new DivePlanRequest(TestFactory.CreateProfile(SemiClosedLoop(settings), (60, 20)),
//             SemiClosedCylinders(Trimix1845, Trimix3525, Nitrox50), settings);
//
//         // Act
//         var plan = CreatePlan(request, 0.3, 0.8);
//
//         // Assert
//         _output.WriteLine(plan.ToString());
//         Assert.True(false);
//     }
//
//     // ---------------------------------------------------------------------------------
//     // 75 m for 20 minutes on trimix 15/55, ladder trimix 35/25, nitrox 50, and oxygen,
//     // GF 25/80.
//     // ---------------------------------------------------------------------------------
//
//     [Fact]
//     public void CreatePlan_ShouldMatchReferenceSchedule_WhenOpenCircuitAt75MetersOnTrimix1555()
//     {
//         // Arrange
//         var settings = CreateSettings();
//         var request = new DivePlanRequest(TestFactory.CreateProfile((75, 20)),
//             OpenCircuitCylinders(Trimix1555, Trimix3525, Nitrox50), settings);
//
//         // Act
//         var plan = CreatePlan(request, 0.25, 0.8);
//
//         // Assert
//         _output.WriteLine(plan.ToString());
//         Assert.True(false);
//     }
//
//     [Fact]
//     public void CreatePlan_ShouldMatchReferenceSchedule_WhenClosedCircuitAt75MetersOnTrimix1555()
//     {
//         // Arrange
//         var settings = CreateSettings();
//         var request = new DivePlanRequest(TestFactory.CreateProfile(ClosedCircuitLoop(), (75, 20)),
//             ClosedCircuitCylinders(Trimix1555, Trimix3525, Nitrox50), settings);
//
//         // Act
//         var plan = CreatePlan(request, 0.25, 0.8);
//
//         // Assert
//         _output.WriteLine(plan.ToString());
//         Assert.True(false);
//     }
//
//     [Fact]
//     public void CreatePlan_ShouldMatchReferenceSchedule_WhenSemiClosedAt75MetersOnTrimix1555()
//     {
//         // Arrange
//         var settings = CreateSettings();
//         var request = new DivePlanRequest(TestFactory.CreateProfile(SemiClosedLoop(settings), (75, 20)),
//             SemiClosedCylinders(Trimix1555, Trimix3525, Nitrox50), settings);
//
//         // Act
//         var plan = CreatePlan(request, 0.25, 0.8);
//
//         // Assert
//         _output.WriteLine(plan.ToString());
//         Assert.True(false);
//     }
//
//     // ---------------------------------------------------------------------------------
//     // 100 m for 15 minutes on hypoxic trimix 10/70, ladder trimix 21/35, trimix 35/25,
//     // nitrox 50, and oxygen, GF 20/80.
//     // ---------------------------------------------------------------------------------
//
//     [Fact]
//     public void CreatePlan_ShouldMatchReferenceSchedule_WhenOpenCircuitAt100MetersOnTrimix1070()
//     {
//         // Arrange
//         var settings = CreateSettings();
//         var request = new DivePlanRequest(TestFactory.CreateProfile((100, 15)),
//             OpenCircuitCylinders(Trimix1070, Trimix2135, Trimix3525, Nitrox50), settings);
//
//         // Act
//         var plan = CreatePlan(request, 0.2, 0.8);
//
//         // Assert
//         _output.WriteLine(plan.ToString());
//         Assert.True(false);
//     }
//
//     [Fact]
//     public void CreatePlan_ShouldMatchReferenceSchedule_WhenClosedCircuitAt100MetersOnTrimix1070()
//     {
//         // Arrange
//         var settings = CreateSettings();
//         var request = new DivePlanRequest(TestFactory.CreateProfile(ClosedCircuitLoop(), (100, 15)),
//             ClosedCircuitCylinders(Trimix1070, Trimix2135, Trimix3525, Nitrox50), settings);
//
//         // Act
//         var plan = CreatePlan(request, 0.2, 0.8);
//
//         // Assert
//         _output.WriteLine(plan.ToString());
//         Assert.True(false);
//     }
//
//     [Fact]
//     public void CreatePlan_ShouldMatchReferenceSchedule_WhenSemiClosedAt100MetersOnTrimix1070()
//     {
//         // Arrange
//         var settings = CreateSettings();
//         var request = new DivePlanRequest(TestFactory.CreateProfile(SemiClosedLoop(settings), (100, 15)),
//             SemiClosedCylinders(Trimix1070, Trimix2135, Trimix3525, Nitrox50), settings);
//
//         // Act
//         var plan = CreatePlan(request, 0.2, 0.8);
//
//         // Assert
//         _output.WriteLine(plan.ToString());
//         Assert.True(false);
//     }
// }
