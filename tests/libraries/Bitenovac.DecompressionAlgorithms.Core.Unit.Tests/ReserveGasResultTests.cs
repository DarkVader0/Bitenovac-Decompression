using Bitenovac.DecompressionAlgorithms.Core.Calculations;
using Bitenovac.DecompressionAlgorithms.Units;

namespace Bitenovac.DecompressionAlgorithms.Core.Unit.Tests;

public sealed class ReserveGasResultTests
{
    private const int Precision = 5;

    private static CylinderReserveStatus CreateStatus(double requiredLiter,
        double remainingLiter) =>
        new(TestFactory.CreateCylinder(), Volume.FromLiter(requiredLiter), Volume.FromLiter(remainingLiter));

    [Fact]
    public void Constructor_ShouldThrowArgumentNullException_WhenCylinderStatusesIsNull()
    {
        // Arrange
        IEnumerable<CylinderReserveStatus> statuses = null!;

        // Act
        Action act = () => new ReserveGasResult(statuses);

        // Assert
        Assert.Throws<ArgumentNullException>(act);
    }

    [Fact]
    public void Constructor_ShouldThrowArgumentException_WhenCylinderStatusesContainsANullEntry()
    {
        // Arrange
        var statuses = new[] { CreateStatus(500, 800), null! };

        // Act
        Action act = () => new ReserveGasResult(statuses);

        // Assert
        Assert.Throws<ArgumentException>(act);
    }

    [Fact]
    public void Constructor_ShouldSucceed_WhenCylinderStatusesIsEmpty()
    {
        // Arrange
        var statuses = Array.Empty<CylinderReserveStatus>();

        // Act
        var result = new ReserveGasResult(statuses);

        // Assert
        Assert.Empty(result.CylinderStatuses);
    }

    [Fact]
    public void CylinderStatuses_ShouldReturnSuppliedStatuses_InOrder()
    {
        // Arrange
        var first = CreateStatus(500, 800);
        var second = CreateStatus(300, 400);

        // Act
        var result = new ReserveGasResult([first, second]);

        // Assert
        Assert.Equal(500, result.CylinderStatuses[0].RequiredReserve.InLiter, Precision);
        Assert.Equal(300, result.CylinderStatuses[1].RequiredReserve.InLiter, Precision);
    }

    [Fact]
    public void CylinderStatuses_ShouldNotReflectChanges_WhenSourceListIsModifiedAfterConstruction()
    {
        // Arrange
        var statuses = new List<CylinderReserveStatus> { CreateStatus(500, 800) };
        var result = new ReserveGasResult(statuses);

        // Act
        statuses.Add(CreateStatus(300, 400));

        // Assert
        Assert.Single(result.CylinderStatuses);
    }

    [Fact]
    public void AllSatisfied_ShouldReturnTrue_WhenEveryCylinderMeetsItsReserve()
    {
        // Arrange
        var statuses = new[] { CreateStatus(500, 800), CreateStatus(300, 300) };

        // Act
        var result = new ReserveGasResult(statuses);

        // Assert
        Assert.True(result.AllSatisfied);
    }

    [Fact]
    public void AllSatisfied_ShouldReturnFalse_WhenAnyCylinderFallsShortOfItsReserve()
    {
        // Arrange
        var statuses = new[] { CreateStatus(500, 800), CreateStatus(300, 299) };

        // Act
        var result = new ReserveGasResult(statuses);

        // Assert
        Assert.False(result.AllSatisfied);
    }

    [Fact]
    public void AllSatisfied_ShouldReturnTrue_WhenThereAreNoCylinderStatuses()
    {
        // Arrange
        var statuses = Array.Empty<CylinderReserveStatus>();

        // Act
        var result = new ReserveGasResult(statuses);

        // Assert
        Assert.True(result.AllSatisfied);
    }
}