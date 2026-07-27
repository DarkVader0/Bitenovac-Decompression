using Bitenovac.DecompressionAlgorithms.Core.Calculations;

namespace Bitenovac.DecompressionAlgorithms.Core.Unit.Tests;

public sealed class OxygenExposureResultTests
{
    private const int Precision = 5;

    [Fact]
    public void CentralNervousSystemFraction_ShouldReturnConstructorValue_WhenSet()
    {
        // Arrange
        const double cnsFraction = 0.42;

        // Act
        var result = new OxygenExposureResult(cnsFraction, 55);

        // Assert
        Assert.Equal(0.42, result.CentralNervousSystemFraction, Precision);
    }

    [Fact]
    public void OxygenToleranceUnits_ShouldReturnConstructorValue_WhenSet()
    {
        // Arrange
        const double otu = 55;

        // Act
        var result = new OxygenExposureResult(0.42, otu);

        // Assert
        Assert.Equal(55, result.OxygenToleranceUnits, Precision);
    }

    [Fact]
    public void Constructor_ShouldSucceed_WhenBothValuesAreZero()
    {
        // Arrange

        // Act
        var result = new OxygenExposureResult(0, 0);

        // Assert
        Assert.Equal(0, result.CentralNervousSystemFraction, Precision);
        Assert.Equal(0, result.OxygenToleranceUnits, Precision);
    }

    [Fact]
    public void Constructor_ShouldSucceed_WhenTheCentralNervousSystemFractionExceedsOne()
    {
        // Arrange
        const double cnsFraction = 1.5;

        // Act
        var result = new OxygenExposureResult(cnsFraction, 0);

        // Assert
        Assert.Equal(1.5, result.CentralNervousSystemFraction, Precision);
    }

    [Fact]
    public void Constructor_ShouldThrowArgumentOutOfRangeException_WhenTheCentralNervousSystemFractionIsNegative()
    {
        // Arrange
        const double cnsFraction = -0.01;

        // Act
        Action act = () => new OxygenExposureResult(cnsFraction, 0);

        // Assert
        Assert.Throws<ArgumentOutOfRangeException>(act);
    }

    [Fact]
    public void Constructor_ShouldThrowArgumentOutOfRangeException_WhenTheOxygenToleranceUnitsAreNegative()
    {
        // Arrange
        const double otu = -0.01;

        // Act
        Action act = () => new OxygenExposureResult(0, otu);

        // Assert
        Assert.Throws<ArgumentOutOfRangeException>(act);
    }
}