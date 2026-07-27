using Bitenovac.DecompressionAlgorithms.Core.Calculations;
using Bitenovac.DecompressionAlgorithms.Units;

namespace Bitenovac.DecompressionAlgorithms.Core.Unit.Tests;

public sealed class CylinderGasUsageTests
{
    private const int Precision = 5;

    [Fact]
    public void Cylinder_ShouldReturnConstructorValue_WhenSet()
    {
        // Arrange
        var cylinder = TestFactory.CreateCylinder(sizeLiter: 12, startPressureBar: 200);

        // Act
        var usage = new CylinderGasUsage(cylinder, Volume.FromLiter(1200), Pressure.FromBar(100));

        // Assert
        Assert.Equal(12, usage.Cylinder.Size.InLiter, Precision);
    }

    [Fact]
    public void GasUsed_ShouldReturnConstructorValue_WhenSet()
    {
        // Arrange
        var cylinder = TestFactory.CreateCylinder();

        // Act
        var usage = new CylinderGasUsage(cylinder, Volume.FromLiter(1200), Pressure.FromBar(100));

        // Assert
        Assert.Equal(1200, usage.GasUsed.InLiter, Precision);
    }

    [Fact]
    public void EndPressure_ShouldReturnConstructorValue_WhenSet()
    {
        // Arrange
        var cylinder = TestFactory.CreateCylinder();

        // Act
        var usage = new CylinderGasUsage(cylinder, Volume.FromLiter(1200), Pressure.FromBar(100));

        // Assert
        Assert.Equal(100, usage.EndPressure.InBar, Precision);
    }

    [Fact]
    public void StartPressure_ShouldReturnTheCylinderStartPressure_WhenQueried()
    {
        // Arrange
        var cylinder = TestFactory.CreateCylinder(startPressureBar: 232);

        // Act
        var usage = new CylinderGasUsage(cylinder, Volume.FromLiter(100), Pressure.FromBar(100));

        // Assert
        Assert.Equal(232, usage.StartPressure.InBar, Precision);
    }

    [Fact]
    public void Constructor_ShouldSucceed_WhenGasUsedIsZero()
    {
        // Arrange
        var cylinder = TestFactory.CreateCylinder(startPressureBar: 200);

        // Act
        var usage = new CylinderGasUsage(cylinder, Volume.Zero, Pressure.FromBar(200));

        // Assert
        Assert.Equal(0, usage.GasUsed.InLiter, Precision);
    }

    [Fact]
    public void Constructor_ShouldSucceed_WhenEndPressureEqualsTheStartPressure()
    {
        // Arrange
        var cylinder = TestFactory.CreateCylinder(startPressureBar: 200);

        // Act
        var usage = new CylinderGasUsage(cylinder, Volume.Zero, Pressure.FromBar(200));

        // Assert
        Assert.Equal(200, usage.EndPressure.InBar, Precision);
    }

    [Fact]
    public void Constructor_ShouldThrowArgumentOutOfRangeException_WhenGasUsedIsNegative()
    {
        // Arrange
        var cylinder = TestFactory.CreateCylinder();

        // Act
        Action act = () => new CylinderGasUsage(cylinder, Volume.FromLiter(-1), Pressure.FromBar(100));

        // Assert
        Assert.Throws<ArgumentOutOfRangeException>(act);
    }

    [Fact]
    public void Constructor_ShouldThrowArgumentOutOfRangeException_WhenEndPressureIsNegative()
    {
        // Arrange
        var cylinder = TestFactory.CreateCylinder();

        // Act
        Action act = () => new CylinderGasUsage(cylinder, Volume.FromLiter(100), Pressure.FromBar(-1));

        // Assert
        Assert.Throws<ArgumentOutOfRangeException>(act);
    }

    [Fact]
    public void Constructor_ShouldThrowArgumentOutOfRangeException_WhenEndPressureExceedsTheStartPressure()
    {
        // Arrange
        var cylinder = TestFactory.CreateCylinder(startPressureBar: 200);

        // Act
        Action act = () => new CylinderGasUsage(cylinder, Volume.FromLiter(100), Pressure.FromBar(201));

        // Assert
        Assert.Throws<ArgumentOutOfRangeException>(act);
    }

    [Fact]
    public void IsExhausted_ShouldReturnTrue_WhenTheEndPressureIsZero()
    {
        // Arrange
        var cylinder = TestFactory.CreateCylinder(startPressureBar: 200);

        // Act
        var usage = new CylinderGasUsage(cylinder, Volume.FromLiter(2400), Pressure.Zero);

        // Assert
        Assert.True(usage.IsExhausted);
    }

    [Fact]
    public void IsExhausted_ShouldReturnFalse_WhenGasRemainsInTheCylinder()
    {
        // Arrange
        var cylinder = TestFactory.CreateCylinder(startPressureBar: 200);

        // Act
        var usage = new CylinderGasUsage(cylinder, Volume.FromLiter(2400), Pressure.FromBar(0.5));

        // Assert
        Assert.False(usage.IsExhausted);
    }
}