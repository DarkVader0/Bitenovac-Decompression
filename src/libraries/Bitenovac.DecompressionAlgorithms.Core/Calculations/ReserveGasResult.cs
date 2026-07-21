namespace Bitenovac.DecompressionAlgorithms.Core.Calculations;

/// <summary>
/// Represents the outcome of the reserve gas calculation over all cylinders: the
/// per-cylinder reserve assessments and a summary indicating whether every cylinder
/// satisfies its reserve requirement. Whether the reserve is met is an expected planning
/// outcome rather than an error, and is reported through the <see cref="AllSatisfied" />
/// flag rather than by throwing.
/// </summary>
/// <remarks>Instances are immutable; the per-cylinder statuses are copied on construction.</remarks>
public sealed class ReserveGasResult
{
    private readonly CylinderReserveStatus[] _cylinderStatuses;

    /// <summary>Initializes a new instance of the <see cref="ReserveGasResult" /> class.</summary>
    /// <param name="cylinderStatuses">The reserve assessment for each cylinder.</param>
    /// <exception cref="ArgumentNullException"><paramref name="cylinderStatuses" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentException"><paramref name="cylinderStatuses" /> contains a <see langword="null" /> entry.</exception>
    public ReserveGasResult(IEnumerable<CylinderReserveStatus> cylinderStatuses)
    {
        ArgumentNullException.ThrowIfNull(cylinderStatuses);

        var statuses = cylinderStatuses.ToArray();
        if (Array.Exists(statuses, static status => status is null))
        {
            throw new ArgumentException("The cylinder statuses must not contain a null entry.", nameof(cylinderStatuses));
        }

        _cylinderStatuses = statuses;
    }

    /// <summary>Gets the reserve assessment for each cylinder, in the order the cylinders were supplied.</summary>
    public IReadOnlyList<CylinderReserveStatus> CylinderStatuses => _cylinderStatuses;

    /// <summary>
    /// Gets a value indicating whether every cylinder satisfies its reserve requirement.
    /// A dive whose plan does not satisfy the reserve is a valid, expected result and is
    /// reported here rather than by throwing.
    /// </summary>
    public bool AllSatisfied => Array.TrueForAll(_cylinderStatuses, static status => status.IsSatisfied);
}