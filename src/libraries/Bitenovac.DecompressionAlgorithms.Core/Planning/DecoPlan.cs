using Bitenovac.DecompressionAlgorithms.Core.Calculations;

namespace Bitenovac.DecompressionAlgorithms.Core.Planning;

/// <summary>
/// Represents the result of a decompression planning calculation: the fully expanded
/// dive profile including descent, working ascent, bottom, and decompression stop
/// segments, the gas consumed from each cylinder, the total runtime, the per-cylinder
/// reserve gas assessment, the accrued oxygen toxicity exposure, and whether the plan is
/// valid. A plan is invalid when one or more planned inter-level ascents would incur a
/// decompression obligation; such ascents are recorded so that the diver can amend the
/// profile.
/// </summary>
/// <remarks>Instances are immutable; the expanded segments, gas usage, and violations are copied on construction.</remarks>
public sealed class DecoPlan
{
    private readonly DiveSegment[] _expandedSegments;
    private readonly CylinderGasUsage[] _gasUsage;
    private readonly AscentViolation[] _violations;

    /// <summary>Initializes a new instance of the <see cref="DecoPlan" /> class.</summary>
    /// <param name="expandedSegments">The fully expanded sequence of dive segments.</param>
    /// <param name="gasUsage">The gas consumed from each cylinder over the dive.</param>
    /// <param name="totalRuntime">The total runtime of the dive, from leaving the surface to returning to it.</param>
    /// <param name="reserveGas">The per-cylinder reserve gas assessment for the dive.</param>
    /// <param name="centralNervousSystemFraction">
    /// The accrued central nervous system oxygen toxicity, as a fraction of the
    /// single-exposure limit.
    /// </param>
    /// <param name="oxygenToleranceUnits">The accrued pulmonary oxygen toxicity, in oxygen tolerance units.</param>
    /// <param name="violations">The inter-level ascents, if any, that would incur a decompression obligation.</param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="expandedSegments" />, <paramref name="gasUsage" />, <paramref name="reserveGas" />, or
    /// <paramref name="violations" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="expandedSegments" /> is empty, or <paramref name="gasUsage" /> or
    /// <paramref name="violations" /> contains a <see langword="null" /> entry.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="totalRuntime" /> is negative, <paramref name="centralNervousSystemFraction" /> is negative, or
    /// <paramref name="oxygenToleranceUnits" /> is negative.
    /// </exception>
    public DecoPlan(
        IEnumerable<DiveSegment> expandedSegments,
        IEnumerable<CylinderGasUsage> gasUsage,
        TimeSpan totalRuntime,
        ReserveGasResult reserveGas,
        double centralNervousSystemFraction,
        double oxygenToleranceUnits,
        IEnumerable<AscentViolation> violations)
    {
        ArgumentNullException.ThrowIfNull(expandedSegments);
        ArgumentNullException.ThrowIfNull(gasUsage);
        ArgumentNullException.ThrowIfNull(reserveGas);
        ArgumentNullException.ThrowIfNull(violations);

        var segments = expandedSegments.ToArray();
        if (segments.Length == 0)
        {
            throw new ArgumentException("At least one segment must be supplied.", nameof(expandedSegments));
        }

        var usage = gasUsage.ToArray();
        if (Array.Exists(usage, static item => item is null))
        {
            throw new ArgumentException("The gas usage must not contain a null entry.", nameof(gasUsage));
        }

        var recordedViolations = violations.ToArray();
        if (Array.Exists(recordedViolations, static item => item is null))
        {
            throw new ArgumentException("The violations must not contain a null entry.", nameof(violations));
        }

        if (totalRuntime < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(totalRuntime), totalRuntime,
                "The total runtime must not be negative.");
        }

        if (centralNervousSystemFraction < 0.0)
        {
            throw new ArgumentOutOfRangeException(nameof(centralNervousSystemFraction), centralNervousSystemFraction,
                "The central nervous system fraction must not be negative.");
        }

        if (oxygenToleranceUnits < 0.0)
        {
            throw new ArgumentOutOfRangeException(nameof(oxygenToleranceUnits), oxygenToleranceUnits,
                "The oxygen tolerance units must not be negative.");
        }

        _expandedSegments = segments;
        _gasUsage = usage;
        _violations = recordedViolations;
        TotalRuntime = totalRuntime;
        ReserveGas = reserveGas;
        CentralNervousSystemFraction = centralNervousSystemFraction;
        OxygenToleranceUnits = oxygenToleranceUnits;
    }

    /// <summary>Gets the fully expanded sequence of dive segments, in the order they occur.</summary>
    public IReadOnlyList<DiveSegment> ExpandedSegments => _expandedSegments;

    /// <summary>Gets the gas consumed from each cylinder over the dive.</summary>
    public IReadOnlyList<CylinderGasUsage> GasUsage => _gasUsage;

    /// <summary>Gets the total runtime of the dive, from leaving the surface to returning to it.</summary>
    public TimeSpan TotalRuntime { get; }

    /// <summary>Gets the per-cylinder reserve gas assessment for the dive.</summary>
    public ReserveGasResult ReserveGas { get; }

    /// <summary>Gets the accrued central nervous system oxygen toxicity, as a fraction of the single-exposure limit.</summary>
    public double CentralNervousSystemFraction { get; }

    /// <summary>Gets the accrued pulmonary oxygen toxicity, in oxygen tolerance units.</summary>
    public double OxygenToleranceUnits { get; }

    /// <summary>Gets the inter-level ascents that would incur a decompression obligation.</summary>
    public IReadOnlyList<AscentViolation> Violations => _violations;

    /// <summary>
    /// Gets a value indicating whether the plan is valid, being <see langword="true" /> when
    /// no planned inter-level ascent would incur a decompression obligation.
    /// </summary>
    public bool IsValid => _violations.Length == 0;
}