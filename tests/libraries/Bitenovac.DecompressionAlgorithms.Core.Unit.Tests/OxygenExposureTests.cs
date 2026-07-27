using Bitenovac.DecompressionAlgorithms.Core.Calculations;
using Bitenovac.DecompressionAlgorithms.Core.Planning;

namespace Bitenovac.DecompressionAlgorithms.Core.Unit.Tests;

public sealed class OxygenExposureTests
{
    private const int Precision = 5;
    private const int FractionPrecision = 8;

    [Fact]
    public void Calculate_ShouldThrowArgumentNullException_WhenSegmentsIsNull()
    {
        // Arrange
        IReadOnlyList<DiveSegment> segments = null!;

        // Act
        Action act = () => OxygenExposure.Calculate(segments, TestFactory.CreateSettings());

        // Assert
        Assert.Throws<ArgumentNullException>(act);
    }

    [Fact]
    public void Calculate_ShouldThrowArgumentNullException_WhenSettingsIsNull()
    {
        // Arrange
        var segments = new[] { TestFactory.CreateSegment(10, 10) };

        // Act
        Action act = () => OxygenExposure.Calculate(segments, null!);

        // Assert
        Assert.Throws<ArgumentNullException>(act);
    }

    [Fact]
    public void Calculate_ShouldReturnZeroExposure_WhenThereAreNoSegments()
    {
        // Arrange
        var segments = Array.Empty<DiveSegment>();

        // Act
        var result = OxygenExposure.Calculate(segments, TestFactory.CreateSettings());

        // Assert
        Assert.Equal(0, result.CentralNervousSystemFraction, Precision);
        Assert.Equal(0, result.OxygenToleranceUnits, Precision);
    }

    [Fact]
    public void Calculate_ShouldReturnZeroExposure_WhenThePartialPressureStaysBelowTheThreshold()
    {
        // Arrange
        var segments = new[] { TestFactory.CreateSegment(0, 60) };

        // Act
        var result = OxygenExposure.Calculate(segments, TestFactory.CreateSettings());

        // Assert
        Assert.Equal(0, result.CentralNervousSystemFraction, Precision);
        Assert.Equal(0, result.OxygenToleranceUnits, Precision);
    }

    [Fact]
    public void Calculate_ShouldAccrueOneOxygenToleranceUnit_WhenPureOxygenIsBreathedAtTheSurfaceForOneMinute()
    {
        // Arrange
        var segments = new[] { TestFactory.CreateSegment(0, 1, GasMixture.Oxygen) };

        // Act
        var result = OxygenExposure.Calculate(segments, TestFactory.CreateSettings());

        // Assert
        Assert.Equal(1.0, result.OxygenToleranceUnits, Precision);
    }

    [Fact]
    public void Calculate_ShouldExpressTheCentralNervousSystemToxicityAsAFractionOfTheLimit_WhenOxygenIsBreathed()
    {
        // Arrange
        var segments = new[] { TestFactory.CreateSegment(0, 1, GasMixture.Oxygen) };

        // Act
        var result = OxygenExposure.Calculate(segments, TestFactory.CreateSettings());

        // Assert
        Assert.Equal(0.00317571, result.CentralNervousSystemFraction, FractionPrecision);
    }

    [Fact]
    public void Calculate_ShouldAccumulateAcrossSegments_WhenSeveralSegmentsAreExposed()
    {
        // Arrange
        var segments = new[]
        {
            TestFactory.CreateSegment(0, 1, GasMixture.Oxygen),
            TestFactory.CreateSegment(0, 1, GasMixture.Oxygen)
        };

        // Act
        var result = OxygenExposure.Calculate(segments, TestFactory.CreateSettings());

        // Assert
        Assert.Equal(2.0, result.OxygenToleranceUnits, Precision);
        Assert.Equal(0.00635141, result.CentralNervousSystemFraction, FractionPrecision);
    }

    [Fact]
    public void Calculate_ShouldTreatTheFirstSegmentAsStartingAtTheSurface_WhenItBeginsAtDepth()
    {
        // Arrange
        var flat = new[]
        {
            TestFactory.CreateSegment(40, 0, kind: SegmentKind.Descent),
            TestFactory.CreateSegment(40, 10)
        };
        var ramped = new[] { TestFactory.CreateSegment(40, 10) };

        // Act
        var flatResult = OxygenExposure.Calculate(flat, TestFactory.CreateSettings());
        var rampedResult = OxygenExposure.Calculate(ramped, TestFactory.CreateSettings());

        // Assert
        Assert.True(flatResult.OxygenToleranceUnits > rampedResult.OxygenToleranceUnits);
    }

    [Fact]
    public void Calculate_ShouldCarryTheDepthForward_WhenTheFollowingSegmentIsHeldAtTheSameDepth()
    {
        // Arrange
        var segments = new[]
        {
            TestFactory.CreateSegment(0, 1, GasMixture.Oxygen),
            TestFactory.CreateSegment(0, 1, GasMixture.Oxygen)
        };
        var single = new[] { TestFactory.CreateSegment(0, 2, GasMixture.Oxygen) };

        // Act
        var split = OxygenExposure.Calculate(segments, TestFactory.CreateSettings());
        var whole = OxygenExposure.Calculate(single, TestFactory.CreateSettings());

        // Assert
        Assert.Equal(whole.OxygenToleranceUnits, split.OxygenToleranceUnits, Precision);
    }

    [Fact]
    public void Calculate_ShouldAccrueMoreExposure_WhenTheSameGasIsBreathedDeeper()
    {
        // Arrange
        var shallow = new[] { TestFactory.CreateSegment(20, 10) };
        var deep = new[] { TestFactory.CreateSegment(40, 10) };

        // Act
        var shallowResult = OxygenExposure.Calculate(shallow, TestFactory.CreateSettings());
        var deepResult = OxygenExposure.Calculate(deep, TestFactory.CreateSettings());

        // Assert
        Assert.True(deepResult.OxygenToleranceUnits > shallowResult.OxygenToleranceUnits);
        Assert.True(deepResult.CentralNervousSystemFraction > shallowResult.CentralNervousSystemFraction);
    }

    [Fact]
    public void Calculate_ShouldAccrueMoreExposure_WhenTheGasIsRicherAtTheSameDepth()
    {
        // Arrange
        var onAir = new[] { TestFactory.CreateSegment(30, 20) };
        var onNitrox = new[] { TestFactory.CreateSegment(30, 20, GasMixture.FromPercent(32, 0)) };

        // Act
        var airResult = OxygenExposure.Calculate(onAir, TestFactory.CreateSettings());
        var nitroxResult = OxygenExposure.Calculate(onNitrox, TestFactory.CreateSettings());

        // Assert
        Assert.True(nitroxResult.OxygenToleranceUnits > airResult.OxygenToleranceUnits);
    }
}