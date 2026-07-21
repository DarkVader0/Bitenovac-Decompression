using Bitenovac.DecompressionAlgorithms.Core.Equipment;
using Bitenovac.DecompressionAlgorithms.Units;

namespace Bitenovac.DecompressionAlgorithms.Core.Planning;

/// <summary>
/// Represents the outcome of gas consumption for a single cylinder over the course of a
/// planned dive: the cylinder to which the usage relates, the volume of free gas
/// consumed, and the cylinder pressure remaining at the end of the dive. This is a
/// result of planning and is produced by the algorithm rather than supplied as input.
/// </summary>
/// <remarks>Instances are immutable.</remarks>
public sealed class CylinderGasUsage
{
    /// <summary>Initializes a new instance of the <see cref="CylinderGasUsage" /> class.</summary>
    /// <param name="cylinder">The cylinder to which this usage relates.</param>
    /// <param name="gasUsed">The volume of free gas consumed from the cylinder over the dive, measured at surface conditions.</param>
    /// <param name="endPressure">The pressure remaining in the cylinder at the end of the dive.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="gasUsed" /> is negative, or <paramref name="endPressure" /> is negative or exceeds the cylinder's start
    /// pressure.
    /// </exception>
    public CylinderGasUsage(Cylinder cylinder,
        Volume gasUsed,
        Pressure endPressure)
    {
        if (gasUsed.InLiter < 0.0)
        {
            throw new ArgumentOutOfRangeException(nameof(gasUsed), gasUsed.InLiter,
                "The gas used must not be negative.");
        }

        if (endPressure.InBar < 0.0)
        {
            throw new ArgumentOutOfRangeException(nameof(endPressure), endPressure.InBar,
                "The end pressure must not be negative.");
        }

        if (endPressure.InBar > cylinder.StartPressure.InBar)
        {
            throw new ArgumentOutOfRangeException(nameof(endPressure), endPressure.InBar,
                "The end pressure must not exceed the cylinder's start pressure.");
        }

        Cylinder = cylinder;
        GasUsed = gasUsed;
        EndPressure = endPressure;
    }

    /// <summary>Gets the cylinder to which this usage relates.</summary>
    public Cylinder Cylinder { get; }

    /// <summary>Gets the volume of free gas consumed from the cylinder over the dive, measured at surface conditions.</summary>
    public Volume GasUsed { get; }

    /// <summary>Gets the pressure remaining in the cylinder at the end of the dive.</summary>
    public Pressure EndPressure { get; }

    /// <summary>Gets the pressure in the cylinder at the start of the dive.</summary>
    public Pressure StartPressure => Cylinder.StartPressure;

    /// <summary>
    /// Gets a value indicating whether the cylinder was overbreathed, meaning the end
    /// pressure fell to zero and the demand for gas could not be met.
    /// </summary>
    public bool IsExhausted => EndPressure.InBar <= 0.0;
}