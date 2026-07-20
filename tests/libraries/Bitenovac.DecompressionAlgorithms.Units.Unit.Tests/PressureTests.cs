namespace Bitenovac.DecompressionAlgorithms.Units.Unit.Tests;

public sealed class PressureTests
{
    private const int Precision = 5;

    [Fact]
    public void FromBar_ShouldReturn1000mbar_When1bar()
    {
        // Arrange
        const double bar = 1;

        // Act
        var pressure = Pressure.FromBar(bar);

        // Assert
        Assert.Equal(1000, pressure.InMillibar);
    }

    [Fact]
    public void FromPsi_ShouldReturn1000mbar_When14point5037738Psi()
    {
        // Arrange
        const double psi = 14.5037738;

        // Act
        var pressure = Pressure.FromPsi(psi);

        // Assert
        Assert.Equal(1000, pressure.InMillibar, Precision);
    }

    [Fact]
    public void FromMillibar_ShouldReturn1000mbar_When1000mbar()
    {
        // Arrange
        const double millibar = 1000;

        // Act
        var pressure = Pressure.FromMillibar(millibar);

        // Assert
        Assert.Equal(1000, pressure.InMillibar);
    }

    [Fact]
    public void FromAta_ShouldReturn1000mbar_When0point98692327Ata()
    {
        // Arrange
        const double ata = 0.98692327;

        // Act
        var pressure = Pressure.FromAta(ata);

        // Assert
        Assert.Equal(1000, pressure.InMillibar, Precision);
    }

    [Fact]
    public void InBar_ShouldReturn1bar_When1000mbar()
    {
        // Arrange
        var pressure = Pressure.FromMillibar(1000);

        // Act
        var bar = pressure.InBar;

        // Assert
        Assert.Equal(1, bar);
    }

    [Fact]
    public void InPsi_ShouldReturn14point5037738Psi_When1000mbar()
    {
        // Arrange
        var pressure = Pressure.FromMillibar(1000);

        // Act
        var psi = pressure.InPsi;

        // Assert
        Assert.Equal(14.5037738, psi, Precision);
    }

    [Fact]
    public void InAta_ShouldReturn0point98692327Ata_When1000mbar()
    {
        // Arrange
        var pressure = Pressure.FromMillibar(1000);

        // Act
        var ata = pressure.InAta;

        // Assert
        Assert.Equal(0.98692327, ata, Precision);
    }

    [Fact]
    public void Equals_ShouldReturnTrue_WhenPressuresAreEqual()
    {
        // Arrange
        var pressure1 = Pressure.FromMillibar(1000);
        var pressure2 = Pressure.FromBar(1);

        // Act
        var areEqual = pressure1.Equals(pressure2);

        // Assert
        Assert.True(areEqual);
    }

    [Fact]
    public void Equals_ShouldReturnFalse_WhenPressuresAreNotEqual()
    {
        // Arrange
        var pressure1 = Pressure.FromMillibar(1000);
        var pressure2 = Pressure.FromBar(2);

        // Act
        var areEqual = pressure1.Equals(pressure2);

        // Assert
        Assert.False(areEqual);
    }

    [Fact]
    public void Equals_ShouldReturnFalse_WhenComparedWithNull()
    {
        // Arrange
        var pressure = Pressure.FromMillibar(1000);

        // Act
        var areEqual = pressure.Equals(null);

        // Assert
        Assert.False(areEqual);
    }

    [Fact]
    public void Equals_ShouldReturnFalse_WhenComparedWithDifferentType()
    {
        // Arrange
        var pressure = Pressure.FromMillibar(1000);
        var differentTypeObject = new object();

        // Act
        var areEqual = pressure.Equals(differentTypeObject);

        // Assert
        Assert.False(areEqual);
    }

    [Fact]
    public void op_Equality_ShouldReturnTrue_WhenPressuresAreEqual()
    {
        // Arrange
        var pressure1 = Pressure.FromMillibar(1000);
        var pressure2 = Pressure.FromBar(1);

        // Act
        var areEqual = pressure1 == pressure2;

        // Assert
        Assert.True(areEqual);
    }

    [Fact]
    public void op_Equality_ShouldReturnFalse_WhenPressuresAreNotEqual()
    {
        // Arrange
        var pressure1 = Pressure.FromMillibar(1000);
        var pressure2 = Pressure.FromBar(2);

        // Act
        var areEqual = pressure1 == pressure2;

        // Assert
        Assert.False(areEqual);
    }

    [Fact]
    public void op_Inequality_ShouldReturnTrue_WhenPressuresAreNotEqual()
    {
        // Arrange
        var pressure1 = Pressure.FromMillibar(1000);
        var pressure2 = Pressure.FromBar(2);

        // Act
        var areNotEqual = pressure1 != pressure2;

        // Assert
        Assert.True(areNotEqual);
    }

    [Fact]
    public void op_Inequality_ShouldReturnFalse_WhenPressuresAreEqual()
    {
        // Arrange
        var pressure1 = Pressure.FromMillibar(1000);
        var pressure2 = Pressure.FromBar(1);

        // Act
        var areNotEqual = pressure1 != pressure2;

        // Assert
        Assert.False(areNotEqual);
    }

    [Fact]
    public void GetHashCode_ShouldReturnSameHashCode_WhenPressuresAreEqual()
    {
        // Arrange
        var pressure1 = Pressure.FromMillibar(1000);
        var pressure2 = Pressure.FromBar(1);

        // Act
        var hashCode1 = pressure1.GetHashCode();
        var hashCode2 = pressure2.GetHashCode();

        // Assert
        Assert.Equal(hashCode1, hashCode2);
    }

    [Fact]
    public void GetHashCode_ShouldReturnDifferentHashCode_WhenPressuresAreNotEqual()
    {
        // Arrange
        var pressure1 = Pressure.FromMillibar(1000);
        var pressure2 = Pressure.FromBar(2);

        // Act
        var hashCode1 = pressure1.GetHashCode();
        var hashCode2 = pressure2.GetHashCode();

        // Assert
        Assert.NotEqual(hashCode1, hashCode2);
    }

    [Fact]
    public void CompareTo_ShouldReturnZero_WhenPressuresAreEqual()
    {
        // Arrange
        var pressure1 = Pressure.FromMillibar(1000);
        var pressure2 = Pressure.FromBar(1);

        // Act
        var comparisonResult = pressure1.CompareTo(pressure2);

        // Assert
        Assert.Equal(0, comparisonResult);
    }

    [Fact]
    public void CompareTo_ShouldReturnNegative_WhenPressure1IsLessThanPressure2()
    {
        // Arrange
        var pressure1 = Pressure.FromMillibar(1000);
        var pressure2 = Pressure.FromBar(2);

        // Act
        var comparisonResult = pressure1.CompareTo(pressure2);

        // Assert
        Assert.True(comparisonResult < 0);
    }

    [Fact]
    public void CompareTo_ShouldReturnPositive_WhenPressure1IsGreaterThanPressure2()
    {
        // Arrange
        var pressure1 = Pressure.FromBar(2);
        var pressure2 = Pressure.FromMillibar(1000);

        // Act
        var comparisonResult = pressure1.CompareTo(pressure2);

        // Assert
        Assert.True(comparisonResult > 0);
    }

    [Fact]
    public void op_Minus_ShouldReturnCorrectPressure_WhenSubtractingTwoPressures()
    {
        // Arrange
        var pressure1 = Pressure.FromBar(2);
        var pressure2 = Pressure.FromMillibar(1000);

        // Act
        var resultPressure = pressure1 - pressure2;

        // Assert
        Assert.Equal(1, resultPressure.InBar);
    }

    [Fact]
    public void op_Plus_ShouldReturnCorrectPressure_WhenAddingTwoPressures()
    {
        // Arrange
        var pressure1 = Pressure.FromBar(2);
        var pressure2 = Pressure.FromMillibar(1000);

        // Act
        var resultPressure = pressure1 + pressure2;

        // Assert
        Assert.Equal(3, resultPressure.InBar);
    }

    [Fact]
    public void op_Multiply_Scalar_ShouldReturnCorrectPressure_WhenMultiplyingPressureByScalar()
    {
        // Arrange
        var pressure = Pressure.FromBar(2);
        const double scalar = 3;

        // Act
        var resultPressure = pressure * scalar;

        // Assert
        Assert.Equal(6, resultPressure.InBar);
    }

    [Fact]
    public void op_Divide_Scalar_ShouldReturnCorrectPressure_WhenDividingPressureByScalar()
    {
        // Arrange
        var pressure = Pressure.FromBar(4);
        const double scalar = 2;

        // Act
        var resultPressure = pressure / scalar;

        // Assert
        Assert.Equal(2, resultPressure.InBar);
    }

    [Fact]
    public void op_Divide_ShouldReturnCorrectScalar_WhenDividingPressureByPressure()
    {
        // Arrange
        var pressure1 = Pressure.FromBar(4);
        var pressure2 = Pressure.FromBar(2);

        // Act
        var resultScalar = pressure1 / pressure2;

        // Assert
        Assert.Equal(2, resultScalar);
    }

    [Fact]
    public void Zero_ShouldReturn0Millibar()
    {
        // Arrange

        // Act
        var pressure = Pressure.Zero;

        // Assert
        Assert.Equal(0, pressure.InMillibar);
    }

    [Fact]
    public void Equals_ShouldReturnTrue_WhenComparedWithSameInstance()
    {
        // Arrange
        var pressure = Pressure.FromMillibar(1000);

        // Act
        var areEqual = pressure.Equals(pressure);

        // Assert
        Assert.True(areEqual);
    }

    [Fact]
    public void Equals_ShouldReturnTrue_WhenComparedWithEqualObject()
    {
        // Arrange
        var pressure = Pressure.FromMillibar(1000);
        object equalObject = Pressure.FromBar(1);

        // Act
        var areEqual = pressure.Equals(equalObject);

        // Assert
        Assert.True(areEqual);
    }

    [Fact]
    public void op_LessThan_ShouldReturnTrue_WhenPressure1IsLessThanPressure2()
    {
        // Arrange
        var pressure1 = Pressure.FromBar(1);
        var pressure2 = Pressure.FromBar(2);

        // Act
        var isLessThan = pressure1 < pressure2;

        // Assert
        Assert.True(isLessThan);
    }

    [Fact]
    public void op_LessThan_ShouldReturnFalse_WhenPressure1IsGreaterThanPressure2()
    {
        // Arrange
        var pressure1 = Pressure.FromBar(2);
        var pressure2 = Pressure.FromBar(1);

        // Act
        var isLessThan = pressure1 < pressure2;

        // Assert
        Assert.False(isLessThan);
    }

    [Fact]
    public void op_LessThanOrEqual_ShouldReturnTrue_WhenPressuresAreEqual()
    {
        // Arrange
        var pressure1 = Pressure.FromMillibar(1000);
        var pressure2 = Pressure.FromBar(1);

        // Act
        var isLessThanOrEqual = pressure1 <= pressure2;

        // Assert
        Assert.True(isLessThanOrEqual);
    }

    [Fact]
    public void op_LessThanOrEqual_ShouldReturnTrue_WhenPressure1IsLessThanPressure2()
    {
        // Arrange
        var pressure1 = Pressure.FromBar(1);
        var pressure2 = Pressure.FromBar(2);

        // Act
        var isLessThanOrEqual = pressure1 <= pressure2;

        // Assert
        Assert.True(isLessThanOrEqual);
    }

    [Fact]
    public void op_GreaterThan_ShouldReturnTrue_WhenPressure1IsGreaterThanPressure2()
    {
        // Arrange
        var pressure1 = Pressure.FromBar(2);
        var pressure2 = Pressure.FromBar(1);

        // Act
        var isGreaterThan = pressure1 > pressure2;

        // Assert
        Assert.True(isGreaterThan);
    }

    [Fact]
    public void op_GreaterThan_ShouldReturnFalse_WhenPressure1IsLessThanPressure2()
    {
        // Arrange
        var pressure1 = Pressure.FromBar(1);
        var pressure2 = Pressure.FromBar(2);

        // Act
        var isGreaterThan = pressure1 > pressure2;

        // Assert
        Assert.False(isGreaterThan);
    }

    [Fact]
    public void op_GreaterThanOrEqual_ShouldReturnTrue_WhenPressuresAreEqual()
    {
        // Arrange
        var pressure1 = Pressure.FromMillibar(1000);
        var pressure2 = Pressure.FromBar(1);

        // Act
        var isGreaterThanOrEqual = pressure1 >= pressure2;

        // Assert
        Assert.True(isGreaterThanOrEqual);
    }

    [Fact]
    public void op_GreaterThanOrEqual_ShouldReturnTrue_WhenPressure1IsGreaterThanPressure2()
    {
        // Arrange
        var pressure1 = Pressure.FromBar(2);
        var pressure2 = Pressure.FromBar(1);

        // Act
        var isGreaterThanOrEqual = pressure1 >= pressure2;

        // Assert
        Assert.True(isGreaterThanOrEqual);
    }

    [Fact]
    public void FromBar_ShouldReturnOriginalBar_WhenConvertedBackToBar()
    {
        // Arrange
        const double expectedBar = 3.5;

        // Act
        var actualBar = Pressure.FromBar(expectedBar).InBar;

        // Assert
        Assert.Equal(expectedBar, actualBar);
    }

    [Fact]
    public void FromPsi_ShouldReturnOriginalPsi_WhenConvertedBackToPsi()
    {
        // Arrange
        const double expectedPsi = 125.75;

        // Act
        var actualPsi = Pressure.FromPsi(expectedPsi).InPsi;

        // Assert
        Assert.Equal(expectedPsi, actualPsi, Precision);
    }

    [Fact]
    public void FromAta_ShouldReturnOriginalAta_WhenConvertedBackToAta()
    {
        // Arrange
        const double expectedAta = 2.4;

        // Act
        var actualAta = Pressure.FromAta(expectedAta).InAta;

        // Assert
        Assert.Equal(expectedAta, actualAta, Precision);
    }
}