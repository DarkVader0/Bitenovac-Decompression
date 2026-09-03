using Bitenovac.CloudBuild.Core.Graph;

namespace Bitenovac.CloudBuild.Core.Coverage;

/// <summary>
/// Checks every gated project's measured coverage against its policy. Pure: it reads no report
/// itself, so the caller is responsible for deciding which projects are gated this run (see
/// <c>Bitenovac.CloudBuild.Core.Planning.TargetDecision.ShouldGateCoverage</c>) and for merging and
/// parsing the coverage report into <see cref="CoverageMeasurement"/> values.
/// </summary>
public static class CoverageGate
{
    /// <summary>
    /// Evaluates every gated project. A project with no measurement fails: it is under the gate
    /// but nothing exercised it, which never happens for a project whose test dependents are
    /// correctly identified upstream.
    /// </summary>
    /// <param name="gated">The projects under the gate this run, with their policy.</param>
    /// <param name="measured">The measured coverage of every project a test run touched.</param>
    /// <returns>One result per entry in <paramref name="gated"/>, ordered by project path.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="gated"/> or <paramref name="measured"/> is null.
    /// </exception>
    public static IReadOnlyList<CoverageGateResult> Evaluate(
        IReadOnlyDictionary<ProjectId, CoveragePolicy> gated,
        IReadOnlyDictionary<ProjectId, CoverageMeasurement> measured)
    {
        ArgumentNullException.ThrowIfNull(gated);
        ArgumentNullException.ThrowIfNull(measured);

        var results = new List<CoverageGateResult>(gated.Count);

        foreach (var (project, policy) in gated.OrderBy(entry => entry.Key.Value, StringComparer.Ordinal))
            results.Add(EvaluateOne(project, policy, measured));

        return results;
    }

    private static CoverageGateResult EvaluateOne(
        ProjectId project,
        CoveragePolicy policy,
        IReadOnlyDictionary<ProjectId, CoverageMeasurement> measured)
    {
        if (!measured.TryGetValue(project, out var measurement))
        {
            return new CoverageGateResult
            {
                Project = project,
                Passed = false,
                Reason = $"{project} is under the coverage gate but no test exercised it.",
            };
        }

        var passed = measurement.LinePercent >= policy.MinimumLinePercent
            && measurement.BranchPercent >= policy.MinimumBranchPercent;

        var figures =
            $"line {measurement.LinePercent:F2}% (min {policy.MinimumLinePercent}%), " +
            $"branch {measurement.BranchPercent:F2}% (min {policy.MinimumBranchPercent}%)";

        return new CoverageGateResult
        {
            Project = project,
            Passed = passed,
            Reason = passed ? figures : $"{project} is below its coverage gate: {figures}",
        };
    }
}
