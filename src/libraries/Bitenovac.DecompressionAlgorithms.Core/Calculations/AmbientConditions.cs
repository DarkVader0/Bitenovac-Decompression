using Bitenovac.DecompressionAlgorithms.Core.Environment;
using Bitenovac.DecompressionAlgorithms.Core.Planning;
using Bitenovac.DecompressionAlgorithms.Units;

namespace Bitenovac.DecompressionAlgorithms.Core.Calculations;

/// <summary>
/// Provides methods for converting between depth and absolute ambient pressure.
/// </summary>
/// <remarks>
/// The ambient pressure at a depth is the surface pressure plus the hydrostatic pressure of the
/// water column above the diver, and the depth at an ambient pressure is the inverse of that
/// relation. The conversion depends only on the surface pressure and the salinity of the
/// <see cref="DivePlanSettings"/>, and is shared by every decompression model.
/// </remarks>
public static class AmbientConditions
{
    /// <summary>
    /// Returns the absolute ambient pressure at the specified depth.
    /// </summary>
    /// <param name="settings">The settings supplying the surface pressure and the salinity.</param>
    /// <param name="depth">The depth below the surface.</param>
    /// <returns>The surface pressure plus the hydrostatic pressure of the water column.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="settings"/> is <see langword="null"/>.</exception>
    public static Pressure PressureAtDepth(DivePlanSettings settings, Depth depth)
    {
        ArgumentNullException.ThrowIfNull(settings);

        var hydrostaticMillibar = PhysicalConstants.HydrostaticPressureMillibar(settings.Salinity, depth.InMeter);
        return Pressure.FromMillibar(settings.SurfacePressure.InMillibar + hydrostaticMillibar);
    }

    /// <summary>
    /// Returns the depth at which the specified absolute ambient pressure prevails.
    /// </summary>
    /// <param name="settings">The settings supplying the surface pressure and the salinity.</param>
    /// <param name="ambient">The absolute ambient pressure.</param>
    /// <returns>The depth of the water column producing <paramref name="ambient"/>, or zero at the surface.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="settings"/> is <see langword="null"/>.</exception>
    /// <remarks>
    /// An ambient pressure at or below the surface pressure maps to the surface, so the result is
    /// never negative.
    /// </remarks>
    public static Depth DepthAtPressure(DivePlanSettings settings, Pressure ambient)
    {
        ArgumentNullException.ThrowIfNull(settings);

        var excessMillibar = ambient.InMillibar - settings.SurfacePressure.InMillibar;
        if (excessMillibar <= 0.0)
        {
            return Depth.Zero;
        }

        var millibarPerMeter = PhysicalConstants.HydrostaticPressureMillibar(settings.Salinity, 1.0);
        return Depth.FromMeter(excessMillibar / millibarPerMeter);
    }
}