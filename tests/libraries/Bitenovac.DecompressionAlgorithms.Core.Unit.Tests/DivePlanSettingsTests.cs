using Bitenovac.DecompressionAlgorithms.Core.Environment;
using Bitenovac.DecompressionAlgorithms.Core.Planning;
using Bitenovac.DecompressionAlgorithms.Units;

namespace Bitenovac.DecompressionAlgorithms.Core.Unit.Tests;

public sealed class DivePlanSettingsTests
{
    private const int Precision = 5;

    private static DivePlanSettings CreateSettings(
        double descentRateMetersPerMinute = 20,
        double ascentRateBelow75PercentMetersPerMinute = 10,
        double ascentRate75To50PercentMetersPerMinute = 9,
        double ascentRate50PercentToStopsMetersPerMinute = 6,
        double ascentRateLastSixMetersMetersPerMinute = 1,
        double bottomSacLitersPerMinute = 20,
        double decoSacLitersPerMinute = 15,
        Pressure? bottomPo2 = null,
        Pressure? decoPo2 = null,
        Pressure? reservePressure = null,
        double reserveStressFactor = 1.5,
        int reserveTeamSize = 2,
        TimeSpan? stopTimeIncrement = null,
        TimeSpan? problemSolvingTime = null,
        TimeSpan? minimumGasSwitchDuration = null,
        bool safetyStop = true,
        bool lastStopAtSixMeters = false,
        bool switchAtRequiredStop = false,
        bool oxygenBreaks = false,
        bool oxygenIsNarcotic = false) =>
        new(
            Pressure.FromBar(1),
            Salinity.Salt,
            descentRateMetersPerMinute,
            ascentRateBelow75PercentMetersPerMinute,
            ascentRate75To50PercentMetersPerMinute,
            ascentRate50PercentToStopsMetersPerMinute,
            ascentRateLastSixMetersMetersPerMinute,
            bottomSacLitersPerMinute,
            decoSacLitersPerMinute,
            bottomPo2 ?? Pressure.FromBar(1.4),
            decoPo2 ?? Pressure.FromBar(1.6),
            reservePressure ?? Pressure.FromBar(50),
            reserveStressFactor,
            reserveTeamSize,
            stopTimeIncrement ?? TimeSpan.FromMinutes(1),
            problemSolvingTime ?? TimeSpan.FromMinutes(1),
            minimumGasSwitchDuration ?? TimeSpan.FromSeconds(30),
            safetyStop,
            lastStopAtSixMeters,
            switchAtRequiredStop,
            oxygenBreaks,
            oxygenIsNarcotic);

    [Fact]
    public void Constructor_ShouldSucceed_WhenAllValuesAreValid()
    {
        // Arrange

        // Act
        var settings = CreateSettings();

        // Assert
        Assert.Equal(20, settings.DescentRateMetersPerMinute, Precision);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Constructor_ShouldThrowArgumentOutOfRangeException_WhenDescentRateIsNotPositive(double rate)
    {
        // Arrange

        // Act
        Action act = () => CreateSettings(rate);

        // Assert
        Assert.Throws<ArgumentOutOfRangeException>(act);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Constructor_ShouldThrowArgumentOutOfRangeException_WhenAscentRateBelow75PercentIsNotPositive(
        double rate)
    {
        // Arrange

        // Act
        Action act = () => CreateSettings(ascentRateBelow75PercentMetersPerMinute: rate);

        // Assert
        Assert.Throws<ArgumentOutOfRangeException>(act);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Constructor_ShouldThrowArgumentOutOfRangeException_WhenAscentRate75To50PercentIsNotPositive(
        double rate)
    {
        // Arrange

        // Act
        Action act = () => CreateSettings(ascentRate75To50PercentMetersPerMinute: rate);

        // Assert
        Assert.Throws<ArgumentOutOfRangeException>(act);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Constructor_ShouldThrowArgumentOutOfRangeException_WhenAscentRate50PercentToStopsIsNotPositive(
        double rate)
    {
        // Arrange

        // Act
        Action act = () => CreateSettings(ascentRate50PercentToStopsMetersPerMinute: rate);

        // Assert
        Assert.Throws<ArgumentOutOfRangeException>(act);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Constructor_ShouldThrowArgumentOutOfRangeException_WhenAscentRateLastSixMetersIsNotPositive(
        double rate)
    {
        // Arrange

        // Act
        Action act = () => CreateSettings(ascentRateLastSixMetersMetersPerMinute: rate);

        // Assert
        Assert.Throws<ArgumentOutOfRangeException>(act);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Constructor_ShouldThrowArgumentOutOfRangeException_WhenBottomSacIsNotPositive(double rate)
    {
        // Arrange

        // Act
        Action act = () => CreateSettings(bottomSacLitersPerMinute: rate);

        // Assert
        Assert.Throws<ArgumentOutOfRangeException>(act);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Constructor_ShouldThrowArgumentOutOfRangeException_WhenDecoSacIsNotPositive(double rate)
    {
        // Arrange

        // Act
        Action act = () => CreateSettings(decoSacLitersPerMinute: rate);

        // Assert
        Assert.Throws<ArgumentOutOfRangeException>(act);
    }

    [Fact]
    public void Constructor_ShouldThrowArgumentOutOfRangeException_WhenBottomPo2IsZero()
    {
        // Arrange

        // Act
        Action act = () => CreateSettings(bottomPo2: Pressure.FromBar(0));

        // Assert
        Assert.Throws<ArgumentOutOfRangeException>(act);
    }

    [Fact]
    public void Constructor_ShouldThrowArgumentOutOfRangeException_WhenBottomPo2IsNegative()
    {
        // Arrange

        // Act
        Action act = () => CreateSettings(bottomPo2: Pressure.FromBar(-0.1));

        // Assert
        Assert.Throws<ArgumentOutOfRangeException>(act);
    }

    [Fact]
    public void Constructor_ShouldThrowArgumentOutOfRangeException_WhenDecoPo2IsZero()
    {
        // Arrange

        // Act
        Action act = () => CreateSettings(decoPo2: Pressure.FromBar(0));

        // Assert
        Assert.Throws<ArgumentOutOfRangeException>(act);
    }

    [Fact]
    public void Constructor_ShouldThrowArgumentOutOfRangeException_WhenDecoPo2IsNegative()
    {
        // Arrange

        // Act
        Action act = () => CreateSettings(decoPo2: Pressure.FromBar(-0.1));

        // Assert
        Assert.Throws<ArgumentOutOfRangeException>(act);
    }

    [Fact]
    public void Constructor_ShouldThrowArgumentOutOfRangeException_WhenReservePressureIsNegative()
    {
        // Arrange

        // Act
        Action act = () => CreateSettings(reservePressure: Pressure.FromBar(-1));

        // Assert
        Assert.Throws<ArgumentOutOfRangeException>(act);
    }

    [Fact]
    public void Constructor_ShouldSucceed_WhenReservePressureIsZero()
    {
        // Arrange

        // Act
        var settings = CreateSettings(reservePressure: Pressure.FromBar(0));

        // Assert
        Assert.Equal(0, settings.ReservePressure.InBar, Precision);
    }

    [Fact]
    public void Constructor_ShouldThrowArgumentOutOfRangeException_WhenReserveStressFactorIsLessThanOne()
    {
        // Arrange

        // Act
        Action act = () => CreateSettings(reserveStressFactor: 0.99);

        // Assert
        Assert.Throws<ArgumentOutOfRangeException>(act);
    }

    [Fact]
    public void Constructor_ShouldSucceed_WhenReserveStressFactorIsExactlyOne()
    {
        // Arrange

        // Act
        var settings = CreateSettings(reserveStressFactor: 1.0);

        // Assert
        Assert.Equal(1.0, settings.ReserveStressFactor, Precision);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Constructor_ShouldThrowArgumentOutOfRangeException_WhenReserveTeamSizeIsLessThanOne(int teamSize)
    {
        // Arrange

        // Act
        Action act = () => CreateSettings(reserveTeamSize: teamSize);

        // Assert
        Assert.Throws<ArgumentOutOfRangeException>(act);
    }

    [Fact]
    public void Constructor_ShouldSucceed_WhenReserveTeamSizeIsExactlyOne()
    {
        // Arrange

        // Act
        var settings = CreateSettings(reserveTeamSize: 1);

        // Assert
        Assert.Equal(1, settings.ReserveTeamSize);
    }

    [Fact]
    public void Constructor_ShouldThrowArgumentOutOfRangeException_WhenStopTimeIncrementIsZero()
    {
        // Arrange

        // Act
        Action act = () => CreateSettings(stopTimeIncrement: TimeSpan.Zero);

        // Assert
        Assert.Throws<ArgumentOutOfRangeException>(act);
    }

    [Fact]
    public void Constructor_ShouldThrowArgumentOutOfRangeException_WhenStopTimeIncrementIsNegative()
    {
        // Arrange

        // Act
        Action act = () => CreateSettings(stopTimeIncrement: TimeSpan.FromMinutes(-1));

        // Assert
        Assert.Throws<ArgumentOutOfRangeException>(act);
    }

    [Fact]
    public void Constructor_ShouldThrowArgumentOutOfRangeException_WhenProblemSolvingTimeIsNegative()
    {
        // Arrange

        // Act
        Action act = () => CreateSettings(problemSolvingTime: TimeSpan.FromSeconds(-1));

        // Assert
        Assert.Throws<ArgumentOutOfRangeException>(act);
    }

    [Fact]
    public void Constructor_ShouldSucceed_WhenProblemSolvingTimeIsZero()
    {
        // Arrange

        // Act
        var settings = CreateSettings(problemSolvingTime: TimeSpan.Zero);

        // Assert
        Assert.Equal(TimeSpan.Zero, settings.ProblemSolvingTime);
    }

    [Fact]
    public void Constructor_ShouldThrowArgumentOutOfRangeException_WhenMinimumGasSwitchDurationIsNegative()
    {
        // Arrange

        // Act
        Action act = () => CreateSettings(minimumGasSwitchDuration: TimeSpan.FromSeconds(-1));

        // Assert
        Assert.Throws<ArgumentOutOfRangeException>(act);
    }

    [Fact]
    public void Constructor_ShouldSucceed_WhenMinimumGasSwitchDurationIsZero()
    {
        // Arrange

        // Act
        var settings = CreateSettings(minimumGasSwitchDuration: TimeSpan.Zero);

        // Assert
        Assert.Equal(TimeSpan.Zero, settings.MinimumGasSwitchDuration);
    }

    [Fact]
    public void SurfacePressure_ShouldReturnConstructorValue_WhenSet()
    {
        // Arrange

        // Act
        var settings = CreateSettings();

        // Assert
        Assert.Equal(1, settings.SurfacePressure.InBar, Precision);
    }

    [Fact]
    public void Salinity_ShouldReturnConstructorValue_WhenSet()
    {
        // Arrange

        // Act
        var settings = CreateSettings();

        // Assert
        Assert.Equal(Salinity.Salt, settings.Salinity);
    }

    [Fact]
    public void AscentRateBelow75PercentMetersPerMinute_ShouldReturnConstructorValue_WhenSet()
    {
        // Arrange

        // Act
        var settings = CreateSettings(ascentRateBelow75PercentMetersPerMinute: 12);

        // Assert
        Assert.Equal(12, settings.AscentRateBelow75PercentMetersPerMinute, Precision);
    }

    [Fact]
    public void AscentRate75To50PercentMetersPerMinute_ShouldReturnConstructorValue_WhenSet()
    {
        // Arrange

        // Act
        var settings = CreateSettings(ascentRate75To50PercentMetersPerMinute: 11);

        // Assert
        Assert.Equal(11, settings.AscentRate75To50PercentMetersPerMinute, Precision);
    }

    [Fact]
    public void AscentRate50PercentToStopsMetersPerMinute_ShouldReturnConstructorValue_WhenSet()
    {
        // Arrange

        // Act
        var settings = CreateSettings(ascentRate50PercentToStopsMetersPerMinute: 7);

        // Assert
        Assert.Equal(7, settings.AscentRate50PercentToStopsMetersPerMinute, Precision);
    }

    [Fact]
    public void AscentRateLastSixMetersMetersPerMinute_ShouldReturnConstructorValue_WhenSet()
    {
        // Arrange

        // Act
        var settings = CreateSettings(ascentRateLastSixMetersMetersPerMinute: 3);

        // Assert
        Assert.Equal(3, settings.AscentRateLastSixMetersMetersPerMinute, Precision);
    }

    [Fact]
    public void BottomSacLitersPerMinute_ShouldReturnConstructorValue_WhenSet()
    {
        // Arrange

        // Act
        var settings = CreateSettings(bottomSacLitersPerMinute: 18);

        // Assert
        Assert.Equal(18, settings.BottomSacLitersPerMinute, Precision);
    }

    [Fact]
    public void DecoSacLitersPerMinute_ShouldReturnConstructorValue_WhenSet()
    {
        // Arrange

        // Act
        var settings = CreateSettings(decoSacLitersPerMinute: 14);

        // Assert
        Assert.Equal(14, settings.DecoSacLitersPerMinute, Precision);
    }

    [Fact]
    public void BottomPo2_ShouldReturnConstructorValue_WhenSet()
    {
        // Arrange

        // Act
        var settings = CreateSettings(bottomPo2: Pressure.FromBar(1.3));

        // Assert
        Assert.Equal(1.3, settings.BottomPo2.InBar, Precision);
    }

    [Fact]
    public void DecoPo2_ShouldReturnConstructorValue_WhenSet()
    {
        // Arrange

        // Act
        var settings = CreateSettings(decoPo2: Pressure.FromBar(1.5));

        // Assert
        Assert.Equal(1.5, settings.DecoPo2.InBar, Precision);
    }

    [Fact]
    public void ReservePressure_ShouldReturnConstructorValue_WhenSet()
    {
        // Arrange

        // Act
        var settings = CreateSettings(reservePressure: Pressure.FromBar(40));

        // Assert
        Assert.Equal(40, settings.ReservePressure.InBar, Precision);
    }

    [Fact]
    public void ReserveStressFactor_ShouldReturnConstructorValue_WhenSet()
    {
        // Arrange

        // Act
        var settings = CreateSettings(reserveStressFactor: 2.0);

        // Assert
        Assert.Equal(2.0, settings.ReserveStressFactor, Precision);
    }

    [Fact]
    public void ReserveTeamSize_ShouldReturnConstructorValue_WhenSet()
    {
        // Arrange

        // Act
        var settings = CreateSettings(reserveTeamSize: 3);

        // Assert
        Assert.Equal(3, settings.ReserveTeamSize);
    }

    [Fact]
    public void StopTimeIncrement_ShouldReturnConstructorValue_WhenSet()
    {
        // Arrange

        // Act
        var settings = CreateSettings(stopTimeIncrement: TimeSpan.FromMinutes(2));

        // Assert
        Assert.Equal(TimeSpan.FromMinutes(2), settings.StopTimeIncrement);
    }

    [Fact]
    public void ProblemSolvingTime_ShouldReturnConstructorValue_WhenSet()
    {
        // Arrange

        // Act
        var settings = CreateSettings(problemSolvingTime: TimeSpan.FromMinutes(5));

        // Assert
        Assert.Equal(TimeSpan.FromMinutes(5), settings.ProblemSolvingTime);
    }

    [Fact]
    public void MinimumGasSwitchDuration_ShouldReturnConstructorValue_WhenSet()
    {
        // Arrange

        // Act
        var settings = CreateSettings(minimumGasSwitchDuration: TimeSpan.FromSeconds(45));

        // Assert
        Assert.Equal(TimeSpan.FromSeconds(45), settings.MinimumGasSwitchDuration);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void SafetyStop_ShouldReturnConstructorValue_WhenSet(bool safetyStop)
    {
        // Arrange

        // Act
        var settings = CreateSettings(safetyStop: safetyStop);

        // Assert
        Assert.Equal(safetyStop, settings.SafetyStop);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void LastStopAtSixMeters_ShouldReturnConstructorValue_WhenSet(bool lastStopAtSixMeters)
    {
        // Arrange

        // Act
        var settings = CreateSettings(lastStopAtSixMeters: lastStopAtSixMeters);

        // Assert
        Assert.Equal(lastStopAtSixMeters, settings.LastStopAtSixMeters);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void SwitchAtRequiredStop_ShouldReturnConstructorValue_WhenSet(bool switchAtRequiredStop)
    {
        // Arrange

        // Act
        var settings = CreateSettings(switchAtRequiredStop: switchAtRequiredStop);

        // Assert
        Assert.Equal(switchAtRequiredStop, settings.SwitchAtRequiredStop);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void OxygenBreaks_ShouldReturnConstructorValue_WhenSet(bool oxygenBreaks)
    {
        // Arrange

        // Act
        var settings = CreateSettings(oxygenBreaks: oxygenBreaks);

        // Assert
        Assert.Equal(oxygenBreaks, settings.OxygenBreaks);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void OxygenIsNarcotic_ShouldReturnConstructorValue_WhenSet(bool oxygenIsNarcotic)
    {
        // Arrange

        // Act
        var settings = CreateSettings(oxygenIsNarcotic: oxygenIsNarcotic);

        // Assert
        Assert.Equal(oxygenIsNarcotic, settings.OxygenIsNarcotic);
    }
}