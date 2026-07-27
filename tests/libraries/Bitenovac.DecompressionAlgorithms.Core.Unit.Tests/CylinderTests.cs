using Bitenovac.DecompressionAlgorithms.Core.Equipment;
using Bitenovac.DecompressionAlgorithms.Core.Planning;
using Bitenovac.DecompressionAlgorithms.Units;

namespace Bitenovac.DecompressionAlgorithms.Core.Unit.Tests;

public sealed class CylinderTests
{
    private const int Precision = 5;

    [Fact]
    public void Gas_ShouldReturnConstructorValue_WhenSet()
    {
        // Arrange
        var gas = GasMixture.FromPercent(32, 0);

        // Act
        var cylinder = new Cylinder(gas, Volume.FromLiter(12), Pressure.FromBar(200), CylinderPurpose.BottomGas);

        // Assert
        Assert.Equal(gas, cylinder.Gas);
    }

    [Fact]
    public void Size_ShouldReturnConstructorValue_WhenSet()
    {
        // Arrange
        var size = Volume.FromLiter(12);

        // Act
        var cylinder = new Cylinder(GasMixture.Air, size, Pressure.FromBar(200), CylinderPurpose.BottomGas);

        // Assert
        Assert.Equal(12, cylinder.Size.InLiter, Precision);
    }

    [Fact]
    public void StartPressure_ShouldReturnConstructorValue_WhenSet()
    {
        // Arrange
        var startPressure = Pressure.FromBar(200);

        // Act
        var cylinder = new Cylinder(GasMixture.Air, Volume.FromLiter(12), startPressure, CylinderPurpose.BottomGas);

        // Assert
        Assert.Equal(200, cylinder.StartPressure.InBar, Precision);
    }

    [Theory]
    [InlineData(CylinderPurpose.BottomGas)]
    [InlineData(CylinderPurpose.DecoGas)]
    [InlineData(CylinderPurpose.Diluent)]
    [InlineData(CylinderPurpose.Bailout)]
    public void Purpose_ShouldReturnConstructorValue_WhenSet(CylinderPurpose purpose)
    {
        // Arrange
        var size = Volume.FromLiter(12);

        // Act
        var cylinder = new Cylinder(GasMixture.Air, size, Pressure.FromBar(200), purpose);

        // Assert
        Assert.Equal(purpose, cylinder.Purpose);
    }

    [Fact]
    public void StartGasVolume_ShouldReturnSizeScaledByThePressureRatio_WhenSurfacePressureIsOneBar()
    {
        // Arrange
        var cylinder = TestFactory.CreateCylinder(sizeLiter: 12, startPressureBar: 200);

        // Act
        var volume = cylinder.StartGasVolume(Pressure.FromBar(1));

        // Assert
        Assert.Equal(2400, volume.InLiter, Precision);
    }

    [Fact]
    public void StartGasVolume_ShouldReturnHalfTheVolume_WhenSurfacePressureIsDoubled()
    {
        // Arrange
        var cylinder = TestFactory.CreateCylinder(sizeLiter: 12, startPressureBar: 200);

        // Act
        var volume = cylinder.StartGasVolume(Pressure.FromBar(2));

        // Assert
        Assert.Equal(1200, volume.InLiter, Precision);
    }

    [Fact]
    public void StartGasVolume_ShouldReturnTheSameVolume_WhenTheSameRatioIsExpressedInDifferentUnits()
    {
        // Arrange
        var cylinder = TestFactory.CreateCylinder(sizeLiter: 12, startPressureBar: 200);

        // Act
        var fromBar = cylinder.StartGasVolume(Pressure.FromBar(1));
        var fromMillibar = cylinder.StartGasVolume(Pressure.FromMillibar(1000));

        // Assert
        Assert.Equal(fromBar.InLiter, fromMillibar.InLiter, Precision);
    }

    [Fact]
    public void StartGasVolume_ShouldReturnZero_WhenTheCylinderIsEmpty()
    {
        // Arrange
        var cylinder = TestFactory.CreateCylinder(sizeLiter: 12, startPressureBar: 0);

        // Act
        var volume = cylinder.StartGasVolume(Pressure.FromBar(1));

        // Assert
        Assert.Equal(0, volume.InLiter, Precision);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void StartGasVolume_ShouldThrowArgumentOutOfRangeException_WhenSurfacePressureIsNotPositive(double bar)
    {
        // Arrange
        var cylinder = TestFactory.CreateCylinder();

        // Act
        Action act = () => cylinder.StartGasVolume(Pressure.FromBar(bar));

        // Assert
        Assert.Throws<ArgumentOutOfRangeException>(act);
    }

    [Fact]
    public void Default_ShouldReturnZeroSizeAndZeroStartPressure_WhenNotConstructed()
    {
        // Arrange
        var cylinder = default(Cylinder);

        // Act
        var sizeLiter = cylinder.Size.InLiter;

        // Assert
        Assert.Equal(0, sizeLiter, Precision);
        Assert.Equal(0, cylinder.StartPressure.InBar, Precision);
    }
}
