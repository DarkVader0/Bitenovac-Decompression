namespace Bitenovac.DecompressionAlgorithms.Units;

/// <summary>
/// Provides the unit conversion factors shared by <see cref="Depth"/>, <see cref="Pressure"/>, and
/// <see cref="Volume"/>.
/// </summary>
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