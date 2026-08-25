using Bitenovac.DecompressionAlgorithms.Core.Planning;
using Bitenovac.DecompressionAlgorithms.Units;

namespace Bitenovac.DecompressionAlgorithms.Core.Unit.Tests;

public sealed class DiveProfileTests
{
    private static DiveSegment CreateSegment(SegmentKind kind,
        double minutes)
    {
        return new DiveSegment(Depth.FromMeter(20), TimeSpan.FromMinutes(minutes), GasMixture.Air, kind);
    }

    [Fact]
    public void Constructor_ShouldThrowArgumentNullException_WhenSegmentsIsNull()
    {
        // Arrange
        IEnumerable<DiveSegment> segments = null!;

        // Act
        Action act = () => new DiveProfile(segments);

        // Assert
        Assert.Throws<ArgumentNullException>(act);
    }

    [Fact]
    public void Constructor_ShouldThrowArgumentException_WhenSegmentsIsEmpty()
    {
        // Arrange
        var segments = Array.Empty<DiveSegment>();

        // Act
        Action act = () => new DiveProfile(segments);

        // Assert
        Assert.Throws<ArgumentException>(act);
    }

    [Fact]
    public void Constructor_ShouldSucceed_WhenSegmentsContainsExactlyOneEntry()
    {
        // Arrange
        var segments = new[] { CreateSegment(SegmentKind.Bottom, 20) };

        // Act
        var profile = new DiveProfile(segments);

        // Assert
        Assert.Single(profile.Segments);
    }

    [Fact]
    public void Segments_ShouldReturnSuppliedSegments_InOrder()
    {
        // Arrange
        var first = CreateSegment(SegmentKind.Descent, 2);
        var second = CreateSegment(SegmentKind.Bottom, 20);

        // Act
        var profile = new DiveProfile([first, second]);

        // Assert
        Assert.Equal(SegmentKind.Descent, profile.Segments[0].Kind);
        Assert.Equal(SegmentKind.Bottom, profile.Segments[1].Kind);
    }

    [Fact]
    public void Segments_ShouldNotReflectChanges_WhenSourceListIsModifiedAfterConstruction()
    {
        // Arrange
        var segments = new List<DiveSegment> { CreateSegment(SegmentKind.Bottom, 20) };
        var profile = new DiveProfile(segments);

        // Act
        segments.Add(CreateSegment(SegmentKind.Ascent, 5));

        // Assert
        Assert.Single(profile.Segments);
    }

    [Fact]
    public void BottomTime_ShouldReturnZero_WhenNoSegmentIsOfKindBottom()
    {
        // Arrange
        var profile = new DiveProfile([
            CreateSegment(SegmentKind.Descent, 2),
            CreateSegment(SegmentKind.Ascent, 5)
        ]);

        // Act
        var bottomTime = profile.BottomTime();

        // Assert
        Assert.Equal(TimeSpan.Zero, bottomTime);
    }

    [Fact]
    public void BottomTime_ShouldReturnSumOfBottomSegmentDurations_WhenMultipleBottomSegmentsArePresent()
    {
        // Arrange
        var profile = new DiveProfile([
            CreateSegment(SegmentKind.Descent, 2),
            CreateSegment(SegmentKind.Bottom, 20),
            CreateSegment(SegmentKind.GasSwitch, 1),
            CreateSegment(SegmentKind.Bottom, 10),
            CreateSegment(SegmentKind.Ascent, 5)
        ]);

        // Act
        var bottomTime = profile.BottomTime();

        // Assert
        Assert.Equal(TimeSpan.FromMinutes(30), bottomTime);
    }

    [Fact]
    public void BottomTime_ShouldExcludeNonBottomSegments_WhenComputingTheSum()
    {
        // Arrange
        var profile = new DiveProfile([
            CreateSegment(SegmentKind.Descent, 2),
            CreateSegment(SegmentKind.Bottom, 20),
            CreateSegment(SegmentKind.Stop, 3),
            CreateSegment(SegmentKind.Ascent, 5)
        ]);

        // Act
        var bottomTime = profile.BottomTime();

        // Assert
        Assert.Equal(TimeSpan.FromMinutes(20), bottomTime);
    }

    [Fact]
    public void TotalRuntime_ShouldReturnSumOfAllSegmentDurations_RegardlessOfKind()
    {
        // Arrange
        var profile = new DiveProfile([
            CreateSegment(SegmentKind.Descent, 2),
            CreateSegment(SegmentKind.Bottom, 20),
            CreateSegment(SegmentKind.Stop, 3),
            CreateSegment(SegmentKind.Ascent, 5)
        ]);

        // Act
        var totalRuntime = profile.TotalRuntime();

        // Assert
        Assert.Equal(TimeSpan.FromMinutes(30), totalRuntime);
    }

    [Fact]
    public void TotalRuntime_ShouldReturnSingleSegmentDuration_WhenOnlyOneSegmentIsPresent()
    {
        // Arrange
        var profile = new DiveProfile([CreateSegment(SegmentKind.Bottom, 20)]);

        // Act
        var totalRuntime = profile.TotalRuntime();

        // Assert
        Assert.Equal(TimeSpan.FromMinutes(20), totalRuntime);
    }
}