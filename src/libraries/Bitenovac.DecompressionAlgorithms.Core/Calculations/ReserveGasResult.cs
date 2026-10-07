namespace Bitenovac.DecompressionAlgorithms.Core.Calculations;

/// <summary>
/// Represents the outcome of the reserve gas calculation over all cylinders.
/// </summary>
/// <remarks>
/// The result holds the per-cylinder reserve assessments and a summary of whether every cylinder
/// satisfies its reserve requirement. Whether the reserve is met is an expected planning outcome
/// rather than an error, and is reported through <see cref="AllSatisfied"/> rather than by throwing.
/// The per-cylinder statuses are copied on construction.
/// </remarks>
public sealed class ReserveGasResult
{
    private readonly CylinderReserveStatus[] _cylinderStatuses;

    /// <summary>
    /// Initializes a new instance of the <see cref="ReserveGasResult"/> class.
    /// </summary>
    /// <param name="cylinderStatuses">The reserve assessment for each cylinder.</param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="cylinderStatuses"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="cylinderStatuses"/> contains a <see langword="null"/> entry.
    /// </exception>
    public ReserveGasResult(IEnumerable<CylinderReserveStatus> cylinderStatuses)
    {
        ArgumentNullException.ThrowIfNull(cylinderStatuses);

        var statuses = cylinderStatuses.ToArray();
        if (Array.Exists(statuses, static status => status is null))
        {
            throw new ArgumentException("The cylinder statuses must not contain a null entry.",
                nameof(cylinderStatuses));
        }

        _cylinderStatuses = statuses;
    }

    /// <summary>
    /// Gets the reserve assessment for each cylinder.
    /// </summary>
    /// <value>
    /// The assessments, in the order the cylinders were supplied.
    /// </value>
    public IReadOnlyList<CylinderReserveStatus> CylinderStatuses => _cylinderStatuses;

    /// <summary>
    /// Gets a value that indicates whether every cylinder satisfies its reserve requirement.
    /// </summary>
    /// <value>
    /// <see langword="true"/> if every cylinder satisfies its reserve requirement; otherwise,
    /// <see langword="false"/>. A plan that does not satisfy the reserve is a valid, expected result
    /// and is reported here rather than by throwing.
    /// </value>
    public bool AllSatisfied => Array.TrueForAll(_cylinderStatuses, static status => status.IsSatisfied);
}