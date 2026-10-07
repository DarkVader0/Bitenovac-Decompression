using Bitenovac.DecompressionAlgorithms.Core.Equipment;
using Bitenovac.DecompressionAlgorithms.Core.Planning;
using Bitenovac.DecompressionAlgorithms.Units;

namespace Bitenovac.DecompressionAlgorithms.Core.Calculations;

/// <summary>
/// Provides methods for choosing the breathing gas at a given depth.
/// </summary>
/// <remarks>
/// From the cylinders available to the diver, the gas with the highest oxygen content whose partial
/// pressure of oxygen at the depth does not exceed the permitted limit is chosen. Breathing the
/// richest permissible gas minimizes inert gas uptake and accelerates decompression. The selection
/// depends only on the gases carried, the ambient pressure and the oxygen partial pressure limit,
/// and is shared by every decompression model.
/// </remarks>
public static class GasSelector
{
    /// <summary>
    /// The meters of water per bar assumed by <see cref="MaximumOperatingDepthModel.Simplified"/>,
    /// which ignores the configured salinity.
    /// </summary>
    private const double SimplifiedMetersPerBar = 10.0;

    /// <summary>
    /// The surface pressure in bar assumed by <see cref="MaximumOperatingDepthModel.Simplified"/>,
    /// which ignores the configured surface pressure.
    /// </summary>
    private const double SimplifiedSurfaceBar = 1.0;

    /// <summary>
    /// The depth tolerance, in meters, within which a depth counts as being at a maximum operating
    /// depth rather than beyond it, absorbing the rounding of the conversion.
    /// </summary>
    private const double DepthToleranceMeter = 1e-9;

    /// <summary>
    /// Selects the richest breathing gas from the available cylinders whose partial pressure of
    /// oxygen at the specified ambient pressure does not exceed the permitted limit.
    /// </summary>
    /// <param name="cylinders">The cylinders available to the diver.</param>
    /// <param name="ambient">The absolute ambient pressure at the depth for which a gas is being selected.</param>
    /// <param name="maxPo2">The maximum permitted partial pressure of oxygen at that depth.</param>
    /// <param name="requiredPurpose">
    /// The role the selected cylinder must be carried for, so a rebreather draws only on its diluent
    /// supply; or <see langword="null"/> to make every cylinder a candidate.
    /// </param>
    /// <returns>The cylinder holding the richest gas that is breathable within the limit at the given depth.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="cylinders"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="cylinders"/> is empty.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="maxPo2"/> is not greater than zero.</exception>
    /// <exception cref="InvalidOperationException">
    /// No available gas is breathable within the limit at the given depth.
    /// </exception>
    /// <remarks>
    /// Ties on oxygen fraction are resolved in favor of the higher helium content, which is less
    /// narcotic.
    /// </remarks>
    public static Cylinder SelectRichestGas(IReadOnlyList<Cylinder> cylinders,
        Pressure ambient,
        Pressure maxPo2,
        CylinderPurpose? requiredPurpose = null)
    {
        ArgumentNullException.ThrowIfNull(cylinders);

        if (cylinders.Count == 0)
        {
            throw new ArgumentException("At least one cylinder must be available.", nameof(cylinders));
        }

        if (maxPo2.InMillibar <= 0.0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxPo2), maxPo2.InMillibar,
                "The maximum partial pressure of oxygen must be greater than zero.");
        }

        var found = false;
        Cylinder best = default;

        for (var i = 0; i < cylinders.Count; i++)
        {
            var cylinder = cylinders[i];
            if (!IsAvailableFor(cylinder, requiredPurpose))
            {
                continue;
            }

            if (cylinder.Gas.PartialPressureO2(ambient).InMillibar > maxPo2.InMillibar)
            {
                continue;
            }

            if (!found
                || cylinder.Gas.FractionO2 > best.Gas.FractionO2
                || (cylinder.Gas.FractionO2.Equals(best.Gas.FractionO2)
                    && cylinder.Gas.FractionHe > best.Gas.FractionHe))
            {
                best = cylinder;
                found = true;
            }
        }

        if (!found)
        {
            throw new InvalidOperationException(
                "No available gas is breathable within the oxygen partial pressure limit at the given depth.");
        }

        return best;
    }

    /// <summary>
    /// Determines whether a cylinder may supply a segment breathed through the specified apparatus.
    /// </summary>
    /// <param name="cylinder">The cylinder being considered.</param>
    /// <param name="requiredPurpose">
    /// The role the supply must be carried for, or <see langword="null"/> for open circuit.
    /// </param>
    /// <returns>
    /// <see langword="true"/> if the cylinder may supply the segment; otherwise, <see langword="false"/>.
    /// </returns>
    /// <remarks>
    /// A rebreather is fed from its diluent alone. Open circuit may draw on any cylinder except the
    /// oxygen supply of a rebreather, which feeds the loop and carries no second stage to breathe from.
    /// </remarks>
    public static bool IsAvailableFor(Cylinder cylinder, CylinderPurpose? requiredPurpose) =>
        requiredPurpose is { } purpose
            ? cylinder.Purpose == purpose
            : cylinder.Purpose != CylinderPurpose.Oxygen;

    /// <summary>
    /// Returns the maximum operating depth of a gas, expressed as an ambient pressure.
    /// </summary>
    /// <param name="gas">The gas whose maximum operating depth is required.</param>
    /// <param name="maxPo2">The maximum permitted partial pressure of oxygen.</param>
    /// <returns>The ambient pressure at which the gas reaches its oxygen partial pressure limit.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="maxPo2"/> is not greater than zero, or <paramref name="gas"/> contains no oxygen.
    /// </exception>
    /// <remarks>
    /// The result is the shallowest ambient pressure at which the gas's partial pressure of oxygen
    /// reaches the permitted limit, which is the limit divided by the oxygen fraction of the gas.
    /// </remarks>
    public static Pressure MaxOperatingPressure(GasMixture gas, Pressure maxPo2)
    {
        if (maxPo2.InMillibar <= 0.0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxPo2), maxPo2.InMillibar,
                "The maximum partial pressure of oxygen must be greater than zero.");
        }

        if (gas.FractionO2 <= 0.0)
        {
            throw new ArgumentOutOfRangeException(nameof(gas), gas.FractionO2,
                "The gas must contain oxygen to have a maximum operating depth.");
        }

        return Pressure.FromMillibar(maxPo2.InMillibar / gas.FractionO2);
    }

    /// <summary>
    /// Returns the maximum operating depth of a gas, in meters.
    /// </summary>
    /// <param name="gas">The gas whose maximum operating depth is required.</param>
    /// <param name="maxPo2">The maximum permitted partial pressure of oxygen.</param>
    /// <param name="settings">The settings supplying the environment and the depth model.</param>
    /// <returns>
    /// The deepest depth, in meters, at which the gas's partial pressure of oxygen is within the
    /// permitted limit. The result is never negative.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="settings"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="maxPo2"/> is not greater than zero, or <paramref name="gas"/> contains no oxygen.
    /// </exception>
    /// <remarks>
    /// Under <see cref="MaximumOperatingDepthModel.Realistic"/>, the depth is the one at which the limit
    /// is reached in the configured environment. Under <see cref="MaximumOperatingDepthModel.Simplified"/>,
    /// it is the value the training agencies publish, ten meters per bar above a one bar surface.
    /// </remarks>
    public static double MaxOperatingDepthMeter(GasMixture gas,
        Pressure maxPo2,
        DivePlanSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        var operatingPressure = MaxOperatingPressure(gas, maxPo2);

        var depthMeter = settings.MaximumOperatingDepthModel is MaximumOperatingDepthModel.Simplified
            ? (operatingPressure.InBar - SimplifiedSurfaceBar) * SimplifiedMetersPerBar
            : AmbientConditions.DepthAtPressure(settings, operatingPressure).InMeter;

        return Math.Max(depthMeter, 0.0);
    }

    /// <summary>
    /// Determines whether a gas may be breathed at the specified depth within the permitted oxygen
    /// partial pressure limit.
    /// </summary>
    /// <param name="gas">The gas being considered.</param>
    /// <param name="depthMeter">The depth, in meters, at which the gas would be breathed.</param>
    /// <param name="maxPo2">The maximum permitted partial pressure of oxygen.</param>
    /// <param name="settings">The settings supplying the environment and the depth model.</param>
    /// <returns>
    /// <see langword="true"/> if the gas may be breathed at the depth; otherwise, <see langword="false"/>.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="settings"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="maxPo2"/> is not greater than zero.</exception>
    /// <remarks>
    /// Under <see cref="MaximumOperatingDepthModel.Realistic"/>, the actual partial pressure at the depth
    /// is compared against the limit. Under <see cref="MaximumOperatingDepthModel.Simplified"/>, the depth
    /// is compared against the published maximum operating depth, which admits the fraction of a
    /// percent by which the rounded depth overshoots the limit.
    /// </remarks>
    public static bool IsBreathableAt(GasMixture gas,
        double depthMeter,
        Pressure maxPo2,
        DivePlanSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        if (maxPo2.InMillibar <= 0.0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxPo2), maxPo2.InMillibar,
                "The maximum partial pressure of oxygen must be greater than zero.");
        }

        return settings.MaximumOperatingDepthModel is MaximumOperatingDepthModel.Simplified
            ? gas.FractionO2 <= 0.0
              || depthMeter <= MaxOperatingDepthMeter(gas, maxPo2, settings) + DepthToleranceMeter
            : gas.PartialPressureO2(AmbientConditions.PressureAtDepth(settings, Depth.FromMeter(depthMeter)))
                .InMillibar <= maxPo2.InMillibar;
    }

    /// <summary>
    /// Selects the richest breathing gas from the available cylinders that may be breathed at the
    /// specified depth.
    /// </summary>
    /// <param name="cylinders">The cylinders available to the diver.</param>
    /// <param name="depthMeter">The depth, in meters, for which a gas is being selected.</param>
    /// <param name="maxPo2">The maximum permitted partial pressure of oxygen at that depth.</param>
    /// <param name="settings">The settings supplying the environment and the depth model.</param>
    /// <param name="requiredPurpose">
    /// The role the selected cylinder must be carried for, so a rebreather draws only on its diluent
    /// supply; or <see langword="null"/> to make every cylinder a candidate.
    /// </param>
    /// <returns>The cylinder holding the richest gas that is breathable at the given depth.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="cylinders"/> or <paramref name="settings"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="ArgumentException"><paramref name="cylinders"/> is empty.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="maxPo2"/> is not greater than zero.</exception>
    /// <exception cref="InvalidOperationException">No available gas is breathable at the given depth.</exception>
    /// <remarks>
    /// Breathability is decided by <see cref="IsBreathableAt"/>, which applies the maximum operating
    /// depth model carried by the settings. Ties are resolved in favor of the higher helium content,
    /// which is less narcotic.
    /// </remarks>
    public static Cylinder SelectRichestGasAt(IReadOnlyList<Cylinder> cylinders,
        double depthMeter,
        Pressure maxPo2,
        DivePlanSettings settings,
        CylinderPurpose? requiredPurpose = null)
    {
        ArgumentNullException.ThrowIfNull(cylinders);
        ArgumentNullException.ThrowIfNull(settings);

        if (cylinders.Count == 0)
        {
            throw new ArgumentException("At least one cylinder must be available.", nameof(cylinders));
        }

        if (maxPo2.InMillibar <= 0.0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxPo2), maxPo2.InMillibar,
                "The maximum partial pressure of oxygen must be greater than zero.");
        }

        var found = false;
        Cylinder best = default;

        for (var i = 0; i < cylinders.Count; i++)
        {
            var cylinder = cylinders[i];
            if (!IsAvailableFor(cylinder, requiredPurpose)
                || !IsBreathableAt(cylinder.Gas, depthMeter, maxPo2, settings))
            {
                continue;
            }

            if (!found
                || cylinder.Gas.FractionO2 > best.Gas.FractionO2
                || (cylinder.Gas.FractionO2.Equals(best.Gas.FractionO2)
                    && cylinder.Gas.FractionHe > best.Gas.FractionHe))
            {
                best = cylinder;
                found = true;
            }
        }

        if (!found)
        {
            throw new InvalidOperationException(
                "No available gas is breathable within the oxygen partial pressure limit at the given depth.");
        }

        return best;
    }
}