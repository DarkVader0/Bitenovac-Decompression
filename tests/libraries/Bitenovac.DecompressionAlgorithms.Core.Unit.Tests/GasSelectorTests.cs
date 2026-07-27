using Bitenovac.DecompressionAlgorithms.Core.Calculations;
using Bitenovac.DecompressionAlgorithms.Core.Equipment;
using Bitenovac.DecompressionAlgorithms.Core.Planning;
using Bitenovac.DecompressionAlgorithms.Units;

namespace Bitenovac.DecompressionAlgorithms.Core.Unit.Tests;

public sealed class GasSelectorTests
{
    private const int Precision = 5;

    [Fact]
    public void SelectRichestGas_ShouldThrowArgumentNullException_WhenCylindersIsNull()
    {
        // Arrange
        IReadOnlyList<Cylinder> cylinders = null!;

        // Act
        Action act = () => GasSelector.SelectRichestGas(cylinders, Pressure.FromBar(1), Pressure.FromBar(1.4));

        // Assert
        Assert.Throws<ArgumentNullException>(act);
    }

    [Fact]
    public void SelectRichestGas_ShouldThrowArgumentException_WhenCylindersIsEmpty()
    {
        // Arrange
        var cylinders = Array.Empty<Cylinder>();

        // Act
        Action act = () => GasSelector.SelectRichestGas(cylinders, Pressure.FromBar(1), Pressure.FromBar(1.4));

        // Assert
        Assert.Throws<ArgumentException>(act);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void SelectRichestGas_ShouldThrowArgumentOutOfRangeException_WhenMaxPo2IsNotPositive(double bar)
    {
        // Arrange
        var cylinders = new[] { TestFactory.CreateCylinder() };

        // Act
        Action act = () => GasSelector.SelectRichestGas(cylinders, Pressure.FromBar(1), Pressure.FromBar(bar));

        // Assert
        Assert.Throws<ArgumentOutOfRangeException>(act);
    }

    [Fact]
    public void SelectRichestGas_ShouldThrowInvalidOperationException_WhenNoGasIsBreathableAtTheDepth()
    {
        // Arrange
        var cylinders = new[] { TestFactory.CreateCylinder(GasMixture.Oxygen) };

        // Act
        Action act = () => GasSelector.SelectRichestGas(cylinders, Pressure.FromBar(10), Pressure.FromBar(1.4));

        // Assert
        Assert.Throws<InvalidOperationException>(act);
    }

    [Fact]
    public void SelectRichestGas_ShouldReturnTheOnlyCylinder_WhenASingleBreathableGasIsAvailable()
    {
        // Arrange
        var cylinders = new[] { TestFactory.CreateCylinder(GasMixture.Air) };

        // Act
        var selected = GasSelector.SelectRichestGas(cylinders, Pressure.FromBar(4), Pressure.FromBar(1.4));

        // Assert
        Assert.Equal(GasMixture.Air, selected.Gas);
    }

    [Fact]
    public void SelectRichestGas_ShouldReturnTheHighestOxygenContent_WhenEveryGasIsBreathable()
    {
        // Arrange
        var cylinders = new[]
        {
            TestFactory.CreateCylinder(GasMixture.Air),
            TestFactory.CreateCylinder(GasMixture.FromPercent(50, 0)),
            TestFactory.CreateCylinder(GasMixture.Oxygen)
        };

        // Act
        var selected = GasSelector.SelectRichestGas(cylinders, Pressure.FromBar(1), Pressure.FromBar(1.6));

        // Assert
        Assert.Equal(GasMixture.Oxygen, selected.Gas);
    }

    [Fact]
    public void SelectRichestGas_ShouldExcludeGasesBeyondTheLimit_WhenTheAmbientPressureIsHigh()
    {
        // Arrange
        var cylinders = new[]
        {
            TestFactory.CreateCylinder(GasMixture.Air),
            TestFactory.CreateCylinder(GasMixture.FromPercent(50, 0)),
            TestFactory.CreateCylinder(GasMixture.Oxygen)
        };

        // Act
        var selected = GasSelector.SelectRichestGas(cylinders, Pressure.FromBar(4), Pressure.FromBar(1.4));

        // Assert
        Assert.Equal(GasMixture.Air, selected.Gas);
    }

    [Fact]
    public void SelectRichestGas_ShouldIncludeTheGas_WhenItsPartialPressureExactlyEqualsTheLimit()
    {
        // Arrange
        var cylinders = new[] { TestFactory.CreateCylinder(GasMixture.Oxygen) };

        // Act
        var selected = GasSelector.SelectRichestGas(cylinders, Pressure.FromBar(1.4), Pressure.FromBar(1.4));

        // Assert
        Assert.Equal(GasMixture.Oxygen, selected.Gas);
    }

    [Fact]
    public void SelectRichestGas_ShouldPreferTheHigherHeliumContent_WhenTwoGasesShareTheSameOxygenContent()
    {
        // Arrange
        var cylinders = new[]
        {
            TestFactory.CreateCylinder(GasMixture.FromPercent(21, 0)),
            TestFactory.CreateCylinder(GasMixture.FromPercent(21, 35))
        };

        // Act
        var selected = GasSelector.SelectRichestGas(cylinders, Pressure.FromBar(4), Pressure.FromBar(1.4));

        // Assert
        Assert.Equal(35, selected.Gas.PercentHe, Precision);
    }

    [Fact]
    public void SelectRichestGas_ShouldReturnTheFirstMatch_WhenTwoCylindersHoldTheIdenticalGas()
    {
        // Arrange
        var cylinders = new[]
        {
            TestFactory.CreateCylinder(GasMixture.Air, startPressureBar: 200),
            TestFactory.CreateCylinder(GasMixture.Air, startPressureBar: 100)
        };

        // Act
        var selected = GasSelector.SelectRichestGas(cylinders, Pressure.FromBar(4), Pressure.FromBar(1.4));

        // Assert
        Assert.Equal(200, selected.StartPressure.InBar, Precision);
    }

    [Fact]
    public void MaxOperatingPressure_ShouldReturnTheLimitDividedByTheOxygenFraction_WhenTheGasIsAir()
    {
        // Arrange
        var gas = GasMixture.Air;

        // Act
        var maxOperating = GasSelector.MaxOperatingPressure(gas, Pressure.FromMillibar(1400));

        // Assert
        Assert.Equal(6666.66667, maxOperating.InMillibar, Precision);
    }

    [Fact]
    public void MaxOperatingPressure_ShouldReturnTheLimitItself_WhenTheGasIsPureOxygen()
    {
        // Arrange
        var gas = GasMixture.Oxygen;

        // Act
        var maxOperating = GasSelector.MaxOperatingPressure(gas, Pressure.FromMillibar(1600));

        // Assert
        Assert.Equal(1600, maxOperating.InMillibar, Precision);
    }

    [Fact]
    public void MaxOperatingPressure_ShouldReturnAShallowerPressure_WhenTheGasIsRicher()
    {
        // Arrange
        var forAir = GasSelector.MaxOperatingPressure(GasMixture.Air, Pressure.FromMillibar(1400));

        // Act
        var forNitrox = GasSelector.MaxOperatingPressure(GasMixture.FromPercent(50, 0), Pressure.FromMillibar(1400));

        // Assert
        Assert.True(forNitrox.InMillibar < forAir.InMillibar);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void MaxOperatingPressure_ShouldThrowArgumentOutOfRangeException_WhenMaxPo2IsNotPositive(double mbar)
    {
        // Arrange
        var gas = GasMixture.Air;

        // Act
        Action act = () => GasSelector.MaxOperatingPressure(gas, Pressure.FromMillibar(mbar));

        // Assert
        Assert.Throws<ArgumentOutOfRangeException>(act);
    }

    [Fact]
    public void MaxOperatingPressure_ShouldThrowArgumentOutOfRangeException_WhenTheGasContainsNoOxygen()
    {
        // Arrange
        var gas = default(GasMixture);

        // Act
        Action act = () => GasSelector.MaxOperatingPressure(gas, Pressure.FromMillibar(1400));

        // Assert
        Assert.Throws<ArgumentOutOfRangeException>(act);
    }
}