using Bitenovac.DecompressionAlgorithms.Core.Calculations;
using Bitenovac.DecompressionAlgorithms.Core.Environment;
using Bitenovac.DecompressionAlgorithms.Core.Planning;
using Bitenovac.DecompressionAlgorithms.Units;

namespace Bitenovac.DecompressionAlgorithms.Core.Unit.Tests;

public sealed class AmbientConditionsTests
{
    private const int Precision = 5;

    [Fact]
    public void PressureAtDepth_ShouldThrowArgumentNullException_WhenSettingsIsNull()
    {
        // Arrange
        DivePlanSettings settings = null!;

        // Act
        Action act = () => AmbientConditions.PressureAtDepth(settings, Depth.FromMeter(10));

        // Assert
        Assert.Throws<ArgumentNullException>(act);
    }

    [Fact]
    public void DepthAtPressure_ShouldThrowArgumentNullException_WhenSettingsIsNull()
    {
        // Arrange
        DivePlanSettings settings = null!;

        // Act
        Action act = () => AmbientConditions.DepthAtPressure(settings, Pressure.FromBar(2));

        // Assert
        Assert.Throws<ArgumentNullException>(act);
    }

    [Fact]
    public void PressureAtDepth_ShouldReturnSurfacePressure_WhenDepthIsZero()
    {
        // Arrange
        var settings = TestFactory.CreateSettings();

        // Act
        var ambient = AmbientConditions.PressureAtDepth(settings, Depth.Zero);

        // Assert
        Assert.Equal(settings.SurfacePressure.InMillibar, ambient.InMillibar, Precision);
    }

    [Fact]
    public void PressureAtDepth_ShouldAddHydrostaticColumn_WhenDepthIsPositive()
    {
        // Arrange
        var settings = TestFactory.CreateSettings(salinity: Salinity.Fresh);
        const double expectedMillibar = 1000.0 + 1000.0 * 9.80665 * 10.0 / 100.0;

        // Act
        var ambient = AmbientConditions.PressureAtDepth(settings, Depth.FromMeter(10));

        // Assert
        Assert.Equal(expectedMillibar, ambient.InMillibar, Precision);
    }

    [Fact]
    public void DepthAtPressure_ShouldInvertPressureAtDepth_WhenPressureIsBelowSurface()
    {
        // Arrange
        var settings = TestFactory.CreateSettings(salinity: Salinity.Salt);
        var ambient = AmbientConditions.PressureAtDepth(settings, Depth.FromMeter(23.5));

        // Act
        var depth = AmbientConditions.DepthAtPressure(settings, ambient);

        // Assert
        Assert.Equal(23.5, depth.InMeter, Precision);
    }

    [Fact]
    public void DepthAtPressure_ShouldReturnZero_WhenPressureIsAtOrBelowSurfacePressure()
    {
        // Arrange
        var settings = TestFactory.CreateSettings();

        // Act
        var depth = AmbientConditions.DepthAtPressure(settings, Pressure.FromBar(0.9));

        // Assert
        Assert.Equal(0.0, depth.InMeter, Precision);
    }
}