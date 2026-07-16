namespace Bitenovac.DecompressionAlgorithms.Units;

/// <summary>
/// Provides the definitional unit conversion factors shared by the unit value types
/// (<see cref="Depth"/>, <see cref="Pressure"/>, <see cref="Volume"/>).
/// </summary>
/// <remarks>
/// These are exact, definitional conversions between a unit and the canonical internal
/// representation of each quantity (millimeters, millibars and milliliters). They are
/// intentionally limited to unit definitions and do not include physical or environmental constrains.
/// </remarks>
internal static class UnitConstants
{
    public const double MmPerMeter = 1000.0;
    public const double MmPerFoot = 304.8;

    public const double MbarPerBar = 1000.0;
    public const double MbarPerPsi = 68.94757293168;
    public const double MbarPerAta = 1013.25;

    public const double MlPerL = 1000.0;
    public const double MlPerCubicFoot = 28316.846592;
}