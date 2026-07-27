using Bitenovac.DecompressionAlgorithms.Core.Calculations;
using Bitenovac.DecompressionAlgorithms.Units;

namespace Bitenovac.DecompressionAlgorithms.Core.Unit.Tests;

public sealed class CylinderReserveStatusTests
{
    private const int Precision = 5;

    [Fact]
    public void Cylinder_ShouldReturnConstructorValue_WhenSet()
    {
        // Arrange
        var cylinder = TestFactory.CreateCylinder(sizeLiter: 12);

        // Act
        var status = new CylinderReserveStatus(cylinder, Volume.FromLiter(500), Volume.FromLiter(800));

        // Assert
        Assert.Equal(12, status.Cylinder.Size.InLiter, Precision);
    }

    [Fact]
    public void RequiredReserve_ShouldReturnConstructorValue_WhenSet()
    {
        // Arrange
        var cylinder = TestFactory.CreateCylinder();

        // Act
        var status = new CylinderReserveStatus(cylinder, Volume.FromLiter(500), Volume.FromLiter(800));

        // Assert
        Assert.Equal(500, status.RequiredReserve.InLiter, Precision);
    }

    [Fact]
    public void ProjectedRemaining_ShouldReturnConstructorValue_WhenSet()
    {
        // Arrange
        var cylinder = TestFactory.CreateCylinder();

        // Act
        var status = new CylinderReserveStatus(cylinder, Volume.FromLiter(500), Volume.FromLiter(800));

        // Assert
        Assert.Equal(800, status.ProjectedRemaining.InLiter, Precision);
    }

    [Fact]
    public void Constructor_ShouldSucceed_WhenBothVolumesAreZero()
    {
        // Arrange
        var cylinder = TestFactory.CreateCylinder();

        // Act
        var status = new CylinderReserveStatus(cylinder, Volume.Zero, Volume.Zero);

        // Assert
        Assert.Equal(0, status.RequiredReserve.InLiter, Precision);
    }

    [Fact]
    public void Constructor_ShouldThrowArgumentOutOfRangeException_WhenRequiredReserveIsNegative()
    {
        // Arrange
        var cylinder = TestFactory.CreateCylinder();

        // Act
        Action act = () => new CylinderReserveStatus(cylinder, Volume.FromLiter(-1), Volume.FromLiter(800));

        // Assert
        Assert.Throws<ArgumentOutOfRangeException>(act);
    }

    [Fact]
    public void Constructor_ShouldThrowArgumentOutOfRangeException_WhenProjectedRemainingIsNegative()
    {
        // Arrange
        var cylinder = TestFactory.CreateCylinder();

        // Act
        Action act = () => new CylinderReserveStatus(cylinder, Volume.FromLiter(500), Volume.FromLiter(-1));

        // Assert
        Assert.Throws<ArgumentOutOfRangeException>(act);
    }

    [Fact]
    public void IsSatisfied_ShouldReturnTrue_WhenTheProjectedRemainingExceedsTheRequiredReserve()
    {
        // Arrange
        var cylinder = TestFactory.CreateCylinder();

        // Act
        var status = new CylinderReserveStatus(cylinder, Volume.FromLiter(500), Volume.FromLiter(800));

        // Assert
        Assert.True(status.IsSatisfied);
    }

    [Fact]
    public void IsSatisfied_ShouldReturnTrue_WhenTheProjectedRemainingEqualsTheRequiredReserve()
    {
        // Arrange
        var cylinder = TestFactory.CreateCylinder();

        // Act
        var status = new CylinderReserveStatus(cylinder, Volume.FromLiter(500), Volume.FromLiter(500));

        // Assert
        Assert.True(status.IsSatisfied);
    }

    [Fact]
    public void IsSatisfied_ShouldReturnFalse_WhenTheProjectedRemainingFallsShortOfTheRequiredReserve()
    {
        // Arrange
        var cylinder = TestFactory.CreateCylinder();

        // Act
        var status = new CylinderReserveStatus(cylinder, Volume.FromLiter(500), Volume.FromLiter(499));

        // Assert
        Assert.False(status.IsSatisfied);
    }
}
