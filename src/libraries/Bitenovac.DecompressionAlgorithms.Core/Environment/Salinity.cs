namespace Bitenovac.DecompressionAlgorithms.Core.Environment;

/// <summary>
/// Specifies the density model of the water, used to convert a depth into the
/// corresponding hydrostatic pressure.
/// </summary>
public enum Salinity
{
    /// <summary>
    /// Fresh water, as found in lakes and rivers (approximately 1000 kg/m³).
    /// </summary>
    Fresh,

    /// <summary>
    /// Seawater (approximately 1030 kg/m³).
    /// </summary>
    Salt,

    /// <summary>
    /// Brackish water, a mixture of fresh and seawater with an intermediate density.
    /// </summary>
    Brackish,

    /// <summary>
    /// The fixed water density defined by the EN 13319 standard, used by many
    /// dive computers so that displayed depths are comparable across devices.
    /// </summary>
    EN13319
}