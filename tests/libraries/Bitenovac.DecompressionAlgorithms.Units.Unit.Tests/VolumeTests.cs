namespace Bitenovac.DecompressionAlgorithms.Units.Unit.Tests;

public sealed class VolumeTests
{
    private const int Precision = 5;

    [Fact]
    public void FromLiter_ShouldReturn1000ml_When1Liter()
    {
        // Arrange
        const double liter = 1;

        // Act
        var volume = Volume.FromLiter(liter);

        // Assert
        Assert.Equal(1000, volume.InMilliliter);
    }

    [Fact]
    public void FromCubicFeet_ShouldReturn28316point846592ml_When1CubicFoot()
    {
        // Arrange
        const double cubicFeet = 1;

        // Act
        var volume = Volume.FromCubicFeet(cubicFeet);

        // Assert
        Assert.Equal(28316.846592, volume.InMilliliter, Precision);
    }

    [Fact]
    public void FromMilliliter_ShouldReturn1000ml_When1000ml()
    {
        // Arrange
        const double milliliter = 1000;

        // Act
        var volume = Volume.FromMilliliter(milliliter);

        // Assert
        Assert.Equal(1000, volume.InMilliliter);
    }

    [Fact]
    public void InLiter_ShouldReturn1Liter_When1000ml()
    {
        // Arrange
        var volume = Volume.FromMilliliter(1000);

        // Act
        var liter = volume.InLiter;

        // Assert
        Assert.Equal(1, liter);
    }

    [Fact]
    public void InCubicFeet_ShouldReturn0point0353147CubicFeet_When1000ml()
    {
        // Arrange
        var volume = Volume.FromMilliliter(1000);

        // Act
        var cubicFeet = volume.InCubicFeet;

        // Assert
        Assert.Equal(0.0353147, cubicFeet, Precision);
    }

    [Fact]
    public void Zero_ShouldReturn0Milliliter()
    {
        // Arrange

        // Act
        var volume = Volume.Zero;

        // Assert
        Assert.Equal(0, volume.InMilliliter);
    }

    [Fact]
    public void Equals_ShouldReturnTrue_WhenVolumesAreEqual()
    {
        // Arrange
        var volume1 = Volume.FromMilliliter(1000);
        var volume2 = Volume.FromLiter(1);

        // Act
        var areEqual = volume1.Equals(volume2);

        // Assert
        Assert.True(areEqual);
    }

    [Fact]
    public void Equals_ShouldReturnFalse_WhenVolumesAreNotEqual()
    {
        // Arrange
        var volume1 = Volume.FromMilliliter(1000);
        var volume2 = Volume.FromLiter(2);

        // Act
        var areEqual = volume1.Equals(volume2);

        // Assert
        Assert.False(areEqual);
    }

    [Fact]
    public void Equals_ShouldReturnTrue_WhenComparedWithSameInstance()
    {
        // Arrange
        var volume = Volume.FromMilliliter(1000);

        // Act
        var areEqual = volume.Equals(volume);

        // Assert
        Assert.True(areEqual);
    }

    [Fact]
    public void Equals_ShouldReturnTrue_WhenComparedWithEqualObject()
    {
        // Arrange
        var volume = Volume.FromMilliliter(1000);
        object equalObject = Volume.FromLiter(1);

        // Act
        var areEqual = volume.Equals(equalObject);

        // Assert
        Assert.True(areEqual);
    }

    [Fact]
    public void Equals_ShouldReturnFalse_WhenComparedWithNull()
    {
        // Arrange
        var volume = Volume.FromMilliliter(1000);

        // Act
        var areEqual = volume.Equals(null);

        // Assert
        Assert.False(areEqual);
    }

    [Fact]
    public void Equals_ShouldReturnFalse_WhenComparedWithDifferentType()
    {
        // Arrange
        var volume = Volume.FromMilliliter(1000);
        var differentTypeObject = new object();

        // Act
        var areEqual = volume.Equals(differentTypeObject);

        // Assert
        Assert.False(areEqual);
    }

    [Fact]
    public void op_Equality_ShouldReturnTrue_WhenVolumesAreEqual()
    {
        // Arrange
        var volume1 = Volume.FromMilliliter(1000);
        var volume2 = Volume.FromLiter(1);

        // Act
        var areEqual = volume1 == volume2;

        // Assert
        Assert.True(areEqual);
    }

    [Fact]
    public void op_Equality_ShouldReturnFalse_WhenVolumesAreNotEqual()
    {
        // Arrange
        var volume1 = Volume.FromMilliliter(1000);
        var volume2 = Volume.FromLiter(2);

        // Act
        var areEqual = volume1 == volume2;

        // Assert
        Assert.False(areEqual);
    }

    [Fact]
    public void op_Inequality_ShouldReturnTrue_WhenVolumesAreNotEqual()
    {
        // Arrange
        var volume1 = Volume.FromMilliliter(1000);
        var volume2 = Volume.FromLiter(2);

        // Act
        var areNotEqual = volume1 != volume2;

        // Assert
        Assert.True(areNotEqual);
    }

    [Fact]
    public void op_Inequality_ShouldReturnFalse_WhenVolumesAreEqual()
    {
        // Arrange
        var volume1 = Volume.FromMilliliter(1000);
        var volume2 = Volume.FromLiter(1);

        // Act
        var areNotEqual = volume1 != volume2;

        // Assert
        Assert.False(areNotEqual);
    }

    [Fact]
    public void GetHashCode_ShouldReturnSameHashCode_WhenVolumesAreEqual()
    {
        // Arrange
        var volume1 = Volume.FromMilliliter(1000);
        var volume2 = Volume.FromLiter(1);

        // Act
        var hashCode1 = volume1.GetHashCode();
        var hashCode2 = volume2.GetHashCode();

        // Assert
        Assert.Equal(hashCode1, hashCode2);
    }

    [Fact]
    public void GetHashCode_ShouldReturnDifferentHashCode_WhenVolumesAreNotEqual()
    {
        // Arrange
        var volume1 = Volume.FromMilliliter(1000);
        var volume2 = Volume.FromLiter(2);

        // Act
        var hashCode1 = volume1.GetHashCode();
        var hashCode2 = volume2.GetHashCode();

        // Assert
        Assert.NotEqual(hashCode1, hashCode2);
    }

    [Fact]
    public void CompareTo_ShouldReturnZero_WhenVolumesAreEqual()
    {
        // Arrange
        var volume1 = Volume.FromMilliliter(1000);
        var volume2 = Volume.FromLiter(1);

        // Act
        var comparisonResult = volume1.CompareTo(volume2);

        // Assert
        Assert.Equal(0, comparisonResult);
    }

    [Fact]
    public void CompareTo_ShouldReturnNegative_WhenVolume1IsLessThanVolume2()
    {
        // Arrange
        var volume1 = Volume.FromMilliliter(1000);
        var volume2 = Volume.FromLiter(2);

        // Act
        var comparisonResult = volume1.CompareTo(volume2);

        // Assert
        Assert.True(comparisonResult < 0);
    }

    [Fact]
    public void CompareTo_ShouldReturnPositive_WhenVolume1IsGreaterThanVolume2()
    {
        // Arrange
        var volume1 = Volume.FromLiter(2);
        var volume2 = Volume.FromMilliliter(1000);

        // Act
        var comparisonResult = volume1.CompareTo(volume2);

        // Assert
        Assert.True(comparisonResult > 0);
    }

    [Fact]
    public void op_LessThan_ShouldReturnTrue_WhenVolume1IsLessThanVolume2()
    {
        // Arrange
        var volume1 = Volume.FromLiter(1);
        var volume2 = Volume.FromLiter(2);

        // Act
        var isLessThan = volume1 < volume2;

        // Assert
        Assert.True(isLessThan);
    }

    [Fact]
    public void op_LessThanOrEqual_ShouldReturnTrue_WhenVolumesAreEqual()
    {
        // Arrange
        var volume1 = Volume.FromMilliliter(1000);
        var volume2 = Volume.FromLiter(1);

        // Act
        var isLessThanOrEqual = volume1 <= volume2;

        // Assert
        Assert.True(isLessThanOrEqual);
    }

    [Fact]
    public void op_GreaterThan_ShouldReturnTrue_WhenVolume1IsGreaterThanVolume2()
    {
        // Arrange
        var volume1 = Volume.FromLiter(2);
        var volume2 = Volume.FromLiter(1);

        // Act
        var isGreaterThan = volume1 > volume2;

        // Assert
        Assert.True(isGreaterThan);
    }

    [Fact]
    public void op_GreaterThanOrEqual_ShouldReturnTrue_WhenVolumesAreEqual()
    {
        // Arrange
        var volume1 = Volume.FromMilliliter(1000);
        var volume2 = Volume.FromLiter(1);

        // Act
        var isGreaterThanOrEqual = volume1 >= volume2;

        // Assert
        Assert.True(isGreaterThanOrEqual);
    }

    [Fact]
    public void op_Minus_ShouldReturnCorrectVolume_WhenSubtractingTwoVolumes()
    {
        // Arrange
        var volume1 = Volume.FromLiter(2);
        var volume2 = Volume.FromMilliliter(1000);

        // Act
        var resultVolume = volume1 - volume2;

        // Assert
        Assert.Equal(1, resultVolume.InLiter);
    }

    [Fact]
    public void op_Plus_ShouldReturnCorrectVolume_WhenAddingTwoVolumes()
    {
        // Arrange
        var volume1 = Volume.FromLiter(2);
        var volume2 = Volume.FromMilliliter(1000);

        // Act
        var resultVolume = volume1 + volume2;

        // Assert
        Assert.Equal(3, resultVolume.InLiter);
    }

    [Fact]
    public void op_Multiply_Scalar_ShouldReturnCorrectVolume_WhenMultiplyingVolumeByScalar()
    {
        // Arrange
        var volume = Volume.FromLiter(2);
        const double scalar = 3;

        // Act
        var resultVolume = volume * scalar;

        // Assert
        Assert.Equal(6, resultVolume.InLiter);
    }

    [Fact]
    public void op_Divide_Scalar_ShouldReturnCorrectVolume_WhenDividingVolumeByScalar()
    {
        // Arrange
        var volume = Volume.FromLiter(4);
        const double scalar = 2;

        // Act
        var resultVolume = volume / scalar;

        // Assert
        Assert.Equal(2, resultVolume.InLiter);
    }

    [Fact]
    public void FromLiter_ShouldReturnOriginalLiter_WhenConvertedBackToLiter()
    {
        // Arrange
        const double expectedLiter = 3.5;

        // Act
        var actualLiter = Volume.FromLiter(expectedLiter).InLiter;

        // Assert
        Assert.Equal(expectedLiter, actualLiter);
    }

    [Fact]
    public void FromCubicFeet_ShouldReturnOriginalCubicFeet_WhenConvertedBackToCubicFeet()
    {
        // Arrange
        const double expectedCubicFeet = 2.5;

        // Act
        var actualCubicFeet = Volume.FromCubicFeet(expectedCubicFeet).InCubicFeet;

        // Assert
        Assert.Equal(expectedCubicFeet, actualCubicFeet, Precision);
    }
}