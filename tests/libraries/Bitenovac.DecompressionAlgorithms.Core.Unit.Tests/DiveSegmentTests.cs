using Bitenovac.DecompressionAlgorithms.Core.Planning;
using Bitenovac.DecompressionAlgorithms.Units;

namespace Bitenovac.DecompressionAlgorithms.Core.Unit.Tests;

public sealed class DiveSegmentTests
{
    private const int Precision = 5;

    [Fact]
    public void Depth_ShouldReturnConstructorValue_WhenSet()
    {
        // Arrange
        var depth = Depth.FromMeter(30);

        // Act
        var segment = new DiveSegment(depth, TimeSpan.FromMinutes(10), GasMixture.Air, SegmentKind.Bottom);

        // Assert
        Assert.Equal(30, segment.Depth.InMeter, Precision);
    }

    [Fact]
    public void Duration_ShouldReturnConstructorValue_WhenSet()
    {
        // Arrange
        var duration = TimeSpan.FromMinutes(10);

        // Act
        var segment = new DiveSegment(Depth.FromMeter(30), duration, GasMixture.Air, SegmentKind.Bottom);

        // Assert
        Assert.Equal(TimeSpan.FromMinutes(10), segment.Duration);
    }

    [Fact]
    public void Gas_ShouldReturnConstructorValue_WhenSet()
    {
        // Arrange
        var gas = GasMixture.FromPercent(32, 0);

        // Act
        var segment = new DiveSegment(Depth.FromMeter(30), TimeSpan.FromMinutes(10), gas, SegmentKind.Bottom);

        // Assert
        Assert.Equal(gas, segment.Gas);
    }

    [Theory]
    [InlineData(SegmentKind.Descent)]
    [InlineData(SegmentKind.Bottom)]
    [InlineData(SegmentKind.Ascent)]
    [InlineData(SegmentKind.Stop)]
    [InlineData(SegmentKind.GasSwitch)]
    public void Kind_ShouldReturnConstructorValue_WhenSet(SegmentKind kind)
    {
        // Arrange
        var depth = Depth.FromMeter(30);

        // Act
        var segment = new DiveSegment(depth, TimeSpan.FromMinutes(10), GasMixture.Air, kind);

        // Assert
        Assert.Equal(kind, segment.Kind);
    }

    [Fact]
    public void Constructor_ShouldAllowZeroDuration_WhenSegmentHasNoElapsedTime()
    {
        // Arrange
        var depth = Depth.FromMeter(30);

        // Act
        var segment = new DiveSegment(depth, TimeSpan.Zero, GasMixture.Air, SegmentKind.GasSwitch);

        // Assert
        Assert.Equal(TimeSpan.Zero, segment.Duration);
    }

    [Fact]
    public void Constructor_ShouldAllowSurfaceDepth_WhenSegmentIsAtZeroMeters()
    {
        // Arrange
        var depth = Depth.Zero;

        // Act
        var segment = new DiveSegment(depth, TimeSpan.FromMinutes(3), GasMixture.Air, SegmentKind.Ascent);

        // Assert
        Assert.Equal(0, segment.Depth.InMeter, Precision);
    }

    [Fact]
    public void Default_ShouldReturnZeroDepthAndZeroDuration_WhenNotConstructed()
    {
        // Arrange
        var segment = default(DiveSegment);

        // Act
        var depthMeter = segment.Depth.InMeter;

        // Assert
        Assert.Equal(0, depthMeter, Precision);
        Assert.Equal(TimeSpan.Zero, segment.Duration);
    }
}