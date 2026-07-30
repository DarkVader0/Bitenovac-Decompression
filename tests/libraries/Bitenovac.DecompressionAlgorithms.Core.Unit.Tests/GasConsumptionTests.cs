using Bitenovac.DecompressionAlgorithms.Core.Calculations;
using Bitenovac.DecompressionAlgorithms.Core.Environment;
using Bitenovac.DecompressionAlgorithms.Core.Equipment;
using Bitenovac.DecompressionAlgorithms.Core.Planning;
using Bitenovac.DecompressionAlgorithms.Units;

namespace Bitenovac.DecompressionAlgorithms.Core.Unit.Tests;

public sealed class GasConsumptionTests
{
    private const int Precision = 5;

    [Fact]
    public void Calculate_ShouldThrowArgumentNullException_WhenSegmentsIsNull()
    {
        // Arrange
        IReadOnlyList<DiveSegment> segments = null!;
        var cylinders = new[] { TestFactory.CreateCylinder() };

        // Act
        Action act = () => GasConsumption.Calculate(segments, cylinders, TestFactory.CreateSettings());

        // Assert
        Assert.Throws<ArgumentNullException>(act);
    }

    [Fact]
    public void Calculate_ShouldThrowArgumentNullException_WhenCylindersIsNull()
    {
        // Arrange
        var segments = new[] { TestFactory.CreateSegment(10, 10) };
        IReadOnlyList<Cylinder> cylinders = null!;

        // Act
        Action act = () => GasConsumption.Calculate(segments, cylinders, TestFactory.CreateSettings());

        // Assert
        Assert.Throws<ArgumentNullException>(act);
    }

    [Fact]
    public void Calculate_ShouldThrowArgumentNullException_WhenSettingsIsNull()
    {
        // Arrange
        var segments = new[] { TestFactory.CreateSegment(10, 10) };
        var cylinders = new[] { TestFactory.CreateCylinder() };

        // Act
        Action act = () => GasConsumption.Calculate(segments, cylinders, null!);

        // Assert
        Assert.Throws<ArgumentNullException>(act);
    }

    [Fact]
    public void Calculate_ShouldThrowArgumentException_WhenCylindersIsEmpty()
    {
        // Arrange
        var segments = new[] { TestFactory.CreateSegment(10, 10) };
        var cylinders = Array.Empty<Cylinder>();

        // Act
        Action act = () => GasConsumption.Calculate(segments, cylinders, TestFactory.CreateSettings());

        // Assert
        Assert.Throws<ArgumentException>(act);
    }

    [Fact]
    public void Calculate_ShouldThrowInvalidOperationException_WhenASegmentGasMatchesNoCylinder()
    {
        // Arrange
        var segments = new[] { TestFactory.CreateSegment(10, 10, GasMixture.FromPercent(50, 0)) };
        var cylinders = new[] { TestFactory.CreateCylinder(GasMixture.Air) };

        // Act
        Action act = () => GasConsumption.Calculate(segments, cylinders, TestFactory.CreateSettings());

        // Assert
        Assert.Throws<InvalidOperationException>(act);
    }

    [Fact]
    public void Calculate_ShouldThrowInvalidOperationException_WhenTheDemandExceedsWhatTheCylinderHolds()
    {
        // Arrange
        var segments = new[] { TestFactory.CreateSegment(0, 1) };
        var cylinders = new[] { TestFactory.CreateCylinder(sizeLiter: 1, startPressureBar: 10) };

        // Act
        Action act = () => GasConsumption.Calculate(segments, cylinders, TestFactory.CreateSettings());

        // Assert
        Assert.Throws<InvalidOperationException>(act);
    }

    [Fact]
    public void Calculate_ShouldReturnOneUsagePerCylinder_InTheOrderTheCylindersWereSupplied()
    {
        // Arrange
        var segments = new[] { TestFactory.CreateSegment(0, 1) };
        var cylinders = new[]
        {
            TestFactory.CreateCylinder(GasMixture.Air, startPressureBar: 200),
            TestFactory.CreateCylinder(GasMixture.FromPercent(50, 0), startPressureBar: 100)
        };

        // Act
        var usage = GasConsumption.Calculate(segments, cylinders, TestFactory.CreateSettings());

        // Assert
        Assert.Equal(2, usage.Count);
        Assert.Equal(200, usage[0].StartPressure.InBar, Precision);
        Assert.Equal(100, usage[1].StartPressure.InBar, Precision);
    }

    [Fact]
    public void Calculate_ShouldConsumeTheSurfaceRate_WhenTheSegmentIsHeldAtTheSurface()
    {
        // Arrange
        var segments = new[] { TestFactory.CreateSegment(0, 12) };
        var cylinders = new[] { TestFactory.CreateCylinder(sizeLiter: 12, startPressureBar: 200) };

        // Act
        var usage = GasConsumption.Calculate(segments, cylinders, TestFactory.CreateSettings());

        // Assert
        Assert.Equal(240, usage[0].GasUsed.InLiter, Precision);
    }

    [Fact]
    public void Calculate_ShouldReduceThePressureInProportionToTheGasConsumed_WhenGasIsBreathed()
    {
        // Arrange
        var segments = new[] { TestFactory.CreateSegment(0, 12) };
        var cylinders = new[] { TestFactory.CreateCylinder(sizeLiter: 12, startPressureBar: 200) };

        // Act
        var usage = GasConsumption.Calculate(segments, cylinders, TestFactory.CreateSettings());

        // Assert
        Assert.Equal(180, usage[0].EndPressure.InBar, Precision);
    }

    [Fact]
    public void Calculate_ShouldScaleTheConsumptionByTheAmbientPressure_WhenTheSegmentIsAtDepth()
    {
        // Arrange
        var segments = new[] { TestFactory.CreateSegment(10, 10) };
        var cylinders = new[] { TestFactory.CreateCylinder(sizeLiter: 12, startPressureBar: 200) };

        // Act
        var usage = GasConsumption.Calculate(segments, cylinders, TestFactory.CreateSettings());

        // Assert
        Assert.Equal(396.133, usage[0].GasUsed.InLiter, Precision);
        Assert.Equal(166.98892, usage[0].EndPressure.InBar, Precision);
    }

    [Fact]
    public void Calculate_ShouldUseTheDecompressionRate_WhenTheSegmentIsADecompressionStop()
    {
        // Arrange
        var segments = new[] { TestFactory.CreateSegment(0, 10, kind: SegmentKind.Stop) };
        var cylinders = new[] { TestFactory.CreateCylinder(sizeLiter: 12, startPressureBar: 200) };

        // Act
        var usage = GasConsumption.Calculate(segments, cylinders,
            TestFactory.CreateSettings(bottomSacLitersPerMinute: 20, decoSacLitersPerMinute: 15));

        // Assert
        Assert.Equal(150, usage[0].GasUsed.InLiter, Precision);
    }

    [Theory]
    [InlineData(SegmentKind.Descent)]
    [InlineData(SegmentKind.Bottom)]
    [InlineData(SegmentKind.Ascent)]
    [InlineData(SegmentKind.GasSwitch)]
    public void Calculate_ShouldUseTheBottomRate_WhenTheSegmentIsNotADecompressionStop(SegmentKind kind)
    {
        // Arrange
        var segments = new[] { TestFactory.CreateSegment(0, 10, kind: kind) };
        var cylinders = new[] { TestFactory.CreateCylinder(sizeLiter: 12, startPressureBar: 200) };

        // Act
        var usage = GasConsumption.Calculate(segments, cylinders,
            TestFactory.CreateSettings(bottomSacLitersPerMinute: 20, decoSacLitersPerMinute: 15));

        // Assert
        Assert.Equal(200, usage[0].GasUsed.InLiter, Precision);
    }

    [Fact]
    public void Calculate_ShouldDrawFromTheMatchingCylinderOnly_WhenSeveralGasesAreCarried()
    {
        // Arrange
        var decoGas = GasMixture.FromPercent(50, 0);
        var segments = new[]
        {
            TestFactory.CreateSegment(0, 12),
            TestFactory.CreateSegment(0, 6, decoGas, SegmentKind.Stop)
        };
        var cylinders = new[]
        {
            TestFactory.CreateCylinder(GasMixture.Air),
            TestFactory.CreateCylinder(decoGas, 11, 200, CylinderPurpose.DecoGas)
        };

        // Act
        var usage = GasConsumption.Calculate(segments, cylinders, TestFactory.CreateSettings());

        // Assert
        Assert.Equal(240, usage[0].GasUsed.InLiter, Precision);
        Assert.Equal(90, usage[1].GasUsed.InLiter, Precision);
    }

    [Fact]
    public void Calculate_ShouldLeaveTheCylinderUntouched_WhenItsGasIsNeverBreathed()
    {
        // Arrange
        var segments = new[] { TestFactory.CreateSegment(0, 12) };
        var cylinders = new[]
        {
            TestFactory.CreateCylinder(GasMixture.Air),
            TestFactory.CreateCylinder(GasMixture.FromPercent(50, 0), 11, 200, CylinderPurpose.DecoGas)
        };

        // Act
        var usage = GasConsumption.Calculate(segments, cylinders, TestFactory.CreateSettings());

        // Assert
        Assert.Equal(0, usage[1].GasUsed.InLiter, Precision);
        Assert.Equal(200, usage[1].EndPressure.InBar, Precision);
    }

    [Fact]
    public void Calculate_ShouldReturnZeroUsage_WhenThereAreNoSegments()
    {
        // Arrange
        var segments = Array.Empty<DiveSegment>();
        var cylinders = new[] { TestFactory.CreateCylinder(sizeLiter: 12, startPressureBar: 200) };

        // Act
        var usage = GasConsumption.Calculate(segments, cylinders, TestFactory.CreateSettings());

        // Assert
        Assert.Equal(0, usage[0].GasUsed.InLiter, Precision);
        Assert.Equal(200, usage[0].EndPressure.InBar, Precision);
    }

    [Fact]
    public void Calculate_ShouldReportZeroEndPressure_WhenTheCylinderHoldsNoGasToBeginWith()
    {
        // Arrange
        var segments = new[] { TestFactory.CreateSegment(0, 12) };
        var cylinders = new[]
        {
            TestFactory.CreateCylinder(GasMixture.Air),
            TestFactory.CreateCylinder(GasMixture.FromPercent(50, 0), 11, 0, CylinderPurpose.DecoGas)
        };

        // Act
        var usage = GasConsumption.Calculate(segments, cylinders, TestFactory.CreateSettings());

        // Assert
        Assert.Equal(0, usage[1].GasUsed.InLiter, Precision);
        Assert.Equal(0, usage[1].EndPressure.InBar, Precision);
        Assert.True(usage[1].IsExhausted);
    }

    [Fact]
    public void Calculate_ShouldReportTheCylinderAsExhausted_WhenTheWholeSupplyIsBreathed()
    {
        // Arrange
        var segments = new[] { TestFactory.CreateSegment(0, 0.5) };
        var cylinders = new[] { TestFactory.CreateCylinder(sizeLiter: 1, startPressureBar: 10) };

        // Act
        var usage = GasConsumption.Calculate(segments, cylinders, TestFactory.CreateSettings());

        // Assert
        Assert.True(usage[0].IsExhausted);
    }

    [Fact]
    public void Calculate_ShouldConsumeMoreGas_WhenTheWaterIsSaltRatherThanFresh()
    {
        // Arrange
        var segments = new[] { TestFactory.CreateSegment(30, 10) };
        var cylinders = new[] { TestFactory.CreateCylinder(sizeLiter: 12, startPressureBar: 200) };
        var inFresh = GasConsumption.Calculate(segments, cylinders,
            TestFactory.CreateSettings(salinity: Salinity.Fresh));

        // Act
        var inSalt = GasConsumption.Calculate(segments, cylinders,
            TestFactory.CreateSettings(salinity: Salinity.Salt));

        // Assert
        Assert.True(inSalt[0].GasUsed.InLiter > inFresh[0].GasUsed.InLiter);
    }

    [Fact]
    public void Calculate_ShouldDrawTheMetabolicRateOfEachPhase_WhenTheSegmentsAreClosedCircuit()
    {
        // Arrange
        // A closed-circuit loop vents nothing, so the oxygen supply gives up only what the
        // diver metabolises: ten minutes of work at 1 L/min and twenty minutes of resting at
        // a stop at 0.6 L/min.
        var loop = BreathingLoop.ClosedCircuit(Pressure.FromBar(1.3));
        var segments = new[]
        {
            new DiveSegment(Depth.FromMeter(30), TimeSpan.FromMinutes(10), GasMixture.Air, SegmentKind.Bottom, loop),
            new DiveSegment(Depth.FromMeter(6), TimeSpan.FromMinutes(20), GasMixture.Air, SegmentKind.Stop, loop)
        };
        var cylinders = new[]
        {
            TestFactory.CreateCylinder(GasMixture.Air, 12, 200, CylinderPurpose.Diluent),
            TestFactory.CreateCylinder(GasMixture.Oxygen, 3, 200, CylinderPurpose.Oxygen)
        };

        // Act
        var usage = GasConsumption.Calculate(segments, cylinders,
            TestFactory.CreateSettings(bottomMetabolicOxygenConsumptionLitersPerMinute: 1,
                decoMetabolicOxygenConsumptionLitersPerMinute: 0.6));

        // Assert
        Assert.Equal(22, usage[1].GasUsed.InLiter, Precision);
    }

    [Fact]
    public void Calculate_ShouldDrawDiluentOnlyToRefillTheLoop_WhenTheSegmentsAreClosedCircuit()
    {
        // Arrange
        // The six liter loop is compressed on the way down and must be topped up from the
        // diluent by 6 x (3.941995 - 1) L; the ascent vents the excess and draws nothing.
        var loop = BreathingLoop.ClosedCircuit(Pressure.FromBar(1.3));
        var segments = new[]
        {
            new DiveSegment(Depth.FromMeter(30), TimeSpan.FromMinutes(10), GasMixture.Air, SegmentKind.Bottom, loop),
            new DiveSegment(Depth.FromMeter(6), TimeSpan.FromMinutes(20), GasMixture.Air, SegmentKind.Stop, loop)
        };
        var cylinders = new[]
        {
            TestFactory.CreateCylinder(GasMixture.Air, 12, 200, CylinderPurpose.Diluent),
            TestFactory.CreateCylinder(GasMixture.Oxygen, 3, 200, CylinderPurpose.Oxygen)
        };

        // Act
        var usage = GasConsumption.Calculate(segments, cylinders, TestFactory.CreateSettings(loopVolumeLiters: 6));

        // Assert
        Assert.Equal(17.65197, usage[0].GasUsed.InLiter, Precision);
    }

    [Fact]
    public void Calculate_ShouldNotRequireAnOxygenSupply_WhenAClosedCircuitSegmentHasNoMetabolicDemand()
    {
        // Arrange
        // With no metabolic demand the loop draws nothing from an oxygen supply, so none
        // needs to be carried and only the descent make-up of 6 x (3.941995 - 1) L is drawn
        // from the diluent.
        var loop = BreathingLoop.ClosedCircuit(Pressure.FromBar(1.3));
        var segments = new[]
        {
            new DiveSegment(Depth.FromMeter(30), TimeSpan.FromMinutes(10), GasMixture.Air, SegmentKind.Bottom, loop)
        };
        var cylinders = new[] { TestFactory.CreateCylinder(GasMixture.Air, 12, 200, CylinderPurpose.Diluent) };

        // Act
        var usage = GasConsumption.Calculate(segments, cylinders,
            TestFactory.CreateSettings(bottomMetabolicOxygenConsumptionLitersPerMinute: 0,
                decoMetabolicOxygenConsumptionLitersPerMinute: 0,
                loopVolumeLiters: 6));

        // Assert
        Assert.Equal(17.65197, usage[0].GasUsed.InLiter, Precision);
    }

    [Fact]
    public void Calculate_ShouldThrowInvalidOperationException_WhenAClosedCircuitSegmentHasNoOxygenSupply()
    {
        // Arrange
        var loop = BreathingLoop.ClosedCircuit(Pressure.FromBar(1.3));
        var segments = new[]
        {
            new DiveSegment(Depth.FromMeter(30), TimeSpan.FromMinutes(10), GasMixture.Air, SegmentKind.Bottom, loop)
        };
        var cylinders = new[] { TestFactory.CreateCylinder(GasMixture.Air, 12, 200, CylinderPurpose.Diluent) };

        // Act
        Action act = () => GasConsumption.Calculate(segments, cylinders, TestFactory.CreateSettings());

        // Assert
        Assert.Throws<InvalidOperationException>(act);
    }

    [Fact]
    public void Calculate_ShouldDrawTheVentedShareOfEachBreath_WhenTheSegmentsAreSemiClosed()
    {
        // Arrange
        // A loop venting one part in ten draws a tenth of the open-circuit demand from its
        // supply, being 0.1 x 20 x 3.941995 x 10 L, plus 6 x (3.941995 - 1) L to fill it on
        // the way down.
        var loop = BreathingLoop.SemiClosed(0.1, Pressure.FromMillibar(500));
        var segments = new[]
        {
            new DiveSegment(Depth.FromMeter(30), TimeSpan.FromMinutes(10), GasMixture.Air, SegmentKind.Bottom, loop)
        };
        var cylinders = new[] { TestFactory.CreateCylinder(GasMixture.Air, 12, 200, CylinderPurpose.Diluent) };

        // Act
        var usage = GasConsumption.Calculate(segments, cylinders, TestFactory.CreateSettings(loopVolumeLiters: 6));

        // Assert
        Assert.Equal(96.49187, usage[0].GasUsed.InLiter, Precision);
    }
}