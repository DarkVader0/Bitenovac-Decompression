using System.Collections;
using Bitenovac.DecompressionAlgorithms.BuhlmannZhl16c;
using Bitenovac.DecompressionAlgorithms.Core.Planning;
using Bitenovac.DecompressionAlgorithms.Units;

namespace Bitenovac.DecompressionAlgorithms.Zhl16c.Unit.Tests;

public sealed class SegmentBufferTests
{
    private const int BeyondInitialCapacityCount = 65;

    private static DiveSegment CreateSegment(double depthMeter) =>
        new(Depth.FromMeter(depthMeter), TimeSpan.FromMinutes(1), GasMixture.Air, SegmentKind.Bottom);

    [Fact]
    public void Count_ShouldBeZero_WhenBufferIsNew()
    {
        // Arrange
        var buffer = new SegmentBuffer();

        // Act

        // Assert
        Assert.Empty(buffer);
    }

    [Fact]
    public void Add_ShouldAppendSegmentsInOrder_WhenCalledRepeatedly()
    {
        // Arrange
        var buffer = new SegmentBuffer();
        var first = CreateSegment(10);
        var second = CreateSegment(20);

        // Act
        buffer.Add(first);
        buffer.Add(second);

        // Assert
        Assert.Equal(2, buffer.Count);
        Assert.Equal(first, buffer[0]);
        Assert.Equal(second, buffer[1]);
    }

    [Fact]
    public void Add_ShouldRetainAllSegments_WhenGrowingBeyondInitialCapacity()
    {
        // Arrange
        var buffer = new SegmentBuffer();

        // Act
        for (var i = 0; i < BeyondInitialCapacityCount; i++)
        {
            buffer.Add(CreateSegment(i));
        }

        // Assert
        Assert.Equal(BeyondInitialCapacityCount, buffer.Count);
        for (var i = 0; i < BeyondInitialCapacityCount; i++)
        {
            Assert.Equal(CreateSegment(i), buffer[i]);
        }
    }

    [Fact]
    public void Indexer_ShouldThrowArgumentOutOfRangeException_WhenIndexIsNegative()
    {
        // Arrange
        var buffer = new SegmentBuffer();
        buffer.Add(CreateSegment(10));

        // Act
        Action act = () => _ = buffer[-1];

        // Assert
        Assert.Throws<ArgumentOutOfRangeException>(act);
    }

    [Fact]
    public void Indexer_ShouldThrowArgumentOutOfRangeException_WhenIndexEqualsCount()
    {
        // Arrange
        var buffer = new SegmentBuffer();
        buffer.Add(CreateSegment(10));

        // Act
        Action act = () => _ = buffer[1];

        // Assert
        Assert.Throws<ArgumentOutOfRangeException>(act);
    }

    [Fact]
    public void Clear_ShouldEmptyBuffer_WhenSegmentsWereAdded()
    {
        // Arrange
        var buffer = new SegmentBuffer();
        buffer.Add(CreateSegment(10));
        buffer.Add(CreateSegment(20));

        // Act
        buffer.Clear();

        // Assert
        Assert.Empty(buffer);
    }

    [Fact]
    public void Clear_ShouldAllowReuse_WhenSegmentsAreAddedAfterwards()
    {
        // Arrange
        var buffer = new SegmentBuffer();
        buffer.Add(CreateSegment(10));
        buffer.Clear();
        var segment = CreateSegment(30);

        // Act
        buffer.Add(segment);

        // Assert
        Assert.Equal(segment, Assert.Single(buffer));
    }

    [Fact]
    public void CopyTo_ShouldCopySegmentsAtGivenIndex_WhenDestinationIsLargeEnough()
    {
        // Arrange
        var buffer = new SegmentBuffer();
        var first = CreateSegment(10);
        var second = CreateSegment(20);
        buffer.Add(first);
        buffer.Add(second);
        var destination = new DiveSegment[4];

        // Act
        buffer.CopyTo(destination, 1);

        // Assert
        Assert.Equal(default, destination[0]);
        Assert.Equal(first, destination[1]);
        Assert.Equal(second, destination[2]);
        Assert.Equal(default, destination[3]);
    }

    [Fact]
    public void CopyTo_ShouldThrowArgumentNullException_WhenArrayIsNull()
    {
        // Arrange
        var buffer = new SegmentBuffer();

        // Act
        var act = () => buffer.CopyTo(null!, 0);

        // Assert
        Assert.Throws<ArgumentNullException>(act);
    }

    [Fact]
    public void CopyTo_ShouldThrowArgumentOutOfRangeException_WhenArrayIndexIsNegative()
    {
        // Arrange
        var buffer = new SegmentBuffer();
        buffer.Add(CreateSegment(10));

        // Act
        var act = () => buffer.CopyTo(new DiveSegment[4], -1);

        // Assert
        Assert.Throws<ArgumentOutOfRangeException>(act);
    }

    [Fact]
    public void CopyTo_ShouldThrowArgumentException_WhenDestinationIsTooSmall()
    {
        // Arrange
        var buffer = new SegmentBuffer();
        buffer.Add(CreateSegment(10));
        buffer.Add(CreateSegment(20));

        // Act
        var act = () => buffer.CopyTo(new DiveSegment[2], 1);

        // Assert
        Assert.Throws<ArgumentException>(act);
    }

    [Fact]
    public void GetEnumerator_ShouldYieldSegmentsInOrder_WhenBufferHasSegments()
    {
        // Arrange
        var buffer = new SegmentBuffer();
        var first = CreateSegment(10);
        var second = CreateSegment(20);
        buffer.Add(first);
        buffer.Add(second);

        // Act
        var enumerated = buffer.ToList();

        // Assert
        Assert.Equal([first, second], enumerated);
    }

    [Fact]
    public void GetEnumerator_ShouldYieldSegmentsInOrder_WhenEnumeratedNonGenerically()
    {
        // Arrange
        var buffer = new SegmentBuffer();
        var segment = CreateSegment(10);
        buffer.Add(segment);
        IEnumerable enumerable = buffer;

        // Act
        var enumerated = enumerable.Cast<DiveSegment>().ToList();

        // Assert
        Assert.Equal([segment], enumerated);
    }

    [Fact]
    public void AddRange_ShouldCopySegments_WhenBufferIsConsumedAsCollection()
    {
        // Arrange
        var buffer = new SegmentBuffer();
        var first = CreateSegment(10);
        var second = CreateSegment(20);
        buffer.Add(first);
        buffer.Add(second);
        var list = new List<DiveSegment>();

        // Act
        list.AddRange(buffer);

        // Assert
        Assert.Equal([first, second], list);
    }

    [Fact]
    public void IsReadOnly_ShouldBeTrue_WhenAccessedThroughCollectionInterface()
    {
        // Arrange
        ICollection<DiveSegment> buffer = new SegmentBuffer();

        // Act
        var isReadOnly = buffer.IsReadOnly;

        // Assert
        Assert.True(isReadOnly);
    }

    [Fact]
    public void Add_ShouldThrowNotSupportedException_WhenCalledThroughCollectionInterface()
    {
        // Arrange
        ICollection<DiveSegment> buffer = new SegmentBuffer();

        // Act
        var act = () => buffer.Add(CreateSegment(10));

        // Assert
        Assert.Throws<NotSupportedException>(act);
    }

    [Fact]
    public void Clear_ShouldThrowNotSupportedException_WhenCalledThroughCollectionInterface()
    {
        // Arrange
        ICollection<DiveSegment> buffer = new SegmentBuffer();

        // Act
        var act = buffer.Clear;

        // Assert
        Assert.Throws<NotSupportedException>(act);
    }

    [Fact]
    public void Remove_ShouldThrowNotSupportedException_WhenCalledThroughCollectionInterface()
    {
        // Arrange
        ICollection<DiveSegment> buffer = new SegmentBuffer();

        // Act
        Action act = () => buffer.Remove(CreateSegment(10));

        // Assert
        Assert.Throws<NotSupportedException>(act);
    }

    [Fact]
    public void Contains_ShouldReturnTrue_WhenSegmentIsInBuffer()
    {
        // Arrange
        var buffer = new SegmentBuffer();
        var segment = CreateSegment(10);
        buffer.Add(segment);
        ICollection<DiveSegment> collection = buffer;

        // Act
        var contains = collection.Contains(segment);

        // Assert
        Assert.True(contains);
    }

    [Fact]
    public void Contains_ShouldReturnFalse_WhenSegmentIsNotInBuffer()
    {
        // Arrange
        var buffer = new SegmentBuffer();
        buffer.Add(CreateSegment(10));
        ICollection<DiveSegment> collection = buffer;

        // Act
        var contains = collection.Contains(CreateSegment(20));

        // Assert
        Assert.False(contains);
    }
}