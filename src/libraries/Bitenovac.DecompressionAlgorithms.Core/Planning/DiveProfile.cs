namespace Bitenovac.DecompressionAlgorithms.Core.Planning;

/// <summary>
/// Represents an ordered sequence of <see cref="DiveSegment" /> phases that make up a
/// dive. The same type is used both for the planned dive supplied as input and for the
/// fully expanded dive returned as a result, so that a computed profile can be fed back
/// in unchanged.
/// </summary>
/// <remarks>Instances are immutable; the segment sequence is copied on construction.</remarks>
public readonly struct DiveProfile
{
    private readonly DiveSegment[] _segments;

    /// <summary>Initializes a new instance of the <see cref="DiveProfile" /> class.</summary>
    /// <param name="segments">The ordered segments that make up the dive.</param>
    /// <exception cref="ArgumentNullException"><paramref name="segments" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="segments" /> is empty or contains a <see langword="null" />
    /// segment.
    /// </exception>
    public DiveProfile(IEnumerable<DiveSegment> segments)
    {
        ArgumentNullException.ThrowIfNull(segments);

        _segments = segments.ToArray();

        if (_segments.Length == 0)
        {
            throw new ArgumentException("A dive profile must contain at least one segment.", nameof(segments));
        }
    }

    /// <summary>Gets the ordered segments that make up the dive.</summary>
    public IReadOnlyList<DiveSegment> Segments => _segments;

    /// <summary>
    /// Gets the total bottom time of the dive, being the combined duration of all
    /// segments classified as <see cref="SegmentKind.Bottom" />.
    /// </summary>
    /// <returns>The sum of the durations of the bottom segments.</returns>
    public TimeSpan BottomTime() => _segments.Where(static segment => segment.Kind == SegmentKind.Bottom)
        .Aggregate(TimeSpan.Zero, static (current, segment) => current + segment.Duration);

    /// <summary>Gets the total elapsed time of the dive, being the combined duration of all segments.</summary>
    /// <returns>The sum of the durations of every segment.</returns>
    public TimeSpan TotalRuntime() =>
        _segments.Aggregate(TimeSpan.Zero, static (current, segment) => current + segment.Duration);
}