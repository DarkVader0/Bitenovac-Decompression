namespace Bitenovac.DecompressionAlgorithms.Core.Planning;

/// <summary>
/// Classifies the role of a <see cref="DiveSegment"/> within a dive profile,
/// distinguishing planned movement, bottom time, and generated ascent phases.
/// </summary>
public enum SegmentKind
{
    /// <summary>
    /// A descent from a shallower to a deeper depth.
    /// </summary>
    Descent,
    /// <summary>
    /// Time spent at the bottom (deepest planned) portion of the dive.
    /// </summary>
    Bottom,
    /// <summary>
    /// An ascent from a deeper to a shallower depth.
    /// </summary>
    Ascent,
    /// <summary>
    /// A decompression stop held at a fixed depth for a required duration.
    /// </summary>
    Stop,
    /// <summary>
    /// A change of breathing gas, typically performed at a stop.
    /// </summary>
    GasSwitch
}

