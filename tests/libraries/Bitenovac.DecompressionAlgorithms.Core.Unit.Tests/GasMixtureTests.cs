using Bitenovac.DecompressionAlgorithms.Core.Planning;
using Bitenovac.DecompressionAlgorithms.Units;

namespace Bitenovac.DecompressionAlgorithms.Core.Unit.Tests;

public sealed class GasMixtureTests
{
    private const int Precision = 5;

    [Fact]
    public void FromPercent_ShouldReturn21PercentO2_When21PercentO2AndNoHelium()
    {
        // Arrange
        const double percentO2 = 21;
        const double percentHe = 0;

        // Act
        var gas = GasMixture.FromPercent(percentO2, percentHe);

        // Assert
        Assert.Equal(21, gas.PercentO2);
    }

    [Fact]
    public void FromPercent_ShouldThrowArgumentOutOfRangeException_WhenPercentO2IsZero()
    {
        // Arrange
        const double percentO2 = 0;
        const double percentHe = 0;

        // Act
        Action act = () => GasMixture.FromPercent(percentO2, percentHe);

        // Assert
        Assert.Throws<ArgumentOutOfRangeException>(act);
    }

    [Fact]
    public void FromPercent_ShouldThrowArgumentOutOfRangeException_WhenPercentO2IsNegative()
    {
        // Arrange
        const double percentO2 = -1;
        const double percentHe = 0;

        // Act
        Action act = () => GasMixture.FromPercent(percentO2, percentHe);

        // Assert
        Assert.Throws<ArgumentOutOfRangeException>(act);
    }

    [Fact]
    public void FromPercent_ShouldThrowArgumentOutOfRangeException_WhenPercentO2ExceedsOneHundred()
    {
        // Arrange
        const double percentO2 = 100.1;
        const double percentHe = 0;

        // Act
        Action act = () => GasMixture.FromPercent(percentO2, percentHe);

        // Assert
        Assert.Throws<ArgumentOutOfRangeException>(act);
    }

    [Fact]
    public void FromPercent_ShouldThrowArgumentOutOfRangeException_WhenPercentHeIsNegative()
    {
        // Arrange
        const double percentO2 = 21;
        const double percentHe = -1;

        // Act
        Action act = () => GasMixture.FromPercent(percentO2, percentHe);

        // Assert
        Assert.Throws<ArgumentOutOfRangeException>(act);
    }

    [Fact]
    public void FromPercent_ShouldThrowArgumentOutOfRangeException_WhenPercentHeExceedsOneHundred()
    {
        // Arrange
        const double percentO2 = 21;
        const double percentHe = 100.1;

        // Act
        Action act = () => GasMixture.FromPercent(percentO2, percentHe);

        // Assert
        Assert.Throws<ArgumentOutOfRangeException>(act);
    }

    [Fact]
    public void FromPercent_ShouldThrowArgumentOutOfRangeException_WhenO2AndHeSumExceedsOneHundred()
    {
        // Arrange
        const double percentO2 = 60;
        const double percentHe = 41;

        // Act
        Action act = () => GasMixture.FromPercent(percentO2, percentHe);

        // Assert
        Assert.Throws<ArgumentOutOfRangeException>(act);
    }

    [Fact]
    public void FromPercent_ShouldSucceed_WhenO2AndHeSumToExactlyOneHundred()
    {
        // Arrange
        const double percentO2 = 60;
        const double percentHe = 40;

        // Act
        var gas = GasMixture.FromPercent(percentO2, percentHe);

        // Assert
        Assert.Equal(0, gas.PercentN2, Precision);
    }

    [Fact]
    public void FromPercent_ShouldSucceed_WhenPercentO2IsOneHundredAndNoHelium()
    {
        // Arrange
        const double percentO2 = 100;
        const double percentHe = 0;

        // Act
        var gas = GasMixture.FromPercent(percentO2, percentHe);

        // Assert
        Assert.Equal(100, gas.PercentO2);
    }

    [Fact]
    public void Air_ShouldHave21PercentO2AndNoHelium()
    {
        // Arrange

        // Act
        var gas = GasMixture.Air;

        // Assert
        Assert.Equal(21, gas.PercentO2);
        Assert.Equal(0, gas.PercentHe);
    }

    [Fact]
    public void Air_ShouldHave79PercentN2()
    {
        // Arrange

        // Act
        var gas = GasMixture.Air;

        // Assert
        Assert.Equal(79, gas.PercentN2, Precision);
    }

    [Fact]
    public void Oxygen_ShouldHave100PercentO2AndNoHeliumOrNitrogen()
    {
        // Arrange

        // Act
        var gas = GasMixture.Oxygen;

        // Assert
        Assert.Equal(100, gas.PercentO2);
        Assert.Equal(0, gas.PercentHe);
        Assert.Equal(0, gas.PercentN2, Precision);
    }

    [Fact]
    public void FractionO2_ShouldReturn0point21_When21PercentO2()
    {
        // Arrange
        var gas = GasMixture.FromPercent(21, 0);

        // Act
        var fraction = gas.FractionO2;

        // Assert
        Assert.Equal(0.21, fraction, Precision);
    }

    [Fact]
    public void FractionHe_ShouldReturn0point35_When35PercentHe()
    {
        // Arrange
        var gas = GasMixture.FromPercent(21, 35);

        // Act
        var fraction = gas.FractionHe;

        // Assert
        Assert.Equal(0.35, fraction, Precision);
    }

    [Fact]
    public void FractionN2_ShouldReturnBalance_WhenO2AndHeSpecified()
    {
        // Arrange
        var gas = GasMixture.FromPercent(21, 35);

        // Act
        var fraction = gas.FractionN2;

        // Assert
        Assert.Equal(0.44, fraction, Precision);
    }

    [Fact]
    public void PercentO2_ShouldRoundTrip_WhenConstructedFromPercent()
    {
        // Arrange
        const double expectedPercentO2 = 32;

        // Act
        var gas = GasMixture.FromPercent(expectedPercentO2, 0);

        // Assert
        Assert.Equal(expectedPercentO2, gas.PercentO2, Precision);
    }

    [Fact]
    public void PercentHe_ShouldRoundTrip_WhenConstructedFromPercent()
    {
        // Arrange
        const double expectedPercentHe = 45;

        // Act
        var gas = GasMixture.FromPercent(18, expectedPercentHe);

        // Assert
        Assert.Equal(expectedPercentHe, gas.PercentHe, Precision);
    }

    [Fact]
    public void PartialPressureO2_ShouldReturnCorrectPressure_WhenGivenAmbientPressure()
    {
        // Arrange
        var gas = GasMixture.FromPercent(21, 0);
        var ambient = Pressure.FromBar(4);

        // Act
        var po2 = gas.PartialPressureO2(ambient);

        // Assert
        Assert.Equal(0.84, po2.InBar, Precision);
    }

    [Fact]
    public void PartialPressureHe_ShouldReturnCorrectPressure_WhenGivenAmbientPressure()
    {
        // Arrange
        var gas = GasMixture.FromPercent(21, 35);
        var ambient = Pressure.FromBar(4);

        // Act
        var phe = gas.PartialPressureHe(ambient);

        // Assert
        Assert.Equal(1.4, phe.InBar, Precision);
    }

    [Fact]
    public void PartialPressureN2_ShouldReturnCorrectPressure_WhenGivenAmbientPressure()
    {
        // Arrange
        var gas = GasMixture.FromPercent(21, 35);
        var ambient = Pressure.FromBar(4);

        // Act
        var pn2 = gas.PartialPressureN2(ambient);

        // Assert
        Assert.Equal(1.76, pn2.InBar, Precision);
    }

    [Fact]
    public void Equals_ShouldReturnTrue_WhenMixturesHaveSameComposition()
    {
        // Arrange
        var gas1 = GasMixture.FromPercent(21, 35);
        var gas2 = GasMixture.FromPercent(21, 35);

        // Act
        var areEqual = gas1.Equals(gas2);

        // Assert
        Assert.True(areEqual);
    }

    [Fact]
    public void Equals_ShouldReturnFalse_WhenOxygenContentDiffers()
    {
        // Arrange
        var gas1 = GasMixture.FromPercent(21, 35);
        var gas2 = GasMixture.FromPercent(18, 35);

        // Act
        var areEqual = gas1.Equals(gas2);

        // Assert
        Assert.False(areEqual);
    }

    [Fact]
    public void Equals_ShouldReturnFalse_WhenHeliumContentDiffers()
    {
        // Arrange
        var gas1 = GasMixture.FromPercent(21, 35);
        var gas2 = GasMixture.FromPercent(21, 30);

        // Act
        var areEqual = gas1.Equals(gas2);

        // Assert
        Assert.False(areEqual);
    }

    [Fact]
    public void Equals_ShouldReturnTrue_WhenComparedWithSameInstance()
    {
        // Arrange
        var gas = GasMixture.FromPercent(21, 35);

        // Act
        var areEqual = gas.Equals(gas);

        // Assert
        Assert.True(areEqual);
    }

    [Fact]
    public void Equals_ShouldReturnTrue_WhenComparedWithEqualObject()
    {
        // Arrange
        var gas = GasMixture.FromPercent(21, 35);
        object equalObject = GasMixture.FromPercent(21, 35);

        // Act
        var areEqual = gas.Equals(equalObject);

        // Assert
        Assert.True(areEqual);
    }

    [Fact]
    public void Equals_ShouldReturnFalse_WhenComparedWithNull()
    {
        // Arrange
        var gas = GasMixture.FromPercent(21, 35);

        // Act
        var areEqual = gas.Equals(null);

        // Assert
        Assert.False(areEqual);
    }

    [Fact]
    public void Equals_ShouldReturnFalse_WhenComparedWithDifferentType()
    {
        // Arrange
        var gas = GasMixture.FromPercent(21, 35);
        var differentTypeObject = new object();

        // Act
        var areEqual = gas.Equals(differentTypeObject);

        // Assert
        Assert.False(areEqual);
    }

    [Fact]
    public void op_Equality_ShouldReturnTrue_WhenMixturesHaveSameComposition()
    {
        // Arrange
        var gas1 = GasMixture.FromPercent(21, 35);
        var gas2 = GasMixture.FromPercent(21, 35);

        // Act
        var areEqual = gas1 == gas2;

        // Assert
        Assert.True(areEqual);
    }

    [Fact]
    public void op_Equality_ShouldReturnFalse_WhenMixturesDiffer()
    {
        // Arrange
        var gas1 = GasMixture.FromPercent(21, 35);
        var gas2 = GasMixture.FromPercent(21, 30);

        // Act
        var areEqual = gas1 == gas2;

        // Assert
        Assert.False(areEqual);
    }

    [Fact]
    public void op_Inequality_ShouldReturnTrue_WhenMixturesDiffer()
    {
        // Arrange
        var gas1 = GasMixture.FromPercent(21, 35);
        var gas2 = GasMixture.FromPercent(21, 30);

        // Act
        var areNotEqual = gas1 != gas2;

        // Assert
        Assert.True(areNotEqual);
    }

    [Fact]
    public void op_Inequality_ShouldReturnFalse_WhenMixturesHaveSameComposition()
    {
        // Arrange
        var gas1 = GasMixture.FromPercent(21, 35);
        var gas2 = GasMixture.FromPercent(21, 35);

        // Act
        var areNotEqual = gas1 != gas2;

        // Assert
        Assert.False(areNotEqual);
    }

    [Fact]
    public void GetHashCode_ShouldReturnSameHashCode_WhenMixturesHaveSameComposition()
    {
        // Arrange
        var gas1 = GasMixture.FromPercent(21, 35);
        var gas2 = GasMixture.FromPercent(21, 35);

        // Act
        var hashCode1 = gas1.GetHashCode();
        var hashCode2 = gas2.GetHashCode();

        // Assert
        Assert.Equal(hashCode1, hashCode2);
    }

    [Fact]
    public void GetHashCode_ShouldReturnDifferentHashCode_WhenMixturesDiffer()
    {
        // Arrange
        var gas1 = GasMixture.FromPercent(21, 35);
        var gas2 = GasMixture.FromPercent(21, 30);

        // Act
        var hashCode1 = gas1.GetHashCode();
        var hashCode2 = gas2.GetHashCode();

        // Assert
        Assert.NotEqual(hashCode1, hashCode2);
    }

    [Fact]
    public void ToString_ShouldReturnAir_WhenMixtureIsAir()
    {
        // Arrange
        var gas = GasMixture.Air;

        // Act
        var name = gas.ToString();

        // Assert
        Assert.Equal("Air", name);
    }

    [Fact]
    public void ToString_ShouldReturnO2_WhenMixtureIsPureOxygen()
    {
        // Arrange
        var gas = GasMixture.Oxygen;

        // Act
        var name = gas.ToString();

        // Assert
        Assert.Equal("O2", name);
    }

    [Fact]
    public void ToString_ShouldReturnNitroxName_WhenMixtureIsHeliumFree()
    {
        // Arrange
        var gas = GasMixture.FromPercent(50, 0);

        // Act
        var name = gas.ToString();

        // Assert
        Assert.Equal("NX50", name);
    }

    [Fact]
    public void ToString_ShouldReturnTrimixName_WhenMixtureContainsHelium()
    {
        // Arrange
        var gas = GasMixture.FromPercent(18, 45);

        // Act
        var name = gas.ToString();

        // Assert
        Assert.Equal("TX18/45", name);
    }

    [Fact]
    public void ToString_ShouldReturnTrimixName_WhenAirLikeOxygenContentContainsHelium()
    {
        // Arrange
        var gas = GasMixture.FromPercent(21, 35);

        // Act
        var name = gas.ToString();

        // Assert
        Assert.Equal("TX21/35", name);
    }

    [Fact]
    public void ToString_ShouldRoundToNearestWholePercent_WhenContentIsFractional()
    {
        // Arrange
        var gas = GasMixture.FromPercent(49.6, 0);

        // Act
        var name = gas.ToString();

        // Assert
        Assert.Equal("NX50", name);
    }
}