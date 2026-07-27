using Bitenovac.DecompressionAlgorithms.Core.Calculations;
using Bitenovac.DecompressionAlgorithms.Units;

namespace Bitenovac.DecompressionAlgorithms.Core.Unit.Tests;

public sealed class OxygenToxicityTests
{
    private const int Precision = 5;

    [Theory]
    [InlineData(0)]
    [InlineData(210)]
    [InlineData(499)]
    [InlineData(500)]
    public void CnsRatePerSecond_ShouldReturnZero_WhenThePartialPressureIsAtOrBelowTheThreshold(int po2Mbar)
    {
        // Arrange

        // Act
        var rate = OxygenToxicity.CnsRatePerSecond(po2Mbar);

        // Assert
        Assert.Equal(0, rate, Precision);
    }

    [Fact]
    public void CnsRatePerSecond_ShouldReturnAPositiveRate_WhenThePartialPressureExceedsTheThreshold()
    {
        // Arrange
        const int po2Mbar = 501;

        // Act
        var rate = OxygenToxicity.CnsRatePerSecond(po2Mbar);

        // Assert
        Assert.True(rate > 0);
    }

    [Fact]
    public void CnsRatePerSecond_ShouldReturnTheLowBranchValue_WhenThePartialPressureIsWithinTheLowBranch()
    {
        // Arrange
        const int po2Mbar = 1000;

        // Act
        var rate = OxygenToxicity.CnsRatePerSecond(po2Mbar);

        // Assert
        Assert.Equal(5.29284e-05, rate, 10);
    }

    [Fact]
    public void CnsRatePerSecond_ShouldReturnTheHighBranchValue_WhenThePartialPressureExceedsFifteenHundred()
    {
        // Arrange
        const int po2Mbar = 1600;

        // Act
        var rate = OxygenToxicity.CnsRatePerSecond(po2Mbar);

        // Assert
        Assert.Equal(0.00035562, rate, 8);
    }

    [Fact]
    public void CnsRatePerSecond_ShouldIncreaseWithThePartialPressure_WhenBothValuesAreWithinTheLowBranch()
    {
        // Arrange
        var atOneThousand = OxygenToxicity.CnsRatePerSecond(1000);

        // Act
        var atFourteenHundred = OxygenToxicity.CnsRatePerSecond(1400);

        // Assert
        Assert.True(atFourteenHundred > atOneThousand);
    }

    [Fact]
    public void CalculateCns_ShouldReturnZero_WhenThePartialPressureIsAtOrBelowTheThreshold()
    {
        // Arrange
        const int po2Mbar = 500;

        // Act
        var cns = OxygenToxicity.CalculateCns(po2Mbar, 3600);

        // Assert
        Assert.Equal(0, cns, Precision);
    }

    [Fact]
    public void CalculateCns_ShouldReturnZero_WhenTheDurationIsZero()
    {
        // Arrange
        const int po2Mbar = 1400;

        // Act
        var cns = OxygenToxicity.CalculateCns(po2Mbar, 0);

        // Assert
        Assert.Equal(0, cns, Precision);
    }

    [Fact]
    public void CalculateCns_ShouldReturnThePercentageOfTheSingleExposureLimit_WhenThePartialPressureIsToxic()
    {
        // Arrange
        const int po2Mbar = 1000;

        // Act
        var cns = OxygenToxicity.CalculateCns(po2Mbar, 60);

        // Assert
        Assert.Equal(0.31757, cns, Precision);
    }

    [Fact]
    public void CalculateCns_ShouldScaleLinearlyWithDuration_WhenTheDurationIsDoubled()
    {
        // Arrange
        var forOneMinute = OxygenToxicity.CalculateCns(1400, 60);

        // Act
        var forTwoMinutes = OxygenToxicity.CalculateCns(1400, 120);

        // Assert
        Assert.Equal(forOneMinute * 2.0, forTwoMinutes, Precision);
    }

    [Fact]
    public void CalculateCnsTransition_ShouldEqualTheFixedCalculation_WhenTheStartAndEndPressuresAreEqual()
    {
        // Arrange
        var expected = OxygenToxicity.CalculateCns(1400, 60);

        // Act
        var cns = OxygenToxicity.CalculateCnsTransition(1400, 1400, 60);

        // Assert
        Assert.Equal(expected, cns, Precision);
    }

    [Fact]
    public void CalculateCnsTransition_ShouldIntegrateOverTheRamp_WhenThePressureChangesAcrossTheSegment()
    {
        // Arrange

        // Act
        var cns = OxygenToxicity.CalculateCnsTransition(600, 1400, 60);

        // Assert
        Assert.Equal(0.35037, cns, Precision);
    }

    [Fact]
    public void CalculateCnsTransition_ShouldExceedTheMeanEvaluation_WhenThePressureChangesAcrossTheSegment()
    {
        // Arrange
        var atTheMean = OxygenToxicity.CalculateCns(1000, 60);

        // Act
        var cns = OxygenToxicity.CalculateCnsTransition(600, 1400, 60);

        // Assert
        Assert.True(cns > atTheMean);
    }

    [Fact]
    public void CalculateCnsTransition_ShouldAccrueTheToxicPortionOnly_WhenTheRampStartsBelowTheThreshold()
    {
        // Arrange

        // Act
        var cns = OxygenToxicity.CalculateCnsTransition(210, 700, 60);

        // Assert
        Assert.Equal(0.06006, cns, Precision);
    }

    [Fact]
    public void CalculateCnsTransition_ShouldReturnZero_WhenTheWholeRampIsAtOrBelowTheThreshold()
    {
        // Arrange

        // Act
        var cns = OxygenToxicity.CalculateCnsTransition(210, 500, 60);

        // Assert
        Assert.Equal(0, cns, Precision);
    }

    [Fact]
    public void CalculateCnsTransition_ShouldIntegrateAcrossBothBranches_WhenTheRampCrossesTheBranchPoint()
    {
        // Arrange

        // Act
        var cns = OxygenToxicity.CalculateCnsTransition(1400, 1600, 60);

        // Assert
        Assert.Equal(1.06035, cns, Precision);
    }

    [Fact]
    public void CalculateCnsTransition_ShouldReturnTheSameValue_WhenTheRampIsReversed()
    {
        // Arrange
        var rising = OxygenToxicity.CalculateCnsTransition(600, 1400, 60);

        // Act
        var falling = OxygenToxicity.CalculateCnsTransition(1400, 600, 60);

        // Assert
        Assert.Equal(rising, falling, Precision);
    }

    [Fact]
    public void CalculateCnsTransition_ShouldNotTruncateTheRamp_WhenTheEndpointsDifferByOneMillibar()
    {
        // Arrange
        var flat = OxygenToxicity.CalculateCns(1000, 60);

        // Act
        var cns = OxygenToxicity.CalculateCnsTransition(1000, 1001, 60);

        // Assert
        Assert.True(cns > flat);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(210)]
    [InlineData(500)]
    public void CalculateOtu_ShouldReturnZero_WhenThePartialPressureIsAtOrBelowTheThreshold(int po2Mbar)
    {
        // Arrange

        // Act
        var otu = OxygenToxicity.CalculateOtu(po2Mbar, 3600);

        // Assert
        Assert.Equal(0, otu, Precision);
    }

    [Fact]
    public void CalculateOtu_ShouldReturnOneUnitPerMinute_WhenThePartialPressureIsOneBar()
    {
        // Arrange
        const int po2Mbar = 1000;

        // Act
        var otu = OxygenToxicity.CalculateOtu(po2Mbar, 60);

        // Assert
        Assert.Equal(1.0, otu, Precision);
    }

    [Fact]
    public void CalculateOtu_ShouldReturnTheBakerRelationValue_WhenThePartialPressureIsOnePointFiveBar()
    {
        // Arrange
        const int po2Mbar = 1500;

        // Act
        var otu = OxygenToxicity.CalculateOtu(po2Mbar, 60);

        // Assert
        Assert.Equal(1.77769, otu, Precision);
    }

    [Fact]
    public void CalculateOtu_ShouldScaleLinearlyWithDuration_WhenTheDurationIsDoubled()
    {
        // Arrange
        var forOneMinute = OxygenToxicity.CalculateOtu(1400, 60);

        // Act
        var forTwoMinutes = OxygenToxicity.CalculateOtu(1400, 120);

        // Assert
        Assert.Equal(forOneMinute * 2.0, forTwoMinutes, Precision);
    }

    [Fact]
    public void CalculateOtuTransition_ShouldReturnZero_WhenBothEndpointsAreAtOrBelowTheThreshold()
    {
        // Arrange

        // Act
        var otu = OxygenToxicity.CalculateOtuTransition(210, 500, 3600);

        // Assert
        Assert.Equal(0, otu, Precision);
    }

    [Fact]
    public void CalculateOtuTransition_ShouldEqualTheFixedCalculation_WhenTheStartAndEndPressuresAreEqual()
    {
        // Arrange
        var expected = OxygenToxicity.CalculateOtu(1400, 60);

        // Act
        var otu = OxygenToxicity.CalculateOtuTransition(1400, 1400, 60);

        // Assert
        Assert.Equal(expected, otu, Precision);
    }

    [Fact]
    public void CalculateOtuTransition_ShouldIntegrateOverTheRamp_WhenThePressureRisesFromTheThreshold()
    {
        // Arrange

        // Act
        var otu = OxygenToxicity.CalculateOtuTransition(500, 1500, 60);

        // Assert
        Assert.Equal(0.97141, otu, Precision);
    }

    [Fact]
    public void CalculateOtuTransition_ShouldClipToTheToxicPortion_WhenTheStartPressureIsBelowTheThreshold()
    {
        // Arrange

        // Act
        var otu = OxygenToxicity.CalculateOtuTransition(0, 1000, 60);

        // Assert
        Assert.Equal(0.27322, otu, Precision);
    }

    [Fact]
    public void CalculateOtuTransition_ShouldReturnTheSameValue_WhenTheRampIsReversed()
    {
        // Arrange
        var rising = OxygenToxicity.CalculateOtuTransition(0, 1000, 60);

        // Act
        var falling = OxygenToxicity.CalculateOtuTransition(1000, 0, 60);

        // Assert
        Assert.Equal(rising, falling, Precision);
    }

    [Fact]
    public void CalculateOtuTransition_ShouldReturnZero_WhenTheDurationIsZero()
    {
        // Arrange

        // Act
        var otu = OxygenToxicity.CalculateOtuTransition(1000, 1500, 0);

        // Assert
        Assert.Equal(0, otu, Precision);
    }

    [Fact]
    public void CalculateCnsTransition_ShouldMatchTheIntegerOverload_WhenCalledWithPressureAndTimeSpan()
    {
        // Arrange
        var expected = OxygenToxicity.CalculateCnsTransition(1000, 1400, 60);

        // Act
        var cns = OxygenToxicity.CalculateCnsTransition(
            Pressure.FromMillibar(1000),
            Pressure.FromMillibar(1400),
            TimeSpan.FromSeconds(60));

        // Assert
        Assert.Equal(expected, cns, Precision);
    }

    [Fact]
    public void CalculateOtuTransition_ShouldMatchTheIntegerOverload_WhenCalledWithPressureAndTimeSpan()
    {
        // Arrange
        var expected = OxygenToxicity.CalculateOtuTransition(1000, 1500, 60);

        // Act
        var otu = OxygenToxicity.CalculateOtuTransition(
            Pressure.FromMillibar(1000),
            Pressure.FromMillibar(1500),
            TimeSpan.FromSeconds(60));

        // Assert
        Assert.Equal(expected, otu, Precision);
    }

    [Fact]
    public void CalculateCnsTransition_ShouldThrowArgumentOutOfRangeException_WhenTheDurationIsNegative()
    {
        // Arrange
        var duration = TimeSpan.FromSeconds(-1);

        // Act
        Action act = () => OxygenToxicity.CalculateCnsTransition(
            Pressure.FromMillibar(1000),
            Pressure.FromMillibar(1400),
            duration);

        // Assert
        Assert.Throws<ArgumentOutOfRangeException>(act);
    }

    [Fact]
    public void CalculateOtuTransition_ShouldThrowArgumentOutOfRangeException_WhenTheDurationIsNegative()
    {
        // Arrange
        var duration = TimeSpan.FromSeconds(-1);

        // Act
        Action act = () => OxygenToxicity.CalculateOtuTransition(
            Pressure.FromMillibar(1000),
            Pressure.FromMillibar(1500),
            duration);

        // Assert
        Assert.Throws<ArgumentOutOfRangeException>(act);
    }
}
