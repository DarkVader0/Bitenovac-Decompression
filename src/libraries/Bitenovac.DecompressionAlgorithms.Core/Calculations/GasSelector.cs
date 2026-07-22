using Bitenovac.DecompressionAlgorithms.Core.Equipment;
using Bitenovac.DecompressionAlgorithms.Core.Planning;
using Bitenovac.DecompressionAlgorithms.Units;

namespace Bitenovac.DecompressionAlgorithms.Core.Calculations;

/// <summary>
/// Provides the model-agnostic rule for choosing the breathing gas at a given depth: from
/// the cylinders available to the diver, the gas with the highest oxygen content whose
/// partial pressure of oxygen at that depth does not exceed the permitted limit is chosen.
/// Breathing the richest permissible gas minimizes inert gas uptake and accelerates
/// decompression. This selection depends only on the gases carried, the ambient pressure,
/// and the oxygen partial pressure limit, and is therefore identical for every
/// decompression model.
/// </summary>
public static class GasSelector
{
    /// <summary>
    /// Selects the richest breathing gas from the available cylinders whose partial
    /// pressure of oxygen at the given ambient pressure does not exceed the permitted
    /// limit. Among the cylinders that are permissible at the depth, the one whose gas has
    /// the highest oxygen fraction is chosen; ties are resolved in favor of the higher
    /// helium content, so that the least narcotic of two otherwise equivalent gases is
    /// preferred.
    /// </summary>
    /// <param name="cylinders">The cylinders available to the diver.</param>
    /// <param name="ambient">The absolute ambient pressure at the depth for which a gas is being selected.</param>
    /// <param name="maxPo2">The maximum permitted partial pressure of oxygen at that depth.</param>
    /// <returns>The cylinder holding the richest gas that is breathable within the limit at the given depth.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="cylinders" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentException"><paramref name="cylinders" /> is empty.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="maxPo2" /> is not greater than zero.</exception>
    /// <exception cref="InvalidOperationException">No available gas is breathable within the limit at the given depth.</exception>
    public static Cylinder SelectRichestGas(IReadOnlyList<Cylinder> cylinders,
        Pressure ambient,
        Pressure maxPo2)
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

        foreach (var cylinder in cylinders)
        {
            // The gas is permissible only if its oxygen partial pressure at this depth is
            // within the limit.
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
    /// Returns the maximum operating depth of a gas, being the shallowest ambient pressure
    /// at which the gas's partial pressure of oxygen reaches the permitted limit. Expressed
    /// as a pressure, this is the limit divided by the oxygen fraction of the gas.
    /// </summary>
    /// <param name="gas">The gas whose maximum operating depth is required.</param>
    /// <param name="maxPo2">The maximum permitted partial pressure of oxygen.</param>
    /// <returns>The ambient pressure at which the gas reaches its oxygen partial pressure limit.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="maxPo2" /> is not greater than zero, or <paramref name="gas" /> contains no oxygen.
    /// </exception>
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
}