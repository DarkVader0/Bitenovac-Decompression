using Bitenovac.DecompressionAlgorithms.Core.Calculations;
using Bitenovac.DecompressionAlgorithms.Core.Environment;
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
    public void SelectRichestGas_ShouldKeepTheRicherGas_WhenAPoorerCylinderFollowsIt()
    {
        // Arrange
        var cylinders = new[]
        {
            TestFactory.CreateCylinder(GasMixture.FromPercent(50, 0)),
            TestFactory.CreateCylinder(GasMixture.Air)
        };

        // Act
        var selected = GasSelector.SelectRichestGas(cylinders, Pressure.FromBar(1), Pressure.FromBar(1.6));

        // Assert
        Assert.Equal(GasMixture.FromPercent(50, 0), selected.Gas);
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
    public void SelectRichestGas_ShouldRestrictTheChoiceToTheRequiredPurpose_WhenOneIsSupplied()
    {
        // Arrange
        var cylinders = new[]
        {
            TestFactory.CreateCylinder(GasMixture.FromPercent(50, 0), purpose: CylinderPurpose.DecoGas),
            TestFactory.CreateCylinder(GasMixture.Air, purpose: CylinderPurpose.Diluent)
        };

        // Act
        var selected = GasSelector.SelectRichestGas(cylinders, Pressure.FromBar(1), Pressure.FromBar(1.6),
            CylinderPurpose.Diluent);

        // Assert
        Assert.Equal(GasMixture.Air, selected.Gas);
    }

    [Fact]
    public void SelectRichestGas_ShouldSkipTheOxygenSupply_WhenNoPurposeIsRequired()
    {
        // Arrange
        var cylinders = new[]
        {
            TestFactory.CreateCylinder(GasMixture.Air),
            TestFactory.CreateCylinder(GasMixture.FromPercent(50, 0), purpose: CylinderPurpose.Oxygen)
        };

        // Act
        var selected = GasSelector.SelectRichestGas(cylinders, Pressure.FromBar(1), Pressure.FromBar(1.6));

        // Assert
        Assert.Equal(GasMixture.Air, selected.Gas);
    }

    [Fact]
    public void IsAvailableFor_ShouldMatchThePurposeExactly_WhenOneIsRequired()
    {
        // Arrange
        var diluent = TestFactory.CreateCylinder(GasMixture.Air, purpose: CylinderPurpose.Diluent);
        var stage = TestFactory.CreateCylinder(GasMixture.FromPercent(50, 0), purpose: CylinderPurpose.DecoGas);

        // Act
        var diluentAvailable = GasSelector.IsAvailableFor(diluent, CylinderPurpose.Diluent);
        var stageAvailable = GasSelector.IsAvailableFor(stage, CylinderPurpose.Diluent);

        // Assert
        Assert.True(diluentAvailable);
        Assert.False(stageAvailable);
    }

    [Fact]
    public void IsAvailableFor_ShouldExcludeOnlyTheOxygenSupply_WhenNoPurposeIsRequired()
    {
        // Arrange
        var bottomGas = TestFactory.CreateCylinder(GasMixture.Air);
        var oxygenSupply = TestFactory.CreateCylinder(GasMixture.Oxygen, purpose: CylinderPurpose.Oxygen);

        // Act
        var bottomGasAvailable = GasSelector.IsAvailableFor(bottomGas, null);
        var oxygenSupplyAvailable = GasSelector.IsAvailableFor(oxygenSupply, null);

        // Assert
        Assert.True(bottomGasAvailable);
        Assert.False(oxygenSupplyAvailable);
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

    [Fact]
    public void MaxOperatingDepthMeter_ShouldReturnTheDepthAtWhichTheLimitIsReached_WhenTheModelIsRealistic()
    {
        // Arrange
        const double MillibarPerMeter = 1000.0 * 9.80665 / 100.0;
        var expected = (1600.0 - 1000.0) / MillibarPerMeter;
        var settings = TestFactory.CreateSettings(
            maximumOperatingDepthModel: MaximumOperatingDepthModel.Realistic);

        // Act
        var depthMeter = GasSelector.MaxOperatingDepthMeter(GasMixture.Oxygen, Pressure.FromBar(1.6), settings);

        // Assert
        Assert.Equal(expected, depthMeter, Precision);
    }

    [Fact]
    public void MaxOperatingDepthMeter_ShouldReturnExactlySixMeters_WhenTheModelIsSimplifiedAndTheGasIsOxygen()
    {
        // Arrange
        var settings = TestFactory.CreateSettings(
            maximumOperatingDepthModel: MaximumOperatingDepthModel.Simplified);

        // Act
        var depthMeter = GasSelector.MaxOperatingDepthMeter(GasMixture.Oxygen, Pressure.FromBar(1.6), settings);

        // Assert
        Assert.Equal(6.0, depthMeter, Precision);
    }

    [Fact]
    public void
        MaxOperatingDepthMeter_ShouldReturnExactlyTwentyTwoMeters_WhenTheModelIsSimplifiedAndTheGasIsNitroxFifty()
    {
        // Arrange
        var settings = TestFactory.CreateSettings(
            maximumOperatingDepthModel: MaximumOperatingDepthModel.Simplified);

        // Act
        var depthMeter = GasSelector.MaxOperatingDepthMeter(GasMixture.FromPercent(50, 0), Pressure.FromBar(1.6),
            settings);

        // Assert
        Assert.Equal(22.0, depthMeter, Precision);
    }

    [Fact]
    public void MaxOperatingDepthMeter_ShouldReturnTheTextbookDepth_WhenTheModelIsSimplifiedAndTheGasIsAir()
    {
        // Arrange
        var settings = TestFactory.CreateSettings(
            maximumOperatingDepthModel: MaximumOperatingDepthModel.Simplified);

        // Act
        var depthMeter = GasSelector.MaxOperatingDepthMeter(GasMixture.Air, Pressure.FromBar(1.4), settings);

        // Assert
        Assert.Equal(56.66667, depthMeter, Precision);
    }

    [Fact]
    public void MaxOperatingDepthMeter_ShouldIgnoreTheEnvironment_WhenTheModelIsSimplified()
    {
        // Arrange
        var atSeaLevelInFreshWater = TestFactory.CreateSettings(
            Pressure.FromMillibar(1000),
            Salinity.Fresh,
            maximumOperatingDepthModel: MaximumOperatingDepthModel.Simplified);
        var atAltitudeInSaltWater = TestFactory.CreateSettings(
            Pressure.FromMillibar(800),
            Salinity.Salt,
            maximumOperatingDepthModel: MaximumOperatingDepthModel.Simplified);

        // Act
        var fresh = GasSelector.MaxOperatingDepthMeter(GasMixture.Oxygen, Pressure.FromBar(1.6),
            atSeaLevelInFreshWater);
        var salt = GasSelector.MaxOperatingDepthMeter(GasMixture.Oxygen, Pressure.FromBar(1.6), atAltitudeInSaltWater);

        // Assert
        Assert.Equal(6.0, fresh, Precision);
        Assert.Equal(6.0, salt, Precision);
    }

    [Fact]
    public void MaxOperatingDepthMeter_ShouldReturnAShallowerDepthInSaltWater_WhenTheModelIsRealistic()
    {
        // Arrange
        var freshSettings = TestFactory.CreateSettings(salinity: Salinity.Fresh,
            maximumOperatingDepthModel: MaximumOperatingDepthModel.Realistic);
        var saltSettings = TestFactory.CreateSettings(salinity: Salinity.Salt,
            maximumOperatingDepthModel: MaximumOperatingDepthModel.Realistic);

        // Act
        var fresh = GasSelector.MaxOperatingDepthMeter(GasMixture.Oxygen, Pressure.FromBar(1.6), freshSettings);
        var salt = GasSelector.MaxOperatingDepthMeter(GasMixture.Oxygen, Pressure.FromBar(1.6), saltSettings);

        // Assert
        Assert.True(salt < fresh);
    }

    [Theory]
    [InlineData(MaximumOperatingDepthModel.Realistic)]
    [InlineData(MaximumOperatingDepthModel.Simplified)]
    public void MaxOperatingDepthMeter_ShouldReturnZero_WhenTheLimitIsReachedAtTheSurface(
        MaximumOperatingDepthModel model)
    {
        // Arrange
        var settings = TestFactory.CreateSettings(maximumOperatingDepthModel: model);

        // Act
        var depthMeter = GasSelector.MaxOperatingDepthMeter(GasMixture.Oxygen, Pressure.FromBar(0.5), settings);

        // Assert
        Assert.Equal(0.0, depthMeter, Precision);
    }

    [Fact]
    public void MaxOperatingDepthMeter_ShouldThrowArgumentNullException_WhenSettingsIsNull()
    {
        // Arrange
        DivePlanSettings settings = null!;

        // Act
        Action act = () => GasSelector.MaxOperatingDepthMeter(GasMixture.Oxygen, Pressure.FromBar(1.6), settings);

        // Assert
        Assert.Throws<ArgumentNullException>(act);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void MaxOperatingDepthMeter_ShouldThrowArgumentOutOfRangeException_WhenMaxPo2IsNotPositive(double bar)
    {
        // Arrange
        var settings = TestFactory.CreateSettings();

        // Act
        Action act = () => GasSelector.MaxOperatingDepthMeter(GasMixture.Air, Pressure.FromBar(bar), settings);

        // Assert
        Assert.Throws<ArgumentOutOfRangeException>(act);
    }

    [Theory]
    [InlineData(MaximumOperatingDepthModel.Realistic)]
    [InlineData(MaximumOperatingDepthModel.Simplified)]
    public void MaxOperatingDepthMeter_ShouldThrowArgumentOutOfRangeException_WhenTheGasContainsNoOxygen(
        MaximumOperatingDepthModel model)
    {
        // Arrange
        var settings = TestFactory.CreateSettings(maximumOperatingDepthModel: model);

        // Act
        Action act = () => GasSelector.MaxOperatingDepthMeter(default, Pressure.FromBar(1.6), settings);

        // Assert
        Assert.Throws<ArgumentOutOfRangeException>(act);
    }

    [Fact]
    public void IsBreathableAt_ShouldRejectOxygenAtSixMeters_WhenTheModelIsRealistic()
    {
        // Arrange
        var settings = TestFactory.CreateSettings(
            Pressure.FromMillibar(1000),
            Salinity.Salt,
            maximumOperatingDepthModel: MaximumOperatingDepthModel.Realistic);

        // Act
        var breathable = GasSelector.IsBreathableAt(GasMixture.Oxygen, 6.0, Pressure.FromBar(1.6), settings);

        // Assert
        Assert.False(breathable);
    }

    [Fact]
    public void IsBreathableAt_ShouldAdmitOxygenAtSixMeters_WhenTheModelIsSimplified()
    {
        // Arrange
        var settings = TestFactory.CreateSettings(
            Pressure.FromMillibar(1000),
            Salinity.Salt,
            maximumOperatingDepthModel: MaximumOperatingDepthModel.Simplified);

        // Act
        var breathable = GasSelector.IsBreathableAt(GasMixture.Oxygen, 6.0, Pressure.FromBar(1.6), settings);

        // Assert
        Assert.True(breathable);
    }

    [Fact]
    public void IsBreathableAt_ShouldReturnFalse_WhenTheModelIsSimplifiedAndTheDepthIsBeyondTheOperatingDepth()
    {
        // Arrange
        var settings = TestFactory.CreateSettings(
            maximumOperatingDepthModel: MaximumOperatingDepthModel.Simplified);

        // Act
        var breathable = GasSelector.IsBreathableAt(GasMixture.Oxygen, 6.5, Pressure.FromBar(1.6), settings);

        // Assert
        Assert.False(breathable);
    }

    [Fact]
    public void IsBreathableAt_ShouldReturnTrue_WhenTheModelIsRealisticAndThePartialPressureExactlyEqualsTheLimit()
    {
        // Arrange
        const double MillibarPerMeter = 1000.0 * 9.80665 / 100.0;
        var depthMeter = (1400.0 - 1000.0) / MillibarPerMeter;
        var settings = TestFactory.CreateSettings(
            Pressure.FromMillibar(1000),
            Salinity.Fresh,
            maximumOperatingDepthModel: MaximumOperatingDepthModel.Realistic);

        // Act
        var breathable = GasSelector.IsBreathableAt(GasMixture.Oxygen, depthMeter, Pressure.FromBar(1.4), settings);

        // Assert
        Assert.True(breathable);
    }

    [Theory]
    [InlineData(MaximumOperatingDepthModel.Realistic)]
    [InlineData(MaximumOperatingDepthModel.Simplified)]
    public void IsBreathableAt_ShouldAdmitAGasHoldingNoOxygen_WhenItHasNoOperatingDepth(
        MaximumOperatingDepthModel model)
    {
        // Arrange
        var settings = TestFactory.CreateSettings(maximumOperatingDepthModel: model);

        // Act
        var breathable = GasSelector.IsBreathableAt(default, 30.0, Pressure.FromBar(1.6), settings);

        // Assert
        Assert.True(breathable);
    }

    [Fact]
    public void IsBreathableAt_ShouldThrowArgumentNullException_WhenSettingsIsNull()
    {
        // Arrange
        DivePlanSettings settings = null!;

        // Act
        Action act = () => GasSelector.IsBreathableAt(GasMixture.Oxygen, 6.0, Pressure.FromBar(1.6), settings);

        // Assert
        Assert.Throws<ArgumentNullException>(act);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void IsBreathableAt_ShouldThrowArgumentOutOfRangeException_WhenMaxPo2IsNotPositive(double bar)
    {
        // Arrange
        var settings = TestFactory.CreateSettings();

        // Act
        Action act = () => GasSelector.IsBreathableAt(GasMixture.Air, 10.0, Pressure.FromBar(bar), settings);

        // Assert
        Assert.Throws<ArgumentOutOfRangeException>(act);
    }

    [Fact]
    public void SelectRichestGasAt_ShouldChooseTheNitrox_WhenTheModelIsRealisticAtSixMeters()
    {
        // Arrange
        var settings = TestFactory.CreateSettings(
            Pressure.FromMillibar(1000),
            Salinity.Salt,
            maximumOperatingDepthModel: MaximumOperatingDepthModel.Realistic);
        Cylinder[] cylinders =
        [
            TestFactory.CreateCylinder(GasMixture.FromPercent(50, 0), purpose: CylinderPurpose.DecoGas),
            TestFactory.CreateCylinder(GasMixture.Oxygen, purpose: CylinderPurpose.DecoGas)
        ];

        // Act
        var selected = GasSelector.SelectRichestGasAt(cylinders, 6.0, Pressure.FromBar(1.6), settings);

        // Assert
        Assert.Equal(0.5, selected.Gas.FractionO2, Precision);
    }

    [Fact]
    public void SelectRichestGasAt_ShouldChooseOxygen_WhenTheModelIsSimplifiedAtSixMeters()
    {
        // Arrange
        var settings = TestFactory.CreateSettings(
            Pressure.FromMillibar(1000),
            Salinity.Salt,
            maximumOperatingDepthModel: MaximumOperatingDepthModel.Simplified);
        Cylinder[] cylinders =
        [
            TestFactory.CreateCylinder(GasMixture.FromPercent(50, 0), purpose: CylinderPurpose.DecoGas),
            TestFactory.CreateCylinder(GasMixture.Oxygen, purpose: CylinderPurpose.DecoGas)
        ];

        // Act
        var selected = GasSelector.SelectRichestGasAt(cylinders, 6.0, Pressure.FromBar(1.6), settings);

        // Assert
        Assert.Equal(1.0, selected.Gas.FractionO2, Precision);
    }

    [Fact]
    public void SelectRichestGasAt_ShouldKeepTheRicherGas_WhenAPoorerCylinderFollowsIt()
    {
        // Arrange
        var settings = TestFactory.CreateSettings();
        Cylinder[] cylinders =
        [
            TestFactory.CreateCylinder(GasMixture.FromPercent(50, 0), purpose: CylinderPurpose.DecoGas),
            TestFactory.CreateCylinder(GasMixture.Air)
        ];

        // Act
        var selected = GasSelector.SelectRichestGasAt(cylinders, 0.0, Pressure.FromBar(1.6), settings);

        // Assert
        Assert.Equal(GasMixture.FromPercent(50, 0), selected.Gas);
    }

    [Fact]
    public void SelectRichestGasAt_ShouldPreferTheHigherHeliumContent_WhenTwoGasesShareTheSameOxygenContent()
    {
        // Arrange
        var settings = TestFactory.CreateSettings();
        Cylinder[] cylinders =
        [
            TestFactory.CreateCylinder(GasMixture.FromPercent(21, 0)),
            TestFactory.CreateCylinder(GasMixture.FromPercent(21, 35))
        ];

        // Act
        var selected = GasSelector.SelectRichestGasAt(cylinders, 30.0, Pressure.FromBar(1.4), settings);

        // Assert
        Assert.Equal(35, selected.Gas.PercentHe, Precision);
    }

    [Fact]
    public void SelectRichestGasAt_ShouldReturnTheFirstMatch_WhenTwoCylindersHoldTheIdenticalGas()
    {
        // Arrange
        var settings = TestFactory.CreateSettings();
        Cylinder[] cylinders =
        [
            TestFactory.CreateCylinder(GasMixture.Air, startPressureBar: 200),
            TestFactory.CreateCylinder(GasMixture.Air, startPressureBar: 100)
        ];

        // Act
        var selected = GasSelector.SelectRichestGasAt(cylinders, 30.0, Pressure.FromBar(1.4), settings);

        // Assert
        Assert.Equal(200, selected.StartPressure.InBar, Precision);
    }

    [Fact]
    public void SelectRichestGasAt_ShouldSkipTheOxygenSupply_WhenNoPurposeIsRequired()
    {
        // Arrange
        var settings = TestFactory.CreateSettings();
        Cylinder[] cylinders =
        [
            TestFactory.CreateCylinder(GasMixture.Air),
            TestFactory.CreateCylinder(GasMixture.FromPercent(50, 0), purpose: CylinderPurpose.Oxygen)
        ];

        // Act
        var selected = GasSelector.SelectRichestGasAt(cylinders, 0.0, Pressure.FromBar(1.6), settings);

        // Assert
        Assert.Equal(GasMixture.Air, selected.Gas);
    }

    [Fact]
    public void SelectRichestGasAt_ShouldRestrictTheChoiceToTheRequiredPurpose_WhenOneIsSupplied()
    {
        // Arrange
        var settings = TestFactory.CreateSettings();
        Cylinder[] cylinders =
        [
            TestFactory.CreateCylinder(GasMixture.Air, purpose: CylinderPurpose.Diluent),
            TestFactory.CreateCylinder(GasMixture.FromPercent(50, 0), purpose: CylinderPurpose.DecoGas)
        ];

        // Act
        var selected = GasSelector.SelectRichestGasAt(cylinders, 0.0, Pressure.FromBar(1.6), settings,
            CylinderPurpose.Diluent);

        // Assert
        Assert.Equal(GasMixture.Air.FractionO2, selected.Gas.FractionO2, Precision);
    }

    [Fact]
    public void SelectRichestGasAt_ShouldThrowArgumentNullException_WhenCylindersIsNull()
    {
        // Arrange
        IReadOnlyList<Cylinder> cylinders = null!;

        // Act
        Action act = () => GasSelector.SelectRichestGasAt(cylinders, 6.0, Pressure.FromBar(1.6),
            TestFactory.CreateSettings());

        // Assert
        Assert.Throws<ArgumentNullException>(act);
    }

    [Fact]
    public void SelectRichestGasAt_ShouldThrowArgumentNullException_WhenSettingsIsNull()
    {
        // Arrange
        Cylinder[] cylinders = [TestFactory.CreateCylinder()];

        // Act
        Action act = () => GasSelector.SelectRichestGasAt(cylinders, 6.0, Pressure.FromBar(1.6), null!);

        // Assert
        Assert.Throws<ArgumentNullException>(act);
    }

    [Fact]
    public void SelectRichestGasAt_ShouldThrowArgumentException_WhenCylindersIsEmpty()
    {
        // Arrange
        var cylinders = Array.Empty<Cylinder>();

        // Act
        Action act = () => GasSelector.SelectRichestGasAt(cylinders, 6.0, Pressure.FromBar(1.6),
            TestFactory.CreateSettings());

        // Assert
        Assert.Throws<ArgumentException>(act);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void SelectRichestGasAt_ShouldThrowArgumentOutOfRangeException_WhenMaxPo2IsNotPositive(double bar)
    {
        // Arrange
        Cylinder[] cylinders = [TestFactory.CreateCylinder()];

        // Act
        Action act = () => GasSelector.SelectRichestGasAt(cylinders, 6.0, Pressure.FromBar(bar),
            TestFactory.CreateSettings());

        // Assert
        Assert.Throws<ArgumentOutOfRangeException>(act);
    }

    [Fact]
    public void SelectRichestGasAt_ShouldThrowInvalidOperationException_WhenNoGasIsBreathableAtTheDepth()
    {
        // Arrange
        var settings = TestFactory.CreateSettings();
        Cylinder[] cylinders = [TestFactory.CreateCylinder(GasMixture.Oxygen, purpose: CylinderPurpose.DecoGas)];

        // Act
        Action act = () => GasSelector.SelectRichestGasAt(cylinders, 90.0, Pressure.FromBar(1.6), settings);

        // Assert
        Assert.Throws<InvalidOperationException>(act);
    }
}