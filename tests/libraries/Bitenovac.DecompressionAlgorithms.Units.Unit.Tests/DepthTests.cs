namespace Bitenovac.DecompressionAlgorithms.Units.Unit.Tests;

public sealed class DepthTests
{
    private const int Precision = 5;

    [Fact]
    public void FromMeter_ShouldReturn1000mm_When1Meter()
    {
        // Arrange
        const double meter = 1;

        // Act
        var depth = Depth.FromMeter(meter);

        // Assert
        Assert.Equal(1000, depth.InMillimeter);
    }

    [Fact]
    public void FromFeet_ShouldReturn1000mm_When3point2808399Feet()
    {
        // Arrange
        const double feet = 3.2808399;

        // Act
        var depth = Depth.FromFeet(feet);

        // Assert
        Assert.Equal(1000, depth.InMillimeter, Precision);
    }

    [Fact]
    public void FromMillimeter_ShouldReturn1000mm_When1000mm()
    {
        // Arrange
        const double millimeter = 1000;

        // Act
        var depth = Depth.FromMillimeter(millimeter);

        // Assert
        Assert.Equal(1000, depth.InMillimeter);
    }

    [Fact]
    public void InMeter_ShouldReturn1Meter_When1000mm()
    {
        // Arrange
        var depth = Depth.FromMillimeter(1000);

        // Act
        var meter = depth.InMeter;

        // Assert
        Assert.Equal(1, meter);
    }

    [Fact]
    public void InFeet_ShouldReturn3point2808399Feet_When1000mm()
    {
        // Arrange
        var depth = Depth.FromMillimeter(1000);

        // Act
        var feet = depth.InFeet;

        // Assert
        Assert.Equal(3.2808399, feet, Precision);
    }

    [Fact]
    public void Zero_ShouldReturn0Millimeter()
    {
        // Arrange

        // Act
        var depth = Depth.Zero;

        // Assert
        Assert.Equal(0, depth.InMillimeter);
    }

    [Fact]
    public void Equals_ShouldReturnTrue_WhenDepthsAreEqual()
    {
        // Arrange
        var depth1 = Depth.FromMillimeter(1000);
        var depth2 = Depth.FromMeter(1);

        // Act
        var areEqual = depth1.Equals(depth2);

        // Assert
        Assert.True(areEqual);
    }

    [Fact]
    public void Equals_ShouldReturnFalse_WhenDepthsAreNotEqual()
    {
        // Arrange
        var depth1 = Depth.FromMillimeter(1000);
        var depth2 = Depth.FromMeter(2);

        // Act
        var areEqual = depth1.Equals(depth2);

        // Assert
        Assert.False(areEqual);
    }

    [Fact]
    public void Equals_ShouldReturnTrue_WhenComparedWithSameInstance()
    {
        // Arrange
        var depth = Depth.FromMillimeter(1000);

        // Act
        var areEqual = depth.Equals(depth);

        // Assert
        Assert.True(areEqual);
    }

    [Fact]
    public void Equals_ShouldReturnTrue_WhenComparedWithEqualObject()
    {
        // Arrange
        var depth = Depth.FromMillimeter(1000);
        object equalObject = Depth.FromMeter(1);

        // Act
        var areEqual = depth.Equals(equalObject);

        // Assert
        Assert.True(areEqual);
    }

    [Fact]
    public void Equals_ShouldReturnFalse_WhenComparedWithNull()
    {
        // Arrange
        var depth = Depth.FromMillimeter(1000);

        // Act
        var areEqual = depth.Equals(null);

        // Assert
        Assert.False(areEqual);
    }

    [Fact]
    public void Equals_ShouldReturnFalse_WhenComparedWithDifferentType()
    {
        // Arrange
        var depth = Depth.FromMillimeter(1000);
        var differentTypeObject = new object();

        // Act
        var areEqual = depth.Equals(differentTypeObject);

        // Assert
        Assert.False(areEqual);
    }

    [Fact]
    public void op_Equality_ShouldReturnTrue_WhenDepthsAreEqual()
    {
        // Arrange
        var depth1 = Depth.FromMillimeter(1000);
        var depth2 = Depth.FromMeter(1);

        // Act
        var areEqual = depth1 == depth2;

        // Assert
        Assert.True(areEqual);
    }

    [Fact]
    public void op_Equality_ShouldReturnFalse_WhenDepthsAreNotEqual()
    {
        // Arrange
        var depth1 = Depth.FromMillimeter(1000);
        var depth2 = Depth.FromMeter(2);

        // Act
        var areEqual = depth1 == depth2;

        // Assert
        Assert.False(areEqual);
    }

    [Fact]
    public void op_Inequality_ShouldReturnTrue_WhenDepthsAreNotEqual()
    {
        // Arrange
        var depth1 = Depth.FromMillimeter(1000);
        var depth2 = Depth.FromMeter(2);

        // Act
        var areNotEqual = depth1 != depth2;

        // Assert
        Assert.True(areNotEqual);
    }

    [Fact]
    public void op_Inequality_ShouldReturnFalse_WhenDepthsAreEqual()
    {
        // Arrange
        var depth1 = Depth.FromMillimeter(1000);
        var depth2 = Depth.FromMeter(1);

        // Act
        var areNotEqual = depth1 != depth2;

        // Assert
        Assert.False(areNotEqual);
    }

    [Fact]
    public void GetHashCode_ShouldReturnSameHashCode_WhenDepthsAreEqual()
    {
        // Arrange
        var depth1 = Depth.FromMillimeter(1000);
        var depth2 = Depth.FromMeter(1);

        // Act
        var hashCode1 = depth1.GetHashCode();
        var hashCode2 = depth2.GetHashCode();

        // Assert
        Assert.Equal(hashCode1, hashCode2);
    }

    [Fact]
    public void GetHashCode_ShouldReturnDifferentHashCode_WhenDepthsAreNotEqual()
    {
        // Arrange
        var depth1 = Depth.FromMillimeter(1000);
        var depth2 = Depth.FromMeter(2);

        // Act
        var hashCode1 = depth1.GetHashCode();
        var hashCode2 = depth2.GetHashCode();

        // Assert
        Assert.NotEqual(hashCode1, hashCode2);
    }

    [Fact]
    public void CompareTo_ShouldReturnZero_WhenDepthsAreEqual()
    {
        // Arrange
        var depth1 = Depth.FromMillimeter(1000);
        var depth2 = Depth.FromMeter(1);

        // Act
        var comparisonResult = depth1.CompareTo(depth2);

        // Assert
        Assert.Equal(0, comparisonResult);
    }

    [Fact]
    public void CompareTo_ShouldReturnNegative_WhenDepth1IsLessThanDepth2()
    {
        // Arrange
        var depth1 = Depth.FromMillimeter(1000);
        var depth2 = Depth.FromMeter(2);

        // Act
        var comparisonResult = depth1.CompareTo(depth2);

        // Assert
        Assert.True(comparisonResult < 0);
    }

    [Fact]
    public void CompareTo_ShouldReturnPositive_WhenDepth1IsGreaterThanDepth2()
    {
        // Arrange
        var depth1 = Depth.FromMeter(2);
        var depth2 = Depth.FromMillimeter(1000);

        // Act
        var comparisonResult = depth1.CompareTo(depth2);

        // Assert
        Assert.True(comparisonResult > 0);
    }

    [Fact]
    public void op_LessThan_ShouldReturnTrue_WhenDepth1IsLessThanDepth2()
    {
        // Arrange
        var depth1 = Depth.FromMeter(1);
        var depth2 = Depth.FromMeter(2);

        // Act
        var isLessThan = depth1 < depth2;

        // Assert
        Assert.True(isLessThan);
    }

    [Fact]
    public void op_LessThanOrEqual_ShouldReturnTrue_WhenDepthsAreEqual()
    {
        // Arrange
        var depth1 = Depth.FromMillimeter(1000);
        var depth2 = Depth.FromMeter(1);

        // Act
        var isLessThanOrEqual = depth1 <= depth2;

        // Assert
        Assert.True(isLessThanOrEqual);
    }

    [Fact]
    public void op_GreaterThan_ShouldReturnTrue_WhenDepth1IsGreaterThanDepth2()
    {
        // Arrange
        var depth1 = Depth.FromMeter(2);
        var depth2 = Depth.FromMeter(1);

        // Act
        var isGreaterThan = depth1 > depth2;

        // Assert
        Assert.True(isGreaterThan);
    }

    [Fact]
    public void op_GreaterThanOrEqual_ShouldReturnTrue_WhenDepthsAreEqual()
    {
        // Arrange
        var depth1 = Depth.FromMillimeter(1000);
        var depth2 = Depth.FromMeter(1);

        // Act
        var isGreaterThanOrEqual = depth1 >= depth2;

        // Assert
        Assert.True(isGreaterThanOrEqual);
    }

    [Fact]
    public void op_Minus_ShouldReturnCorrectDepth_WhenSubtractingTwoDepths()
    {
        // Arrange
        var depth1 = Depth.FromMeter(2);
        var depth2 = Depth.FromMillimeter(1000);

        // Act
        var resultDepth = depth1 - depth2;

        // Assert
        Assert.Equal(1, resultDepth.InMeter);
    }

    [Fact]
    public void op_Plus_ShouldReturnCorrectDepth_WhenAddingTwoDepths()
    {
        // Arrange
        var depth1 = Depth.FromMeter(2);
        var depth2 = Depth.FromMillimeter(1000);

        // Act
        var resultDepth = depth1 + depth2;

        // Assert
        Assert.Equal(3, resultDepth.InMeter);
    }

    [Fact]
    public void op_Multiply_Scalar_ShouldReturnCorrectDepth_WhenMultiplyingDepthByScalar()
    {
        // Arrange
        var depth = Depth.FromMeter(2);
        const double scalar = 3;

        // Act
        var resultDepth = depth * scalar;

        // Assert
        Assert.Equal(6, resultDepth.InMeter);
    }

    [Fact]
    public void op_Divide_Scalar_ShouldReturnCorrectDepth_WhenDividingDepthByScalar()
    {
        // Arrange
        var depth = Depth.FromMeter(4);
        const double scalar = 2;

        // Act
        var resultDepth = depth / scalar;

        // Assert
        Assert.Equal(2, resultDepth.InMeter);
    }

    [Fact]
    public void FromMeter_ShouldReturnOriginalMeter_WhenConvertedBackToMeter()
    {
        // Arrange
        const double expectedMeter = 3.5;

        // Act
        var actualMeter = Depth.FromMeter(expectedMeter).InMeter;

        // Assert
        Assert.Equal(expectedMeter, actualMeter);
    }

    [Fact]
    public void FromFeet_ShouldReturnOriginalFeet_WhenConvertedBackToFeet()
    {
        // Arrange
        const double expectedFeet = 125.75;

        // Act
        var actualFeet = Depth.FromFeet(expectedFeet).InFeet;

        // Assert
        Assert.Equal(expectedFeet, actualFeet, Precision);
    }
}