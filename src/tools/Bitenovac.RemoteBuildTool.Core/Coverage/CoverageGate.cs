using Bitenovac.RemoteBuildTool.Core.Graph;

namespace Bitenovac.RemoteBuildTool.Core.Coverage;

/// <summary>
/// Provides methods for checking measured coverage against each project's <see cref="CoveragePolicy"/>.
/// </summary>
public static class CoverageGate
{
    /// <summary>
    /// Evaluates every gated project against its policy.
    /// </summary>
    /// <param name="gated">The projects under the gate, each with the policy it must meet.</param>
    /// <param name="measured">The measured coverage of every project a test run exercised.</param>
    /// <returns>One result for each project in <paramref name="gated"/>, ordered by project path.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="gated"/> or <paramref name="measured"/> is <see langword="null"/>.
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
