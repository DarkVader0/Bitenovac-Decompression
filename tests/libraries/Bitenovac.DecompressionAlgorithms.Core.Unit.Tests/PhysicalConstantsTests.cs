using Bitenovac.DecompressionAlgorithms.Core.Environment;

namespace Bitenovac.DecompressionAlgorithms.Core.Unit.Tests;

public sealed class PhysicalConstantsTests
{
    private const int Precision = 5;

    [Theory]
    [InlineData(Salinity.Fresh, PhysicalConstants.FreshWaterDensity)]
    [InlineData(Salinity.Salt, PhysicalConstants.SaltWaterDensity)]
    [InlineData(Salinity.Brackish, PhysicalConstants.BrackishWaterDensity)]
    [InlineData(Salinity.EN13319, PhysicalConstants.En13319WaterDensity)]
    public void WaterDensity_ShouldReturnTheDensityForTheSalinity_WhenTheSalinityIsDefined(
        Salinity salinity,
        double expected)
    {
        // Arrange

        // Act
        var density = PhysicalConstants.WaterDensity(salinity);

        // Assert
        Assert.Equal(expected, density, Precision);
    }

    [Fact]
    public void WaterDensity_ShouldThrowArgumentOutOfRangeException_WhenTheSalinityIsNotDefined()
    {
        // Arrange
        const Salinity salinity = (Salinity)99;

        // Act
        Action act = () => PhysicalConstants.WaterDensity(salinity);

        // Assert
        Assert.Throws<ArgumentOutOfRangeException>(act);
    }

    [Fact]
    public void WaterDensity_ShouldReturnTheMeanOfFreshAndSalt_WhenTheSalinityIsBrackish()
    {
        // Arrange
        var expected = (PhysicalConstants.FreshWaterDensity + PhysicalConstants.SaltWaterDensity) / 2.0;

        // Act
        var density = PhysicalConstants.WaterDensity(Salinity.Brackish);

        // Assert
        Assert.Equal(expected, density, Precision);
    }

    [Theory]
    [InlineData(Salinity.Fresh, 980.665)]
    [InlineData(Salinity.Salt, 1010.08495)]
    [InlineData(Salinity.Brackish, 995.374975)]
    [InlineData(Salinity.EN13319, 1000.2783)]
    public void HydrostaticPressureMillibar_ShouldReturnTheColumnPressure_WhenTheDepthIsTenMeters(
        Salinity salinity,
        double expected)
    {
        // Arrange

        // Act
        var pressure = PhysicalConstants.HydrostaticPressureMillibar(salinity, 10);

        // Assert
        Assert.Equal(expected, pressure, Precision);
    }

    [Fact]
    public void HydrostaticPressureMillibar_ShouldReturnZero_WhenTheDepthIsZero()
    {
        // Arrange

        // Act
        var pressure = PhysicalConstants.HydrostaticPressureMillibar(Salinity.Salt, 0);

        // Assert
        Assert.Equal(0, pressure, Precision);
    }

    [Fact]
    public void HydrostaticPressureMillibar_ShouldScaleLinearlyWithDepth_WhenTheDepthIsDoubled()
    {
        // Arrange
        var atTen = PhysicalConstants.HydrostaticPressureMillibar(Salinity.Fresh, 10);

        // Act
        var atTwenty = PhysicalConstants.HydrostaticPressureMillibar(Salinity.Fresh, 20);

        // Assert
        Assert.Equal(atTen * 2.0, atTwenty, Precision);
    }

    [Fact]
    public void HydrostaticPressureMillibar_ShouldReturnMoreForSaltThanFresh_WhenTheDepthIsTheSame()
    {
        // Arrange
        var fresh = PhysicalConstants.HydrostaticPressureMillibar(Salinity.Fresh, 30);

        // Act
        var salt = PhysicalConstants.HydrostaticPressureMillibar(Salinity.Salt, 30);

        // Assert
        Assert.True(salt > fresh);
    }

    [Fact]
    public void AtmosphericPressureAtAltitudeMillibar_ShouldReturnSeaLevelPressure_WhenTheAltitudeIsZero()
    {
        // Arrange

        // Act
        var pressure = PhysicalConstants.AtmosphericPressureAtAltitudeMillibar(0);

        // Assert
        Assert.Equal(PhysicalConstants.SeaLevelPressureMillibar, pressure, Precision);
    }

    [Fact]
    public void
        AtmosphericPressureAtAltitudeMillibar_ShouldReturnTheBarometricValue_WhenTheAltitudeIsOneThousandMeters()
    {
        // Arrange

        // Act
        var pressure = PhysicalConstants.AtmosphericPressureAtAltitudeMillibar(1000);

        // Assert
        Assert.Equal(898.74765, pressure, Precision);
    }

    [Fact]
    public void AtmosphericPressureAtAltitudeMillibar_ShouldDecreaseWithAltitude_WhenTheAltitudeIncreases()
    {
        // Arrange
        var atOneThousand = PhysicalConstants.AtmosphericPressureAtAltitudeMillibar(1000);

        // Act
        var atTwoThousand = PhysicalConstants.AtmosphericPressureAtAltitudeMillibar(2000);

        // Assert
        Assert.True(atTwoThousand < atOneThousand);
    }
}