using Bitenovac.DecompressionAlgorithms.Units;

namespace Bitenovac.DecompressionAlgorithms.Core.Planning;

/// <summary>
/// Records a single inter-level ascent within a multi-level dive that would incur a
/// decompression obligation and is therefore not permitted. The diver planned to ascend
/// from a deeper working level to a shallower one, but the decompression model's ceiling at
/// that point was deeper than the intended depth, meaning a decompression stop would be
/// required before the shallower depth could be reached. The planner records the violation
/// and marks the plan invalid rather than silently omitting the required stop.
/// </summary>
/// <remarks>Instances are immutable.</remarks>
public sealed class AscentViolation
{
    /// <summary>Initializes a new instance of the <see cref="AscentViolation" /> class.</summary>
    /// <param name="fromDepth">The depth from which the inter-level ascent begins.</param>
    /// <param name="toDepth">The intended shallower depth of the inter-level ascent.</param>
    /// <param name="ceiling">
    /// The decompression ceiling at the point of the ascent, which is deeper than
    /// <paramref name="toDepth" />.
    /// </param>
    public AscentViolation(Depth fromDepth,
        Depth toDepth,
        Depth ceiling)
    {
        FromDepth = fromDepth;
        ToDepth = toDepth;
        Ceiling = ceiling;
    }

    /// <summary>Gets the depth from which the inter-level ascent begins.</summary>
    public Depth FromDepth { get; }

    /// <summary>Gets the intended shallower depth of the inter-level ascent.</summary>
    public Depth ToDepth { get; }

    /// <summary>Gets the decompression ceiling at the point of the ascent, which is deeper than the intended depth.</summary>
    public Depth Ceiling { get; }
}