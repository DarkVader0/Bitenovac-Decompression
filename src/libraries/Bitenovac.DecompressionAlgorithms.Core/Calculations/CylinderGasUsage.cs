using Bitenovac.DecompressionAlgorithms.Core.Equipment;
using Bitenovac.DecompressionAlgorithms.Units;

namespace Bitenovac.DecompressionAlgorithms.Core.Calculations;

/// <summary>
/// Represents the gas consumed from a single cylinder over a planned dive.
/// </summary>
/// <remarks>
/// The usage records the volume of free gas consumed and the cylinder pressure remaining at the
/// end of the dive. It is produced by planning rather than supplied as input.
/// </remarks>
public sealed class CylinderGasUsage
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CylinderGasUsage"/> class.
    /// </summary>
    /// <param name="cylinder">The cylinder to which this usage relates.</param>
    /// <param name="gasUsed">
    /// The volume of free gas consumed from the cylinder over the dive, measured at surface conditions.
    /// </param>
    /// <param name="endPressure">The pressure remaining in the cylinder at the end of the dive.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="gasUsed"/> is negative, or <paramref name="endPressure"/> is negative or exceeds
    /// the cylinder's start pressure.
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

    /// <summary>
    /// Gets the cylinder to which this usage relates.
    /// </summary>
    public Cylinder Cylinder { get; }

    /// <summary>
    /// Gets the volume of free gas consumed from the cylinder over the dive.
    /// </summary>
    /// <value>
    /// The consumed volume, measured at surface conditions.
    /// </value>
    public Volume GasUsed { get; }

    /// <summary>
    /// Gets the pressure remaining in the cylinder at the end of the dive.
    /// </summary>
    public Pressure EndPressure { get; }

    /// <summary>
    /// Gets the pressure in the cylinder at the start of the dive.
    /// </summary>
    public Pressure StartPressure => Cylinder.StartPressure;

    /// <summary>
    /// Gets a value that indicates whether the cylinder was overbreathed.
    /// </summary>
    /// <value>
    /// <see langword="true"/> if the end pressure fell to zero and the demand for gas could not be
    /// met; otherwise, <see langword="false"/>.
    /// </value>
    public bool IsExhausted => EndPressure.InBar <= 0.0;
}