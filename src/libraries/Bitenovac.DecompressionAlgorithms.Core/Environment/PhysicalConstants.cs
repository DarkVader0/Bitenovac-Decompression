namespace Bitenovac.DecompressionAlgorithms.Core.Environment;

/// <summary>
/// Provides the physical constants used to model the diving environment: water
/// densities per <see cref="Salinity" />, standard gravity, and the barometric
/// relationship between altitude and atmospheric pressure.
/// </summary>
/// <remarks>
/// Pressures are expressed in millibars, matching the canonical internal unit of
/// <see cref="Units.Pressure" />. Water densities are in kilograms per cubic meter and
/// gravity in meters per second squared; the hydrostatic pressure of a water column is
/// obtained from density × gravity × depth-in-meters and then scaled from pascals to
/// millibars (1 millibar = 100 pascals).
/// </remarks>
internal static class PhysicalConstants
{
    /// <summary>Standard gravitational acceleration (m/s²).</summary>
    public const double GravityMetersPerSecondSquared = 9.80665;

    /// <summary>Number of pascals in one millibar.</summary>
    public const double PascalsPerMillibar = 100.0;

    /// <summary>Density of fresh water (kg/m³).</summary>
    public const double FreshWaterDensity = 1000.0;

    /// <summary>Density of seawater (kg/m³).</summary>
    public const double SaltWaterDensity = 1030.0;

    /// <summary>Density of brackish water (kg/m³), taken as the mean of fresh and salt.</summary>
    public const double BrackishWaterDensity = 1015.0;

    /// <summary>Fixed water density defined by the EN 13319 standard (kg/m³).</summary>
    public const double En13319WaterDensity = 1020.0;

    /// <summary>Standard atmospheric pressure at sea level (mbar).</summary>
    public const double SeaLevelPressureMillibar = 1013.25;

    /// <summary>Standard temperature lapse rate (K/m).</summary>
    public const double TemperatureLapseRate = 0.0065;

    /// <summary>Standard sea-level temperature (K).</summary>
    public const double SeaLevelTemperatureKelvin = 288.15;

    /// <summary>Molar mass of dry air (kg/mol).</summary>
    public const double MolarMassOfAir = 0.0289644;

    /// <summary>Universal gas constant (J/(mol·K)).</summary>
    public const double UniversalGasConstant = 8.31447;

    /// <summary>Gets the water density, in kilograms per cubic meter, for a given salinity.</summary>
    /// <param name="salinity">The salinity model of the water.</param>
    /// <returns>The corresponding water density in kilograms per cubic meter.</returns>
    public static double WaterDensity(Salinity salinity) => salinity switch
    {
        Salinity.Fresh => FreshWaterDensity,
        Salinity.Salt => SaltWaterDensity,
        Salinity.Brackish => BrackishWaterDensity,
        Salinity.EN13319 => En13319WaterDensity,
        _ => throw new ArgumentOutOfRangeException(nameof(salinity), salinity, null)
    };

    /// <summary>
    /// Returns the hydrostatic pressure, in millibars, of a water column of the given
    /// depth and salinity.
    /// </summary>
    /// <param name="salinity">The salinity model of the water.</param>
    /// <param name="depthMeters">The depth of the water column, in meters.</param>
    /// <returns>The hydrostatic pressure of the column, in millibars.</returns>
    public static double HydrostaticPressureMillibar(Salinity salinity, double depthMeters) =>
        WaterDensity(salinity) * GravityMetersPerSecondSquared * depthMeters / PascalsPerMillibar;

    /// <summary>
    /// Returns the atmospheric pressure, in millibars, at a given altitude above sea
    /// level using the barometric formula for the troposphere.
    /// </summary>
    /// <param name="altitudeMeters">The altitude above sea level, in meters.</param>
    /// <returns>The atmospheric pressure at that altitude, in millibars.</returns>
    public static double AtmosphericPressureAtAltitudeMillibar(double altitudeMeters)
    {
        var baseTerm = 1.0 - TemperatureLapseRate * altitudeMeters / SeaLevelTemperatureKelvin;
        var exponent = GravityMetersPerSecondSquared * MolarMassOfAir
                       / (UniversalGasConstant * TemperatureLapseRate);
        return SeaLevelPressureMillibar * Math.Pow(baseTerm, exponent);
    }
}