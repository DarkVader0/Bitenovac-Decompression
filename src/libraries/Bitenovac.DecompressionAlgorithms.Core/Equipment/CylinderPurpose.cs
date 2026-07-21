namespace Bitenovac.DecompressionAlgorithms.Core.Equipment;

/// <summary>
/// Identifies the intended role of a cylinder within a dive plan, which governs
/// when its gas is breathed and how it is considered during gas planning.
/// </summary>
public enum CylinderPurpose
{
    /// <summary>
    /// Gas carried for the bottom (deepest) portion of the dive.
    /// </summary>
    BottomGas,

    /// <summary>
    /// Gas carried to accelerate decompression, breathed during the ascent.
    /// </summary>
    DecoGas,

    /// <summary>
    /// Gas used to fill and flush the breathing loop of a rebreather.
    /// </summary>
    Diluent,

    /// <summary>
    /// Reserve open-circuit gas carried to safely abort a rebreather dive.
    /// </summary>
    Bailout
}