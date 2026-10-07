using Bitenovac.DecompressionAlgorithms.Core.Planning;
using Bitenovac.DecompressionAlgorithms.Units;

namespace Bitenovac.DecompressionAlgorithms.Core.Equipment;

/// <summary>
/// Represents the definition of a diving cylinder: the gas mixture it contains, its capacity, the
/// pressure to which it is filled at the start of the dive, and its intended purpose.
/// </summary>
/// <remarks>
/// The pressure remaining during the dive is tracked by the algorithm and is not part of this
/// definition.
/// </remarks>
public readonly struct Cylinder
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Cylinder"/> structure.
    /// </summary>
    /// <param name="gas">The gas mixture the cylinder contains.</param>
    /// <param name="size">The water (internal) capacity of the cylinder.</param>
    /// <param name="startPressure">The pressure to which the cylinder is filled at the start of the dive.</param>
    /// <param name="purpose">The intended role of the cylinder within the dive plan.</param>
    public Cylinder(GasMixture gas,
        Volume size,
        Pressure startPressure,
        CylinderPurpose purpose)
    {
        Gas = gas;
        Size = size;
        StartPressure = startPressure;
        Purpose = purpose;
    }

    /// <summary>
    /// Gets the gas mixture the cylinder contains.
    /// </summary>
    public GasMixture Gas { get; }

    /// <summary>
    /// Gets the water (internal) capacity of the cylinder.
    /// </summary>
    public Volume Size { get; }

    /// <summary>
    /// Gets the pressure to which the cylinder is filled at the start of the dive.
    /// </summary>
    public Pressure StartPressure { get; }

    /// <summary>
    /// Gets the intended role of the cylinder within the dive plan.
    /// </summary>
    public CylinderPurpose Purpose { get; }

    /// <summary>
    /// Computes the total volume of free gas the cylinder holds at its start pressure, referenced
    /// to the given surface pressure.
    /// </summary>
    /// <param name="surfacePressure">
    /// The surface (reference) pressure against which the free gas volume is measured, typically
    /// the atmospheric pressure of the dive environment.
    /// </param>
    /// <returns>The free gas volume available at the start of the dive.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="surfacePressure"/> is not greater than zero.
    /// </exception>
    /// <remarks>
    /// The free gas volume is the water capacity multiplied by the ratio of the start pressure to
    /// the surface pressure, and therefore assumes ideal-gas behavior. The pressure ratio is
    /// dimensionless, so the result is independent of the unit in which the pressures are
    /// expressed.
    /// </remarks>
    public Volume StartGasVolume(Pressure surfacePressure)
    {
        if (surfacePressure.InMillibar <= 0.0)
        {
            throw new ArgumentOutOfRangeException(nameof(surfacePressure), surfacePressure.InMillibar,
                "The surface pressure must be greater than zero.");
        }

        return Size * (StartPressure / surfacePressure);
    }
}