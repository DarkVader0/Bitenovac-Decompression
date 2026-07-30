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

    [Fact]
    public void ToString_ShouldStartWithValidityAndTotalRuntime_WhenPlanIsValid()
    {
        // Arrange
        var plan = new DecoPlan([CreateSegment()], [], TimeSpan.FromMinutes(30), CreateReserveGas(), 0, 0, []);

        // Act
        var lines = plan.ToString().Split(System.Environment.NewLine);

        // Assert
        Assert.Equal("Valid: True, total runtime: 30", lines[0]);
    }

    [Fact]
    public void ToString_ShouldMergeConsecutiveAscentsIntoOneRow_WhenAscentPassesStopDepthsWithoutHolding()
    {
        // Arrange
        var nitrox50 = GasMixture.FromPercent(50, 0);
        DiveSegment[] segments =
        [
            new(Depth.FromMeter(20), TimeSpan.FromMinutes(20), GasMixture.Air, SegmentKind.Bottom),
            new(Depth.FromMeter(9), TimeSpan.FromMinutes(2), GasMixture.Air, SegmentKind.Ascent),
            new(Depth.FromMeter(6), TimeSpan.FromMinutes(1), GasMixture.Air, SegmentKind.Ascent),
            new(Depth.FromMeter(6), TimeSpan.FromMinutes(3), nitrox50, SegmentKind.Stop),
            new(Depth.FromMeter(0), TimeSpan.FromMinutes(6), nitrox50, SegmentKind.Ascent)
        ];
        var plan = new DecoPlan(segments, [], TimeSpan.FromMinutes(32), CreateReserveGas(), 0, 0, []);

        // Act
        var lines = plan.ToString().Split(System.Environment.NewLine);

        // Assert
        // The two ascent hops merge into one row ending at the stop depth, and the final
        // ascent to the surface is emitted after the last stop.
        Assert.Equal("Bottom        20 m    20 min    20 min  Air", lines[2]);
        Assert.Equal("Ascent         6 m     3 min    23 min  Air", lines[3]);
        Assert.Equal("Stop           6 m     3 min    26 min  NX50", lines[4]);
        Assert.Equal("Ascent         0 m     6 min    32 min  NX50", lines[5]);
    }

    [Fact]
    public void ToString_ShouldReportOxygenExposureGasUsageAndReserve_WhenPlanIsComplete()
    {
        // Arrange
        var usage = new CylinderGasUsage(CreateCylinder(), Volume.FromLiter(1234.56), Pressure.FromBar(72.55));
        var plan = new DecoPlan([CreateSegment()], [usage], TimeSpan.FromMinutes(20), CreateReserveGas(),
            0.4454, 124.08, []);

        // Act
        var text = plan.ToString();

        // Assert
        Assert.Contains("CNS: 44.54 %", text);
        Assert.Contains("OTU: 124.08", text);
        Assert.Contains("Cylinder Air: used 1234.56 L, end pressure 72.55 bar", text);
        Assert.EndsWith("Reserve satisfied: True", text);
    }
}