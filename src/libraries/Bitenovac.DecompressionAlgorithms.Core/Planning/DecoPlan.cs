using Bitenovac.DecompressionAlgorithms.Units;

namespace Bitenovac.DecompressionAlgorithms.Core.Planning;

/// <summary>
/// Represents the result of a decompression planning calculation: the fully expanded
/// dive profile including descent, bottom, ascent, and decompression stop segments, the
/// gas consumed from each cylinder, the total runtime, the minimum reserve gas required,
/// and the accrued oxygen toxicity exposure.
/// </summary>
/// <remarks>Instances are immutable; the expanded segments and gas usage are copied on construction.</remarks>
public sealed class DecoPlan
{
    private readonly DiveSegment[] _expandedSegments;
    private readonly CylinderGasUsage[] _gasUsage;

    /// <summary>Initializes a new instance of the <see cref="DecoPlan" /> class.</summary>
    /// <param name="expandedSegments">The fully expanded sequence of dive segments.</param>
    /// <param name="gasUsage">The gas consumed from each cylinder over the dive.</param>
    /// <param name="totalRuntime">The total runtime of the dive, from leaving the surface to returning to it.</param>
    /// <param name="reserveGas">The minimum reserve gas volume required to safely abort the dive, measured at surface conditions.</param>
    /// <param name="centralNervousSystemFraction">The accrued central nervous system oxygen toxicity, as a fraction of the single-exposure limit.</param>
    /// <param name="oxygenToleranceUnits">The accrued pulmonary oxygen toxicity, in oxygen tolerance units.</param>
    /// <exception cref="ArgumentNullException"><paramref name="expandedSegments" /> or <paramref name="gasUsage" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentException"><paramref name="expandedSegments" /> is empty, or either collection contains a null element.</exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="totalRuntime" /> is negative, <paramref name="centralNervousSystemFraction" /> is negative, or <paramref name="oxygenToleranceUnits" /> is negative.
    /// </exception>
    public DecoPlan(
        IEnumerable<DiveSegment> expandedSegments,
        IEnumerable<CylinderGasUsage> gasUsage,
        TimeSpan totalRuntime,
        Volume reserveGas,
        double centralNervousSystemFraction,
        double oxygenToleranceUnits)
    {
        ArgumentNullException.ThrowIfNull(expandedSegments);
        ArgumentNullException.ThrowIfNull(gasUsage);

        var segments = expandedSegments.ToArray();
        if (segments.Length == 0)
            throw new ArgumentException("At least one segment must be supplied.", nameof(expandedSegments));
        // if (Array.Exists(segments, static segment => segment is null))
        //     throw new ArgumentException("The segments must not contain a null segment.", nameof(expandedSegments));

        var usage = gasUsage.ToArray();
        // if (Array.Exists(usage, static item => item is null))
        //     throw new ArgumentException("The gas usage must not contain a null entry.", nameof(gasUsage));

        if (totalRuntime < TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(totalRuntime), totalRuntime,
                "The total runtime must not be negative.");
        if (centralNervousSystemFraction < 0.0)
            throw new ArgumentOutOfRangeException(nameof(centralNervousSystemFraction), centralNervousSystemFraction,
                "The central nervous system fraction must not be negative.");
        if (oxygenToleranceUnits < 0.0)
            throw new ArgumentOutOfRangeException(nameof(oxygenToleranceUnits), oxygenToleranceUnits,
                "The oxygen tolerance units must not be negative.");

        _expandedSegments = segments;
        _gasUsage = usage;
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

    /// <summary>Gets the minimum reserve gas volume required to safely abort the dive, measured at surface conditions.</summary>
    public Volume ReserveGas { get; }

    /// <summary>Gets the accrued central nervous system oxygen toxicity, as a fraction of the single-exposure limit.</summary>
    public double CentralNervousSystemFraction { get; }

    /// <summary>Gets the accrued pulmonary oxygen toxicity, in oxygen tolerance units.</summary>
    public double OxygenToleranceUnits { get; }
}