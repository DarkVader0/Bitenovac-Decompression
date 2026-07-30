using Bitenovac.DecompressionAlgorithms.Core.Calculations;
using Bitenovac.DecompressionAlgorithms.Core.Equipment;
using Bitenovac.DecompressionAlgorithms.Core.Planning;

namespace Bitenovac.DecompressionAlgorithms.Core.Unit.Tests;

public sealed class ReserveGasTests
{
    private const int Precision = 4;

    private static readonly GasMixture DecoGas = GasMixture.FromPercent(50, 0);

    [Fact]
    public void Calculate_ShouldThrowArgumentNullException_WhenSegmentsIsNull()
    {
        // Arrange
        IReadOnlyList<DiveSegment> segments = null!;
        var cylinders = new[] { TestFactory.CreateCylinder() };

        // Act
        Action act = () => ReserveGas.Calculate(segments, cylinders, TestFactory.CreateSettings());

        // Assert
        Assert.Throws<ArgumentNullException>(act);
    }

    [Fact]
    public void Calculate_ShouldThrowArgumentNullException_WhenCylindersIsNull()
    {
        // Arrange
        var segments = new[] { TestFactory.CreateSegment(0, 1) };
        IReadOnlyList<Cylinder> cylinders = null!;

        // Act
        Action act = () => ReserveGas.Calculate(segments, cylinders, TestFactory.CreateSettings());

        // Assert
        Assert.Throws<ArgumentNullException>(act);
    }

    [Fact]
    public void Calculate_ShouldThrowArgumentNullException_WhenSettingsIsNull()
    {
        // Arrange
        var segments = new[] { TestFactory.CreateSegment(0, 1) };
        var cylinders = new[] { TestFactory.CreateCylinder() };

        // Act
        Action act = () => ReserveGas.Calculate(segments, cylinders, null!);

        // Assert
        Assert.Throws<ArgumentNullException>(act);
    }

    [Fact]
    public void Calculate_ShouldThrowArgumentException_WhenCylindersIsEmpty()
    {
        // Arrange
        var segments = new[] { TestFactory.CreateSegment(0, 1) };
        var cylinders = Array.Empty<Cylinder>();

        // Act
        Action act = () => ReserveGas.Calculate(segments, cylinders, TestFactory.CreateSettings());

        // Assert
        Assert.Throws<ArgumentException>(act);
    }

    [Fact]
    public void Calculate_ShouldReturnOneStatusPerCylinder_InTheOrderTheCylindersWereSupplied()
    {
        // Arrange
        var segments = new[] { TestFactory.CreateSegment(0, 1) };
        var cylinders = new[]
        {
            TestFactory.CreateCylinder(GasMixture.Air),
            TestFactory.CreateCylinder(DecoGas, 11, 200, CylinderPurpose.DecoGas)
        };

        // Act
        var result = ReserveGas.Calculate(segments, cylinders, TestFactory.CreateSettings());

        // Assert
        Assert.Equal(2, result.CylinderStatuses.Count);
        Assert.Equal(CylinderPurpose.BottomGas, result.CylinderStatuses[0].Cylinder.Purpose);
        Assert.Equal(CylinderPurpose.DecoGas, result.CylinderStatuses[1].Cylinder.Purpose);
    }

    [Fact]
    public void Calculate_ShouldRequireTheRetainedFractionOfCapacity_WhenTheCylinderIsDecompressionGas()
    {
        // Arrange
        var segments = new[] { TestFactory.CreateSegment(0, 1) };
        var cylinders = new[]
        {
            TestFactory.CreateCylinder(GasMixture.Air),
            TestFactory.CreateCylinder(DecoGas, 11, 200, CylinderPurpose.DecoGas)
        };

        // Act
        var result = ReserveGas.Calculate(segments, cylinders, TestFactory.CreateSettings());

        // Assert
        Assert.Equal(1320, result.CylinderStatuses[1].RequiredReserve.InLiter, Precision);
    }

    [Fact]
    public void Calculate_ShouldProjectTheWholeCapacityAsRemaining_WhenTheDecompressionGasIsNeverBreathed()
    {
        // Arrange
        var segments = new[] { TestFactory.CreateSegment(0, 1) };
        var cylinders = new[]
        {
            TestFactory.CreateCylinder(GasMixture.Air),
            TestFactory.CreateCylinder(DecoGas, 11, 200, CylinderPurpose.DecoGas)
        };

        // Act
        var result = ReserveGas.Calculate(segments, cylinders, TestFactory.CreateSettings());

        // Assert
        Assert.Equal(2200, result.CylinderStatuses[1].ProjectedRemaining.InLiter, Precision);
    }

    [Fact]
    public void Calculate_ShouldRequireTheTeamEmergencyAscent_WhenTheCylinderIsBottomGasBreathedOverADepthSpan()
    {
        // Arrange
        var segments = new[]
        {
            TestFactory.CreateSegment(30, 10),
            TestFactory.CreateSegment(0, 1, kind: SegmentKind.Ascent)
        };
        var cylinders = new[] { TestFactory.CreateCylinder(GasMixture.Air) };

        // Act
        var result = ReserveGas.Calculate(segments, cylinders, TestFactory.CreateSettings());

        // Assert
        Assert.Equal(1129.5591, result.CylinderStatuses[0].RequiredReserve.InLiter, Precision);
    }

    [Fact]
    public void Calculate_ShouldExcludeTheUnusableFirstStagePressure_WhenProjectingTheRemainingBottomGas()
    {
        // Arrange
        var segments = new[]
        {
            TestFactory.CreateSegment(30, 10),
            TestFactory.CreateSegment(0, 1, kind: SegmentKind.Ascent)
        };
        var cylinders = new[] { TestFactory.CreateCylinder(GasMixture.Air) };

        // Act
        var result = ReserveGas.Calculate(segments, cylinders, TestFactory.CreateSettings());

        // Assert
        Assert.Equal(1471.601, result.CylinderStatuses[0].ProjectedRemaining.InLiter, Precision);
    }

    [Fact]
    public void Calculate_ShouldUseTheLastSixMetersRateOnly_WhenTheAscentLiesWhollyWithinTheFinalBand()
    {
        // Arrange
        var segments = new[]
        {
            TestFactory.CreateSegment(4, 10),
            TestFactory.CreateSegment(0, 1, kind: SegmentKind.Ascent)
        };
        var cylinders = new[] { TestFactory.CreateCylinder(GasMixture.Air) };

        // Act
        var result = ReserveGas.Calculate(segments, cylinders, TestFactory.CreateSettings());

        // Assert
        Assert.Equal(287.07192, result.CylinderStatuses[0].RequiredReserve.InLiter, Precision);
    }

    [Fact]
    public void Calculate_ShouldUseTheToStopsRateOnly_WhenTheAscentEndsBelowTheFinalBand()
    {
        // Arrange
        var segments = new[]
        {
            TestFactory.CreateSegment(30, 10),
            TestFactory.CreateSegment(10, 5, kind: SegmentKind.Ascent)
        };
        var cylinders = new[] { TestFactory.CreateCylinder(GasMixture.Air) };

        // Act
        var result = ReserveGas.Calculate(segments, cylinders, TestFactory.CreateSettings());

        // Assert
        Assert.Equal(592.266, result.CylinderStatuses[0].RequiredReserve.InLiter, Precision);
    }

    [Fact]
    public void Calculate_ShouldRequireNoReserve_WhenTheBottomGasIsBreathedAtASingleDepth()
    {
        // Arrange
        var segments = new[] { TestFactory.CreateSegment(30, 10) };
        var cylinders = new[] { TestFactory.CreateCylinder(GasMixture.Air) };

        // Act
        var result = ReserveGas.Calculate(segments, cylinders, TestFactory.CreateSettings());

        // Assert
        Assert.Equal(0, result.CylinderStatuses[0].RequiredReserve.InLiter, Precision);
    }

    [Fact]
    public void Calculate_ShouldRequireNoReserve_WhenTheBottomGasIsNeverBreathed()
    {
        // Arrange
        var segments = new[] { TestFactory.CreateSegment(0, 1, DecoGas, SegmentKind.Stop) };
        var cylinders = new[]
        {
            TestFactory.CreateCylinder(GasMixture.Air),
            TestFactory.CreateCylinder(DecoGas, 11, 200, CylinderPurpose.DecoGas)
        };

        // Act
        var result = ReserveGas.Calculate(segments, cylinders, TestFactory.CreateSettings());

        // Assert
        Assert.Equal(0, result.CylinderStatuses[0].RequiredReserve.InLiter, Precision);
    }

    [Theory]
    [InlineData(CylinderPurpose.Diluent)]
    [InlineData(CylinderPurpose.Oxygen)]
    public void Calculate_ShouldRequireNoReserve_WhenTheCylinderFeedsARebreatherLoop(CylinderPurpose purpose)
    {
        // Arrange
        var segments = new[] { TestFactory.CreateSegment(0, 12) };
        var cylinders = new[] { TestFactory.CreateCylinder(GasMixture.Air, 12, 200, purpose) };

        // Act
        var result = ReserveGas.Calculate(segments, cylinders, TestFactory.CreateSettings());

        // Assert
        Assert.Equal(0, result.CylinderStatuses[0].RequiredReserve.InLiter, Precision);
        Assert.Equal(2160, result.CylinderStatuses[0].ProjectedRemaining.InLiter, Precision);
    }

    [Fact]
    public void Calculate_ShouldRequireTheEmergencyAscentFromTheDeepestPoint_WhenTheCylinderIsBailout()
    {
        // Arrange
        // The bailout gas is never breathed in the plan, so its band runs from the deepest
        // point of the dive to the surface: 30 m to 6 m at 6 m/min and 6 m to the surface at
        // 1 m/min, for a team of two at 20 L/min and a stress factor of 1.5, giving
        // 2 x (20 x 1.5 x 2.765197 x 4 + 20 x 1.5 x 1.2941995 x 6) L. The gas that remains
        // excludes the 12 L x 10 bar that the first stage can no longer deliver.
        var segments = new[] { TestFactory.CreateSegment(30, 10) };
        var cylinders = new[] { TestFactory.CreateCylinder(GasMixture.Air, 12, 200, CylinderPurpose.Bailout) };

        // Act
        var result = ReserveGas.Calculate(segments, cylinders, TestFactory.CreateSettings());

        // Assert
        Assert.Equal(1129.5591, result.CylinderStatuses[0].RequiredReserve.InLiter, Precision);
        Assert.Equal(1491.601, result.CylinderStatuses[0].ProjectedRemaining.InLiter, Precision);
    }

    [Fact]
    public void Calculate_ShouldShortenTheBailoutBand_WhenARicherBailoutGasIsCarried()
    {
        // Arrange
        // The richer gas is breathable from its maximum operating depth upwards, so the
        // ascent on the leaner gas ends there rather than at the surface.
        var segments = new[] { TestFactory.CreateSegment(30, 10) };
        var alone = new[] { TestFactory.CreateCylinder(GasMixture.Air, 12, 200, CylinderPurpose.Bailout) };
        var withRicherGas = new[]
        {
            TestFactory.CreateCylinder(GasMixture.Air, 12, 200, CylinderPurpose.Bailout),
            TestFactory.CreateCylinder(DecoGas, 11, 200, CylinderPurpose.Bailout)
        };

        // Act
        var aloneResult = ReserveGas.Calculate(segments, alone, TestFactory.CreateSettings());
        var withRicherGasResult = ReserveGas.Calculate(segments, withRicherGas, TestFactory.CreateSettings());

        // Assert
        Assert.True(withRicherGasResult.CylinderStatuses[0].RequiredReserve.InLiter
                    < aloneResult.CylinderStatuses[0].RequiredReserve.InLiter);
        Assert.True(withRicherGasResult.CylinderStatuses[0].RequiredReserve.InLiter > 0);
    }

    [Fact]
    public void Calculate_ShouldNotShortenTheBailoutBand_WhenTheRicherGasIsNotCarriedForDecoOrBailout()
    {
        // Arrange
        // Only a decompression or bailout gas can end the emergency ascent early, so a
        // richer gas carried as bottom gas leaves the bailout band running to the surface.
        var segments = new[] { TestFactory.CreateSegment(30, 10) };
        var alone = new[] { TestFactory.CreateCylinder(GasMixture.Air, 12, 200, CylinderPurpose.Bailout) };
        var withRicherBottomGas = new[]
        {
            TestFactory.CreateCylinder(GasMixture.Air, 12, 200, CylinderPurpose.Bailout),
            TestFactory.CreateCylinder(DecoGas, 11)
        };

        // Act
        var aloneResult = ReserveGas.Calculate(segments, alone, TestFactory.CreateSettings());
        var withRicherBottomGasResult = ReserveGas.Calculate(segments, withRicherBottomGas,
            TestFactory.CreateSettings());

        // Assert
        Assert.Equal(aloneResult.CylinderStatuses[0].RequiredReserve.InLiter,
            withRicherBottomGasResult.CylinderStatuses[0].RequiredReserve.InLiter, Precision);
    }

    [Fact]
    public void Calculate_ShouldReportTheReserveAsSatisfied_WhenAmpleGasRemains()
    {
        // Arrange
        var segments = new[]
        {
            TestFactory.CreateSegment(30, 10),
            TestFactory.CreateSegment(0, 1, kind: SegmentKind.Ascent)
        };
        var cylinders = new[] { TestFactory.CreateCylinder(GasMixture.Air) };

        // Act
        var result = ReserveGas.Calculate(segments, cylinders, TestFactory.CreateSettings());

        // Assert
        Assert.True(result.AllSatisfied);
    }

    [Fact]
    public void Calculate_ShouldReportTheReserveAsUnsatisfied_WhenTheCylinderIsTooSmallForTheEmergencyAscent()
    {
        // Arrange
        var segments = new[]
        {
            TestFactory.CreateSegment(30, 2),
            TestFactory.CreateSegment(0, 1, kind: SegmentKind.Ascent)
        };
        var cylinders = new[] { TestFactory.CreateCylinder(GasMixture.Air, 12, 100) };

        // Act
        var result = ReserveGas.Calculate(segments, cylinders, TestFactory.CreateSettings());

        // Assert
        Assert.False(result.CylinderStatuses[0].IsSatisfied);
        Assert.False(result.AllSatisfied);
    }

    [Fact]
    public void Calculate_ShouldClampTheProjectedRemainingToZero_WhenTheDemandExceedsTheUsableGas()
    {
        // Arrange
        var segments = new[]
        {
            TestFactory.CreateSegment(30, 2),
            TestFactory.CreateSegment(0, 1, kind: SegmentKind.Ascent)
        };
        var cylinders = new[] { TestFactory.CreateCylinder(GasMixture.Air, 12, 20) };

        // Act
        var result = ReserveGas.Calculate(segments, cylinders, TestFactory.CreateSettings());

        // Assert
        Assert.Equal(0, result.CylinderStatuses[0].ProjectedRemaining.InLiter, Precision);
    }

    [Fact]
    public void Calculate_ShouldRequireMoreGas_WhenTheTeamIsLarger()
    {
        // Arrange
        var segments = new[]
        {
            TestFactory.CreateSegment(30, 10),
            TestFactory.CreateSegment(0, 1, kind: SegmentKind.Ascent)
        };
        var cylinders = new[] { TestFactory.CreateCylinder(GasMixture.Air) };
        var forPair = ReserveGas.Calculate(segments, cylinders, TestFactory.CreateSettings(reserveTeamSize: 2));

        // Act
        var forThree = ReserveGas.Calculate(segments, cylinders, TestFactory.CreateSettings(reserveTeamSize: 3));

        // Assert
        Assert.True(forThree.CylinderStatuses[0].RequiredReserve.InLiter
                    > forPair.CylinderStatuses[0].RequiredReserve.InLiter);
    }

    [Fact]
    public void Calculate_ShouldRequireMoreGas_WhenTheStressFactorIsHigher()
    {
        // Arrange
        var segments = new[]
        {
            TestFactory.CreateSegment(30, 10),
            TestFactory.CreateSegment(0, 1, kind: SegmentKind.Ascent)
        };
        var cylinders = new[] { TestFactory.CreateCylinder(GasMixture.Air) };
        var calm = ReserveGas.Calculate(segments, cylinders, TestFactory.CreateSettings(reserveStressFactor: 1.0));

        // Act
        var stressed = ReserveGas.Calculate(segments, cylinders, TestFactory.CreateSettings(reserveStressFactor: 2.0));

        // Assert
        Assert.True(stressed.CylinderStatuses[0].RequiredReserve.InLiter
                    > calm.CylinderStatuses[0].RequiredReserve.InLiter);
    }
}