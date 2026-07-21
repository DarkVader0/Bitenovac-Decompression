using Bitenovac.DecompressionAlgorithms.Core.Equipment;
using Bitenovac.DecompressionAlgorithms.Units;

namespace Bitenovac.DecompressionAlgorithms.Core.Calculations;

/// <summary>
/// Represents the reserve gas assessment for a single cylinder: how much gas must remain
/// as a reserve, how much is projected to remain at the end of the dive, and whether that
/// projected remainder satisfies the reserve requirement. The requirement differs by the
/// cylinder's role: a bottom-gas cylinder must retain enough gas to bring the team from
/// the deepest point of the dive to the next breathable gas, whereas a decompression-gas
/// cylinder must simply retain a fixed fraction of its capacity.
/// </summary>
/// <remarks>Instances are immutable and are produced by the reserve gas calculation.</remarks>
public sealed class CylinderReserveStatus
{
    /// <summary>Initializes a new instance of the <see cref="CylinderReserveStatus" /> class.</summary>
    /// <param name="cylinder">The cylinder to which this assessment relates.</param>
    /// <param name="requiredReserve">The volume of gas, measured at surface conditions, that must remain as a reserve.</param>
    /// <param name="projectedRemaining">
    /// The volume of gas, measured at surface conditions, projected to remain at the end of
    /// the dive.
    /// </param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="requiredReserve" /> or <paramref name="projectedRemaining" /> is negative.
    /// </exception>
    public CylinderReserveStatus(Cylinder cylinder,
        Volume requiredReserve,
        Volume projectedRemaining)
    {
        if (requiredReserve.InLiter < 0.0)
        {
            throw new ArgumentOutOfRangeException(nameof(requiredReserve), requiredReserve.InLiter,
                "The required reserve must not be negative.");
        }

        if (projectedRemaining.InLiter < 0.0)
        {
            throw new ArgumentOutOfRangeException(nameof(projectedRemaining), projectedRemaining.InLiter,
                "The projected remaining gas must not be negative.");
        }

        Cylinder = cylinder;
        RequiredReserve = requiredReserve;
        ProjectedRemaining = projectedRemaining;
    }

    /// <summary>Gets the cylinder to which this assessment relates.</summary>
    public Cylinder Cylinder { get; }

    /// <summary>Gets the volume of gas, measured at surface conditions, that must remain as a reserve.</summary>
    public Volume RequiredReserve { get; }

    /// <summary>Gets the volume of gas, measured at surface conditions, projected to remain at the end of the dive.</summary>
    public Volume ProjectedRemaining { get; }

    /// <summary>
    /// Gets a value indicating whether the projected remaining gas satisfies the reserve
    /// requirement, being <see langword="true" /> when the projected remainder is at least
    /// the required reserve.
    /// </summary>
    public bool IsSatisfied => ProjectedRemaining.InLiter >= RequiredReserve.InLiter;
}