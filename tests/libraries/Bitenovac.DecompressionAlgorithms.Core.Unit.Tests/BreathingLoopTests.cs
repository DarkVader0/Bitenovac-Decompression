using Bitenovac.DecompressionAlgorithms.Core.Equipment;
using Bitenovac.DecompressionAlgorithms.Core.Planning;
using Bitenovac.DecompressionAlgorithms.Units;

namespace Bitenovac.DecompressionAlgorithms.Core.Unit.Tests;

public sealed class BreathingLoopTests
{
    private const int Precision = 6;

    /// <summary>Trimix 18/45, so that the two inert gases are present in an uneven ratio.</summary>
    private static readonly GasMixture Trimix1845 = GasMixture.FromPercent(18, 45);

    private static readonly GasMixture Nitrox32 = GasMixture.FromPercent(32, 0);

    /// <summary>The ambient pressure at 40 m in fresh water at a surface pressure of one bar.</summary>
    private static readonly Pressure At40Meters = Pressure.FromMillibar(4922.66);

    /// <summary>The ambient pressure at 30 m in fresh water at a surface pressure of one bar.</summary>
    private static readonly Pressure At30Meters = Pressure.FromMillibar(3941.995);

    /// <summary>The ambient pressure at 100 m in fresh water at a surface pressure of one bar.</summary>
    private static readonly Pressure At100Meters = Pressure.FromMillibar(10806.65);

    [Fact]
    public void OpenCircuit_ShouldBeTheDefaultInstance_SoAnUnspecifiedSegmentIsBreathedOpenCircuit()
    {
        // Arrange
        var loop = default(BreathingLoop);

        // Act
        var mode = loop.Mode;

        // Assert
        Assert.Equal(DiveMode.OC, mode);
        Assert.Equal(BreathingLoop.OpenCircuit, loop);
    }

    [Fact]
    public void InspiredFractions_ShouldReturnTheSupplyUnaltered_WhenTheLoopIsOpenCircuit()
    {
        // Arrange
        var loop = BreathingLoop.OpenCircuit;

        // Act
        var oxygen = loop.InspiredOxygenFraction(Trimix1845, At40Meters);
        var nitrogen = loop.InspiredNitrogenFraction(Trimix1845, At40Meters);
        var helium = loop.InspiredHeliumFraction(Trimix1845, At40Meters);

        // Assert
        Assert.Equal(Trimix1845.FractionO2, oxygen, Precision);
        Assert.Equal(Trimix1845.FractionN2, nitrogen, Precision);
        Assert.Equal(Trimix1845.FractionHe, helium, Precision);
    }

    [Fact]
    public void InspiredOxygenPressure_ShouldEqualTheSetpoint_WhenTheClosedCircuitLoopCanHoldIt()
    {
        // Arrange
        // At 40 m the diluent alone gives 0.18 x 4.92266 = 0.886 bar and the ambient pressure
        // is 4.92 bar, so a 1.3 bar setpoint lies between the two and is held exactly.
        var loop = BreathingLoop.ClosedCircuit(Pressure.FromBar(1.3));

        // Act
        var oxygen = loop.InspiredOxygenPressure(Trimix1845, At40Meters);

        // Assert
        Assert.Equal(1300.0, oxygen.InMillibar, Precision);
    }

    [Fact]
    public void InspiredOxygenPressure_ShouldFallBackToTheDiluent_WhenTheSetpointIsBelowWhatTheDiluentGives()
    {
        // Arrange
        // At 100 m the diluent alone gives 0.18 x 10.80665 = 1.945 bar. Oxygen cannot be
        // removed from the loop, so the setpoint cannot be reached.
        var loop = BreathingLoop.ClosedCircuit(Pressure.FromBar(1.3));

        // Act
        var oxygen = loop.InspiredOxygenPressure(Trimix1845, At100Meters);

        // Assert
        Assert.Equal(0.18 * 10806.65, oxygen.InMillibar, Precision);
    }

    [Fact]
    public void InspiredOxygenFraction_ShouldBePureOxygen_WhenTheSetpointExceedsTheAmbientPressure()
    {
        // Arrange
        // A 1.3 bar setpoint cannot be reached at the surface, where pure oxygen gives 1 bar.
        var loop = BreathingLoop.ClosedCircuit(Pressure.FromBar(1.3));

        // Act
        var oxygen = loop.InspiredOxygenFraction(Trimix1845, Pressure.FromBar(1));

        // Assert
        Assert.Equal(1.0, oxygen, Precision);
    }

    [Fact]
    public void InspiredFractions_ShouldDivideTheBalanceInTheDiluentRatio_WhenTheLoopIsClosedCircuit()
    {
        // Arrange
        var loop = BreathingLoop.ClosedCircuit(Pressure.FromBar(1.3));

        // Act
        var oxygen = loop.InspiredOxygenFraction(Trimix1845, At40Meters);
        var nitrogen = loop.InspiredNitrogenFraction(Trimix1845, At40Meters);
        var helium = loop.InspiredHeliumFraction(Trimix1845, At40Meters);

        // Assert
        // The inspired mixture is complete, and the loop neither adds nor removes inert gas,
        // so nitrogen and helium keep the 37:45 ratio of the diluent.
        Assert.Equal(1.0, oxygen + nitrogen + helium, Precision);
        Assert.Equal(0.45 / 0.37, helium / nitrogen, Precision);
    }

    [Fact]
    public void InspiredHeliumFraction_ShouldBeZero_WhenTheDiluentHoldsNoInertGas()
    {
        // Arrange
        var loop = BreathingLoop.ClosedCircuit(Pressure.FromBar(1.3));

        // Act
        var helium = loop.InspiredHeliumFraction(GasMixture.Oxygen, At40Meters);
        var nitrogen = loop.InspiredNitrogenFraction(GasMixture.Oxygen, At40Meters);

        // Assert
        Assert.Equal(0.0, helium, Precision);
        Assert.Equal(0.0, nitrogen, Precision);
    }

    [Fact]
    public void SemiClosedOxygenDropCoefficient_ShouldFollowTheSteadyStateBalance()
    {
        // Arrange
        // The coefficient is V̇O₂ x Psurf / (r x RMV) = 1 x 1000 / (0.1 x 20) = 500 mbar.

        // Act
        var coefficient = BreathingLoop.SemiClosedOxygenDropCoefficient(0.1, 1.0, 20.0, Pressure.FromBar(1));

        // Assert
        Assert.Equal(500.0, coefficient.InMillibar, Precision);
    }

    [Fact]
    public void OxygenPressureDrop_ShouldScaleTheCoefficientByTheSupplyInertFraction()
    {
        // Arrange
        var loop = BreathingLoop.SemiClosed(0.1, Pressure.FromMillibar(500));

        // Act
        var drop = loop.OxygenPressureDrop(Nitrox32);

        // Assert
        Assert.Equal(340.0, drop.InMillibar, Precision);
    }

    [Fact]
    public void InspiredOxygenPressure_ShouldFallShortOfTheSupply_WhenTheLoopIsSemiClosed()
    {
        // Arrange
        // At 30 m the supply gives 0.32 x 3.941995 = 1.2614 bar, from which the loop runs
        // 0.34 bar short.
        var loop = BreathingLoop.SemiClosed(0.1, Pressure.FromMillibar(500));

        // Act
        var oxygen = loop.InspiredOxygenPressure(Nitrox32, At30Meters);

        // Assert
        Assert.Equal(921.4384, oxygen.InMillibar, Precision);
    }

    [Fact]
    public void InspiredOxygenFraction_ShouldBeZero_WhenTheSemiClosedLoopCannotSustainAnyOxygen()
    {
        // Arrange
        // At the surface the supply gives only 0.32 bar, less than the 0.34 bar shortfall.
        var loop = BreathingLoop.SemiClosed(0.1, Pressure.FromMillibar(500));

        // Act
        var oxygen = loop.InspiredOxygenFraction(Nitrox32, Pressure.FromBar(1));

        // Assert
        Assert.Equal(0.0, oxygen, Precision);
    }

    [Fact]
    public void SupplyPurpose_ShouldRestrictARebreatherToItsDiluent_ButLeaveOpenCircuitUnrestricted()
    {
        // Arrange

        // Act
        var openCircuit = BreathingLoop.OpenCircuit.SupplyPurpose;
        var closedCircuit = BreathingLoop.ClosedCircuit(Pressure.FromBar(1.3)).SupplyPurpose;
        var semiClosed = BreathingLoop.SemiClosed(0.1, Pressure.FromMillibar(500)).SupplyPurpose;

        // Assert
        Assert.Null(openCircuit);
        Assert.Equal(CylinderPurpose.Diluent, closedCircuit);
        Assert.Equal(CylinderPurpose.Diluent, semiClosed);
    }

    [Fact]
    public void ForDecompression_ShouldRaiseTheLoopToItsDecompressionSetpoint()
    {
        // Arrange
        var loop = BreathingLoop.ClosedCircuit(Pressure.FromBar(1.2), Pressure.FromBar(1.6));

        // Act
        var raised = loop.ForDecompression();

        // Assert
        Assert.Equal(1200.0, loop.Setpoint.InMillibar, Precision);
        Assert.Equal(1600.0, raised.Setpoint.InMillibar, Precision);
        Assert.Equal(1600.0, raised.InspiredOxygenPressure(Trimix1845, At40Meters).InMillibar, Precision);
    }

    [Fact]
    public void ForDecompression_ShouldLeaveTheLoopAsItIs_WhenItIsAlreadyRaised()
    {
        // Arrange
        var raised = BreathingLoop.ClosedCircuit(Pressure.FromBar(1.2), Pressure.FromBar(1.6)).ForDecompression();

        // Act
        var raisedAgain = raised.ForDecompression();

        // Assert
        Assert.Equal(raised, raisedAgain);
    }

    [Fact]
    public void ForDecompression_ShouldChangeNothing_WhenTheLoopIsRunAtOneSetpointThroughout()
    {
        // Arrange
        var loop = BreathingLoop.ClosedCircuit(Pressure.FromBar(1.3));

        // Act
        var raised = loop.ForDecompression();

        // Assert
        Assert.Equal(1300.0, loop.DecoSetpoint.InMillibar, Precision);
        Assert.Equal(loop, raised);
    }

    [Fact]
    public void ForDecompression_ShouldChangeNothing_WhenTheApparatusHasNoSetpointToRaise()
    {
        // Arrange
        var openCircuit = BreathingLoop.OpenCircuit;
        var semiClosed = BreathingLoop.SemiClosed(0.1, Pressure.FromMillibar(500));

        // Act
        var raisedOpenCircuit = openCircuit.ForDecompression();
        var raisedSemiClosed = semiClosed.ForDecompression();

        // Assert
        Assert.Equal(openCircuit, raisedOpenCircuit);
        Assert.Equal(semiClosed, raisedSemiClosed);
    }

    [Fact]
    public void Equals_ShouldDistinguishLoops_WhenOnlyTheirDecompressionSetpointDiffers()
    {
        // Arrange
        var switching = BreathingLoop.ClosedCircuit(Pressure.FromBar(1.2), Pressure.FromBar(1.6));
        var fixedSetpoint = BreathingLoop.ClosedCircuit(Pressure.FromBar(1.2));

        // Act
        var equal = switching == fixedSetpoint;

        // Assert
        Assert.False(equal);
        Assert.Equal(switching.Setpoint.InMillibar, fixedSetpoint.Setpoint.InMillibar, Precision);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void ClosedCircuit_ShouldThrowArgumentOutOfRangeException_WhenTheDecoSetpointIsNotPositive(double bar)
    {
        // Arrange

        // Act
        Action act = () => BreathingLoop.ClosedCircuit(Pressure.FromBar(1.2), Pressure.FromBar(bar));

        // Assert
        Assert.Throws<ArgumentOutOfRangeException>(act);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void ClosedCircuit_ShouldThrowArgumentOutOfRangeException_WhenTheSetpointIsNotPositive(double bar)
    {
        // Arrange

        // Act
        Action act = () => BreathingLoop.ClosedCircuit(Pressure.FromBar(bar));

        // Assert
        Assert.Throws<ArgumentOutOfRangeException>(act);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-0.1)]
    [InlineData(1.1)]
    public void SemiClosed_ShouldThrowArgumentOutOfRangeException_WhenTheDumpRatioIsOutsideItsRange(double dumpRatio)
    {
        // Arrange

        // Act
        Action act = () => BreathingLoop.SemiClosed(dumpRatio, Pressure.FromMillibar(500));

        // Assert
        Assert.Throws<ArgumentOutOfRangeException>(act);
    }

    [Fact]
    public void SemiClosed_ShouldThrowArgumentOutOfRangeException_WhenTheDropCoefficientIsNegative()
    {
        // Arrange

        // Act
        Action act = () => BreathingLoop.SemiClosed(0.1, Pressure.FromMillibar(-1));

        // Assert
        Assert.Throws<ArgumentOutOfRangeException>(act);
    }

    [Fact]
    public void SemiClosedOxygenDropCoefficient_ShouldThrowArgumentOutOfRangeException_WhenTheMinuteVolumeIsNotPositive()
    {
        // Arrange

        // Act
        Action act = () => BreathingLoop.SemiClosedOxygenDropCoefficient(0.1, 1.0, 0.0, Pressure.FromBar(1));

        // Assert
        Assert.Throws<ArgumentOutOfRangeException>(act);
    }

    [Fact]
    public void Equals_ShouldDistinguishLoops_WhenTheirModeOrParameterDiffers()
    {
        // Arrange
        var first = BreathingLoop.ClosedCircuit(Pressure.FromBar(1.3));
        var same = BreathingLoop.ClosedCircuit(Pressure.FromBar(1.3));
        var otherSetpoint = BreathingLoop.ClosedCircuit(Pressure.FromBar(1.2));

        // Act
        var equalToSame = first == same;
        var equalToOtherSetpoint = first == otherSetpoint;

        // Assert
        Assert.True(equalToSame);
        Assert.False(equalToOtherSetpoint);
        Assert.Equal(first.GetHashCode(), same.GetHashCode());
    }

    [Fact]
    public void ToString_ShouldNameTheApparatusWithItsParameter()
    {
        // Arrange

        // Act
        var openCircuit = BreathingLoop.OpenCircuit.ToString();
        var closedCircuit = BreathingLoop.ClosedCircuit(Pressure.FromBar(1.3)).ToString();
        var semiClosed = BreathingLoop.SemiClosed(0.1, Pressure.FromMillibar(500)).ToString();

        // Assert
        Assert.Equal("OC", openCircuit);
        Assert.Equal("CCR @ 1.3", closedCircuit);
        Assert.Equal("PSCR 1:10", semiClosed);
    }
}
