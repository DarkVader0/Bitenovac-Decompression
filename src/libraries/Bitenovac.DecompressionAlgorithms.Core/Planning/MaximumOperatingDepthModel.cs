namespace Bitenovac.DecompressionAlgorithms.Core.Planning;

/// <summary>
/// Specifies how the maximum operating depth of a gas is derived from its oxygen partial
/// pressure limit, and therefore the depth at which the gas becomes breathable.
/// </summary>
/// <remarks>
/// The two models disagree by a few tens of centimeters, which is immaterial to the
/// exposure but decides which decompression stop a gas switch lands on. Pure oxygen at a
/// limit of 1.6 bar is the clearest case: the physical depth is roughly 5.9 m in salt
/// water at sea level, one stop short of the six meter stop the tables and the training
/// agencies name, whereas the simplified model places it at exactly 6 m.
/// </remarks>
public enum MaximumOperatingDepthModel
{
    /// <summary>
    /// Derives the depth from the surface pressure and the water density configured for the
    /// dive, so that the oxygen partial pressure limit is honored exactly at that depth. A
    /// gas is breathable only where its actual partial pressure of oxygen is within the
    /// limit.
    /// </summary>
    Realistic,

    /// <summary>
    /// Uses the convention taught by the training agencies and printed on cylinder labels,
    /// in which the depth in meters is ten times the ratio of the limit to the oxygen
    /// fraction, less ten. Sea level and ten meters of water per bar are assumed regardless
    /// of the configured environment, placing oxygen at 1.6 bar on exactly 6 m and nitrox 50
    /// on exactly 22 m. The actual partial pressure at those depths may exceed the limit by
    /// a fraction of a percent.
    /// </summary>
    Simplified
}