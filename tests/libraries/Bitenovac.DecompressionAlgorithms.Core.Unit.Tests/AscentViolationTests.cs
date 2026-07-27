using Bitenovac.DecompressionAlgorithms.Core.Planning;
using Bitenovac.DecompressionAlgorithms.Units;

namespace Bitenovac.DecompressionAlgorithms.Core.Unit.Tests;

public sealed class AscentViolationTests
{
    private const int Precision = 5;

    [Fact]
    public void FromDepth_ShouldReturnConstructorValue_WhenSet()
    {
        // Arrange
        var fromDepth = Depth.FromMeter(30);
        var toDepth = Depth.FromMeter(20);
        var ceiling = Depth.FromMeter(24);

        // Act
        var violation = new AscentViolation(fromDepth, toDepth, ceiling);

        // Assert
        Assert.Equal(30, violation.FromDepth.InMeter, Precision);
    }

    [Fact]
    public void ToDepth_ShouldReturnConstructorValue_WhenSet()
    {
        // Arrange
        var fromDepth = Depth.FromMeter(30);
        var toDepth = Depth.FromMeter(20);
        var ceiling = Depth.FromMeter(24);

        // Act
        var violation = new AscentViolation(fromDepth, toDepth, ceiling);

        // Assert
        Assert.Equal(20, violation.ToDepth.InMeter, Precision);
    }

    [Fact]
    public void Ceiling_ShouldReturnConstructorValue_WhenSet()
    {
        // Arrange
        var fromDepth = Depth.FromMeter(30);
        var toDepth = Depth.FromMeter(20);
        var ceiling = Depth.FromMeter(24);

        // Act
        var violation = new AscentViolation(fromDepth, toDepth, ceiling);

        // Assert
        Assert.Equal(24, violation.Ceiling.InMeter, Precision);
    }

    [Fact]
    public void Constructor_ShouldAllowFromDepthAndToDepthToBeEqual()
    {
        // Arrange
        var fromDepth = Depth.FromMeter(20);
        var toDepth = Depth.FromMeter(20);
        var ceiling = Depth.FromMeter(20);

        // Act
        var violation = new AscentViolation(fromDepth, toDepth, ceiling);

        // Assert
        Assert.Equal(violation.FromDepth.InMeter, violation.ToDepth.InMeter, Precision);
    }

    [Fact]
    public void Constructor_ShouldAllowZeroDepths()
    {
        // Arrange
        var fromDepth = Depth.FromMeter(0);
        var toDepth = Depth.FromMeter(0);
        var ceiling = Depth.FromMeter(0);

        // Act
        var violation = new AscentViolation(fromDepth, toDepth, ceiling);

        // Assert
        Assert.Equal(0, violation.Ceiling.InMeter, Precision);
    }

    [Fact]
    public void Equals_ShouldReturnFalse_WhenComparingTwoDistinctInstancesWithSameValues()
    {
        // Arrange
        var violation1 = new AscentViolation(Depth.FromMeter(30), Depth.FromMeter(20), Depth.FromMeter(24));
        var violation2 = new AscentViolation(Depth.FromMeter(30), Depth.FromMeter(20), Depth.FromMeter(24));

        // Act
        var areEqual = violation1.Equals(violation2);

        // Assert
        Assert.False(areEqual);
    }

    [Fact]
    public void Equals_ShouldReturnTrue_WhenComparedWithSameInstance()
    {
        // Arrange
        var violation = new AscentViolation(Depth.FromMeter(30), Depth.FromMeter(20), Depth.FromMeter(24));

        // Act
        var areEqual = violation.Equals(violation);

        // Assert
        Assert.True(areEqual);
    }
}