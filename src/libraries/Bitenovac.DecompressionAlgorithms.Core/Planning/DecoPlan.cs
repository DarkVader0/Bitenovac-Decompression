using System.Globalization;
using System.Text;
using Bitenovac.DecompressionAlgorithms.Core.Calculations;
using Bitenovac.DecompressionAlgorithms.Units;

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

    /// <summary>
    /// Returns a human-readable summary of the plan: a header with the validity and total
    /// runtime, a schedule table with one line per phase showing the kind, depth, duration,
    /// runtime, and breathing gas, followed by the oxygen exposure, the per-cylinder gas
    /// usage, and the reserve assessment. Durations and runtimes are rounded to whole
    /// minutes for display. Consecutive ascent segments are merged into a single line
    /// ending at the depth where the ascent pauses, so intermediate stop depths that are
    /// passed without holding do not appear. The output is formatted with the invariant
    /// culture.
    /// </summary>
    /// <returns>The multi-line summary of the plan.</returns>
    public override string ToString()
    {
        var builder = new StringBuilder();
        builder.Append(CultureInfo.InvariantCulture, $"Valid: {IsValid}, total runtime: {TotalRuntime}")
            .AppendLine()
            .AppendLine();

        static void AppendRow(StringBuilder builder,
            SegmentKind kind,
            Depth depth,
            TimeSpan duration,
            TimeSpan runtime,
            GasMixture gas,
            BreathingLoop loop) =>
            builder.Append(CultureInfo.InvariantCulture,
                    $"{kind,-9} {depth.InMeter,6:0.##} m  " +
                    $"{Math.Round(duration.TotalMinutes),4:0} min  " +
                    $"{Math.Round(runtime.TotalMinutes),4:0} min  {gas}" +
                    $"{(loop.Mode == DiveMode.OC ? string.Empty : $" ({loop})")}")
                .AppendLine();

        var runtime = TimeSpan.Zero;
        var pendingAscent = TimeSpan.Zero;
        var pendingAscentGas = GasMixture.Air;
        var pendingAscentLoop = BreathingLoop.OpenCircuit;
        var pendingAscentDepth = Depth.FromMeter(0);
        foreach (var segment in _expandedSegments)
        {
            runtime += segment.Duration;

            if (segment.Kind == SegmentKind.Ascent)
            {
                pendingAscent += segment.Duration;
                pendingAscentGas = segment.Gas;
                pendingAscentLoop = segment.Loop;
                pendingAscentDepth = segment.Depth;
                continue;
            }

            if (pendingAscent > TimeSpan.Zero)
            {
                AppendRow(builder, SegmentKind.Ascent, pendingAscentDepth, pendingAscent,
                    runtime - segment.Duration, pendingAscentGas, pendingAscentLoop);
                pendingAscent = TimeSpan.Zero;
            }

            AppendRow(builder, segment.Kind, segment.Depth, segment.Duration, runtime, segment.Gas, segment.Loop);
        }

        if (pendingAscent > TimeSpan.Zero)
        {
            AppendRow(builder, SegmentKind.Ascent, pendingAscentDepth, pendingAscent, runtime, pendingAscentGas,
                pendingAscentLoop);
        }

        builder.AppendLine()
            .Append(CultureInfo.InvariantCulture, $"CNS: {CentralNervousSystemFraction * 100:0.##} %")
            .AppendLine()
            .Append(CultureInfo.InvariantCulture, $"OTU: {OxygenToleranceUnits:0.##}")
            .AppendLine()
            .AppendLine();

        foreach (var usage in _gasUsage)
        {
            builder.Append(CultureInfo.InvariantCulture,
                    $"Cylinder {usage.Cylinder.Gas}: used {usage.GasUsed.InLiter:0.##} L, " +
                    $"end pressure {usage.EndPressure.InBar:0.##} bar")
                .AppendLine();
        }

        builder.Append(CultureInfo.InvariantCulture, $"Reserve satisfied: {ReserveGas.AllSatisfied}");
        return builder.ToString();
    }
}