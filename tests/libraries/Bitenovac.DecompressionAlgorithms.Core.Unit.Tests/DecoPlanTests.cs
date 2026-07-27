using Bitenovac.DecompressionAlgorithms.Core.Calculations;
using Bitenovac.DecompressionAlgorithms.Core.Equipment;
using Bitenovac.DecompressionAlgorithms.Core.Planning;
using Bitenovac.DecompressionAlgorithms.Units;

namespace Bitenovac.DecompressionAlgorithms.Core.Unit.Tests;

public sealed class DecoPlanTests
{
    private const int Precision = 5;

    private static Cylinder CreateCylinder() =>
        new(GasMixture.Air, Volume.FromLiter(12), Pressure.FromBar(200), CylinderPurpose.BottomGas);

    private static DiveSegment CreateSegment() =>
        new(Depth.FromMeter(20), TimeSpan.FromMinutes(20), GasMixture.Air, SegmentKind.Bottom);

    private static ReserveGasResult CreateReserveGas() =>
        new(Array.Empty<CylinderReserveStatus>());

    [Fact]
    public void Constructor_ShouldThrowArgumentNullException_WhenExpandedSegmentsIsNull()
    {
        // Arrange
        IEnumerable<DiveSegment> expandedSegments = null!;

        // Act
        Action act = () => new DecoPlan(expandedSegments, [], TimeSpan.Zero, CreateReserveGas(), 0, 0, []);

        // Assert
        Assert.Throws<ArgumentNullException>(act);
    }

    [Fact]
    public void Constructor_ShouldThrowArgumentNullException_WhenGasUsageIsNull()
    {
        // Arrange
        IEnumerable<CylinderGasUsage> gasUsage = null!;

        // Act
        Action act = () => new DecoPlan([CreateSegment()], gasUsage, TimeSpan.Zero, CreateReserveGas(), 0, 0, []);

        // Assert
        Assert.Throws<ArgumentNullException>(act);
    }

    [Fact]
    public void Constructor_ShouldThrowArgumentNullException_WhenReserveGasIsNull()
    {
        // Arrange
        ReserveGasResult reserveGas = null!;

        // Act
        Action act = () => new DecoPlan([CreateSegment()], [], TimeSpan.Zero, reserveGas, 0, 0, []);

        // Assert
        Assert.Throws<ArgumentNullException>(act);
    }

    [Fact]
    public void Constructor_ShouldThrowArgumentNullException_WhenViolationsIsNull()
    {
        // Arrange
        IEnumerable<AscentViolation> violations = null!;

        // Act
        Action act = () => new DecoPlan([CreateSegment()], [], TimeSpan.Zero, CreateReserveGas(), 0, 0, violations);

        // Assert
        Assert.Throws<ArgumentNullException>(act);
    }

    [Fact]
    public void Constructor_ShouldThrowArgumentException_WhenExpandedSegmentsIsEmpty()
    {
        // Arrange
        var expandedSegments = Array.Empty<DiveSegment>();

        // Act
        Action act = () => new DecoPlan(expandedSegments, [], TimeSpan.Zero, CreateReserveGas(), 0, 0, []);

        // Assert
        Assert.Throws<ArgumentException>(act);
    }

    [Fact]
    public void Constructor_ShouldThrowArgumentException_WhenGasUsageContainsNullEntry()
    {
        // Arrange
        var gasUsage = new CylinderGasUsage[] { null! };

        // Act
        Action act = () => new DecoPlan([CreateSegment()], gasUsage, TimeSpan.Zero, CreateReserveGas(), 0, 0, []);

        // Assert
        Assert.Throws<ArgumentException>(act);
    }

    [Fact]
    public void Constructor_ShouldThrowArgumentException_WhenViolationsContainsNullEntry()
    {
        // Arrange
        var violations = new AscentViolation[] { null! };

        // Act
        Action act = () => new DecoPlan([CreateSegment()], [], TimeSpan.Zero, CreateReserveGas(), 0, 0, violations);

        // Assert
        Assert.Throws<ArgumentException>(act);
    }

    [Fact]
    public void Constructor_ShouldThrowArgumentOutOfRangeException_WhenTotalRuntimeIsNegative()
    {
        // Arrange
        var totalRuntime = TimeSpan.FromSeconds(-1);

        // Act
        Action act = () => new DecoPlan([CreateSegment()], [], totalRuntime, CreateReserveGas(), 0, 0, []);

        // Assert
        Assert.Throws<ArgumentOutOfRangeException>(act);
    }

    [Fact]
    public void Constructor_ShouldThrowArgumentOutOfRangeException_WhenCentralNervousSystemFractionIsNegative()
    {
        // Arrange
        const double centralNervousSystemFraction = -0.01;

        // Act
        Action act = () => new DecoPlan([CreateSegment()], [], TimeSpan.Zero, CreateReserveGas(),
            centralNervousSystemFraction, 0, []);

        // Assert
        Assert.Throws<ArgumentOutOfRangeException>(act);
    }

    [Fact]
    public void Constructor_ShouldThrowArgumentOutOfRangeException_WhenOxygenToleranceUnitsIsNegative()
    {
        // Arrange
        const double oxygenToleranceUnits = -0.01;

        // Act
        Action act = () =>
            new DecoPlan([CreateSegment()], [], TimeSpan.Zero, CreateReserveGas(), 0, oxygenToleranceUnits, []);

        // Assert
        Assert.Throws<ArgumentOutOfRangeException>(act);
    }

    [Fact]
    public void Constructor_ShouldSucceed_WhenTotalRuntimeIsZero()
    {
        // Arrange
        var totalRuntime = TimeSpan.Zero;

        // Act
        var plan = new DecoPlan([CreateSegment()], [], totalRuntime, CreateReserveGas(), 0, 0, []);

        // Assert
        Assert.Equal(TimeSpan.Zero, plan.TotalRuntime);
    }

    [Fact]
    public void Constructor_ShouldSucceed_WhenCentralNervousSystemFractionAndOxygenToleranceUnitsAreZero()
    {
        // Arrange

        // Act
        var plan = new DecoPlan([CreateSegment()], [], TimeSpan.Zero, CreateReserveGas(), 0, 0, []);

        // Assert
        Assert.Equal(0, plan.CentralNervousSystemFraction, Precision);
        Assert.Equal(0, plan.OxygenToleranceUnits, Precision);
    }

    [Fact]
    public void ExpandedSegments_ShouldReturnSuppliedSegments_WhenSet()
    {
        // Arrange
        var segment = CreateSegment();

        // Act
        var plan = new DecoPlan([segment], [], TimeSpan.Zero, CreateReserveGas(), 0, 0, []);

        // Assert
        Assert.Single(plan.ExpandedSegments);
        Assert.Equal(segment.Depth.InMeter, plan.ExpandedSegments[0].Depth.InMeter, Precision);
    }

    [Fact]
    public void ExpandedSegments_ShouldNotReflectChanges_WhenSourceListIsModifiedAfterConstruction()
    {
        // Arrange
        var segments = new List<DiveSegment> { CreateSegment() };
        var plan = new DecoPlan(segments, [], TimeSpan.Zero, CreateReserveGas(), 0, 0, []);

        // Act
        segments.Add(CreateSegment());

        // Assert
        Assert.Single(plan.ExpandedSegments);
    }

    [Fact]
    public void GasUsage_ShouldReturnSuppliedUsage_WhenSet()
    {
        // Arrange
        var usage = new CylinderGasUsage(CreateCylinder(), Volume.FromLiter(500), Pressure.FromBar(100));

        // Act
        var plan = new DecoPlan([CreateSegment()], [usage], TimeSpan.Zero, CreateReserveGas(), 0, 0, []);

        // Assert
        Assert.Single(plan.GasUsage);
        Assert.Equal(500, plan.GasUsage[0].GasUsed.InLiter, Precision);
    }

    [Fact]
    public void TotalRuntime_ShouldReturnConstructorValue_WhenSet()
    {
        // Arrange
        var totalRuntime = TimeSpan.FromMinutes(45);

        // Act
        var plan = new DecoPlan([CreateSegment()], [], totalRuntime, CreateReserveGas(), 0, 0, []);

        // Assert
        Assert.Equal(totalRuntime, plan.TotalRuntime);
    }

    [Fact]
    public void ReserveGas_ShouldReturnConstructorValue_WhenSet()
    {
        // Arrange
        var reserveGas = CreateReserveGas();

        // Act
        var plan = new DecoPlan([CreateSegment()], [], TimeSpan.Zero, reserveGas, 0, 0, []);

        // Assert
        Assert.Same(reserveGas, plan.ReserveGas);
    }

    [Fact]
    public void CentralNervousSystemFraction_ShouldReturnConstructorValue_WhenSet()
    {
        // Arrange
        const double centralNervousSystemFraction = 0.42;

        // Act
        var plan = new DecoPlan([CreateSegment()], [], TimeSpan.Zero, CreateReserveGas(), centralNervousSystemFraction,
            0, []);

        // Assert
        Assert.Equal(centralNervousSystemFraction, plan.CentralNervousSystemFraction, Precision);
    }

    [Fact]
    public void OxygenToleranceUnits_ShouldReturnConstructorValue_WhenSet()
    {
        // Arrange
        const double oxygenToleranceUnits = 250;

        // Act
        var plan = new DecoPlan([CreateSegment()], [], TimeSpan.Zero, CreateReserveGas(), 0, oxygenToleranceUnits, []);

        // Assert
        Assert.Equal(oxygenToleranceUnits, plan.OxygenToleranceUnits, Precision);
    }

    [Fact]
    public void Violations_ShouldReturnSuppliedViolations_WhenSet()
    {
        // Arrange
        var violation = new AscentViolation(Depth.FromMeter(30), Depth.FromMeter(20), Depth.FromMeter(24));

        // Act
        var plan = new DecoPlan([CreateSegment()], [], TimeSpan.Zero, CreateReserveGas(), 0, 0, [violation]);

        // Assert
        Assert.Single(plan.Violations);
        Assert.Same(violation, plan.Violations[0]);
    }

    [Fact]
    public void Violations_ShouldNotReflectChanges_WhenSourceListIsModifiedAfterConstruction()
    {
        // Arrange
        var violations = new List<AscentViolation>();
        var plan = new DecoPlan([CreateSegment()], [], TimeSpan.Zero, CreateReserveGas(), 0, 0, violations);

        // Act
        violations.Add(new AscentViolation(Depth.FromMeter(30), Depth.FromMeter(20), Depth.FromMeter(24)));

        // Assert
        Assert.Empty(plan.Violations);
    }

    [Fact]
    public void IsValid_ShouldReturnTrue_WhenViolationsIsEmpty()
    {
        // Arrange
        var plan = new DecoPlan([CreateSegment()], [], TimeSpan.Zero, CreateReserveGas(), 0, 0, []);

        // Act
        var isValid = plan.IsValid;

        // Assert
        Assert.True(isValid);
    }

    [Fact]
    public void IsValid_ShouldReturnFalse_WhenViolationsIsNotEmpty()
    {
        // Arrange
        var violation = new AscentViolation(Depth.FromMeter(30), Depth.FromMeter(20), Depth.FromMeter(24));
        var plan = new DecoPlan([CreateSegment()], [], TimeSpan.Zero, CreateReserveGas(), 0, 0, [violation]);

        // Act
        var isValid = plan.IsValid;

        // Assert
        Assert.False(isValid);
    }
}