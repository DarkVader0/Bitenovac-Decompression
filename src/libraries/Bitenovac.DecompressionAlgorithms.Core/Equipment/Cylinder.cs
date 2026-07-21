using Bitenovac.DecompressionAlgorithms.Core.Gas;
using Bitenovac.DecompressionAlgorithms.Units;

namespace Bitenovac.DecompressionAlgorithms.Core.Equipment;

/// <summary>
/// Represents the definition of a diving cylinder: the gas mixture it contains, its
/// capacity, the pressure to which it is filled at the start of the
/// dive, and its intended purpose. The pressure remaining during the dive is tracked by
/// the algorithm and is not part of this definition.
/// </summary>
/// <remarks>Instances are immutable.</remarks>
public readonly struct Cylinder
{
    /// <summary>Initializes a new instance of the <see cref="Cylinder" /> class.</summary>
    /// <param name="gas">The gas mixture the cylinder contains.</param>
    /// <param name="size">The water (internal) capacity of the cylinder.</param>
    /// <param name="startPressure">The pressure to which the cylinder is filled at the start of the dive.</param>
    /// <param name="purpose">The intended role of the cylinder within the dive plan.</param>
    public Cylinder(GasMixture gas, Volume size, Pressure startPressure, CylinderPurpose purpose)
    {
        Gas = gas;
        Size = size;
        StartPressure = startPressure;
        Purpose = purpose;
    }

    /// <summary>Gets the gas mixture the cylinder contains.</summary>
    public GasMixture Gas { get; }

    /// <summary>Gets the water (internal) capacity of the cylinder.</summary>
    public Volume Size { get; }

    /// <summary>Gets the pressure to which the cylinder is filled at the start of the dive.</summary>
    public Pressure StartPressure { get; }

    /// <summary>Gets the intended role of the cylinder within the dive plan.</summary>
    public CylinderPurpose Purpose { get; }

    /// <summary>
    /// Gets the total volume of gas the cylinder holds at its start pressure, measured
    /// at surface (one bar) conditions.
    /// </summary>
    /// <remarks>
    /// This is the free gas capacity, obtained by multiplying the water capacity by the
    /// start pressure expressed in bar. It assumes ideal-gas behaviour.
    /// </remarks>
    /// <returns>The free gas volume available at the start of the dive.</returns>
    public Volume StartGasVolume(Pressure surfacePressure) => Size * (StartPressure / surfacePressure);
}