namespace Bitenovac.DecompressionAlgorithms.BuhlmannZhl16c;

/// <summary>
/// Holds the published Bühlmann ZH-L16C compartment coefficients: the perfusion
/// half-times and the linear M-value coefficients <c>a</c> and <c>b</c> for nitrogen and
/// helium in each of the sixteen tissue compartments. Compartment 1 uses the 1b variant
/// (5.0-minute nitrogen half-time), the variant recommended for repetitive dive planning.
/// The <c>a</c> coefficients are stored in millibars (the published bar values multiplied
/// by 1000) so that all model arithmetic stays in the canonical millibar unit; the
/// <c>b</c> coefficients are dimensionless. The tables are backed by static arrays so
/// that reading a span never allocates, in every build configuration.
/// </summary>
internal static class Zhl16cCoefficients
{
    /// <summary>The number of tissue compartments in the ZH-L16 model.</summary>
    public const int CompartmentCount = 16;

    private static readonly double[] NitrogenHalfTimeTable =
    [
        5.0, 8.0, 12.5, 18.5, 27.0, 38.3, 54.3, 77.0,
        109.0, 146.0, 187.0, 239.0, 305.0, 390.0, 498.0, 635.0
    ];

    private static readonly double[] NitrogenATable =
    [
        1169.6, 1000.0, 861.8, 756.2, 620.0, 504.3, 441.0, 400.0,
        375.0, 350.0, 329.5, 306.5, 283.5, 261.0, 248.0, 232.7
    ];

    private static readonly double[] NitrogenBTable =
    [
        0.5578, 0.6514, 0.7222, 0.7825, 0.8126, 0.8434, 0.8693, 0.8910,
        0.9092, 0.9222, 0.9319, 0.9403, 0.9477, 0.9544, 0.9602, 0.9653
    ];

    private static readonly double[] HeliumHalfTimeTable =
    [
        1.88, 3.02, 4.72, 6.99, 10.21, 14.48, 20.53, 29.11,
        41.20, 55.19, 70.69, 90.34, 115.29, 147.42, 188.24, 240.03
    ];

    private static readonly double[] HeliumATable =
    [
        1618.9, 1383.0, 1191.9, 1045.8, 922.0, 820.5, 730.5, 650.2,
        595.0, 554.5, 533.3, 518.9, 518.1, 517.6, 517.2, 511.9
    ];

    private static readonly double[] HeliumBTable =
    [
        0.4770, 0.5747, 0.6527, 0.7223, 0.7582, 0.7957, 0.8279, 0.8553,
        0.8757, 0.8903, 0.8997, 0.9073, 0.9122, 0.9171, 0.9217, 0.9267
    ];

    /// <summary>Gets the nitrogen half-times, in minutes.</summary>
    public static ReadOnlySpan<double> NitrogenHalfTimeMinutes => NitrogenHalfTimeTable;

    /// <summary>Gets the nitrogen M-value coefficient <c>a</c>, in millibars.</summary>
    public static ReadOnlySpan<double> NitrogenAMillibar => NitrogenATable;

    /// <summary>Gets the dimensionless nitrogen M-value coefficient <c>b</c>.</summary>
    public static ReadOnlySpan<double> NitrogenB => NitrogenBTable;

    /// <summary>Gets the helium half-times, in minutes.</summary>
    public static ReadOnlySpan<double> HeliumHalfTimeMinutes => HeliumHalfTimeTable;

    /// <summary>Gets the helium M-value coefficient <c>a</c>, in millibars.</summary>
    public static ReadOnlySpan<double> HeliumAMillibar => HeliumATable;

    /// <summary>Gets the dimensionless helium M-value coefficient <c>b</c>.</summary>
    public static ReadOnlySpan<double> HeliumB => HeliumBTable;
}
