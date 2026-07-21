using Bitenovac.DecompressionAlgorithms.Core.Gas;
using Bitenovac.DecompressionAlgorithms.Units;

namespace Bitenovac.DecompressionAlgorithms.Core.Planning;

/// <summary>
/// Represents a single phase of a dive held for a fixed duration: a descent, bottom
/// interval, ascent, decompression stop, or gas switch. Segments are the building
/// block shared by the planned <see cref="DiveProfile" /> supplied as input and the
/// fully expanded profile returned as a result.
/// </summary>
/// <remarks>Instances are immutable.</remarks>
public readonly struct DiveSegment
{
    /// <summary>Initializes a new instance of the <see cref="DiveSegment" /> class.</summary>
    /// <param name="depth">The depth at which the segment is held, or the depth reached at its end.</param>
    /// <param name="duration">The length of time the segment lasts.</param>
    /// <param name="gas">The breathing gas used during the segment.</param>
    /// <param name="kind">The role of the segment within the dive.</param>
    public DiveSegment(Depth depth, TimeSpan duration, GasMixture gas, SegmentKind kind)
    {
        Depth = depth;
        Duration = duration;
        Gas = gas;
        Kind = kind;
    }

    /// <summary>Gets the depth at which the segment is held, or the depth reached at its end.</summary>
    public Depth Depth { get; }

    /// <summary>Gets the length of time the segment lasts.</summary>
    public TimeSpan Duration { get; }

    /// <summary>Gets the breathing gas used during the segment.</summary>
    public GasMixture Gas { get; }

    /// <summary>Gets the role of the segment within the dive.</summary>
    public SegmentKind Kind { get; }
}