using Bitenovac.RemoteBuildTool.Core.Graph;
using Bitenovac.RemoteBuildTool.Core.Hashing;

namespace Bitenovac.RemoteBuildTool.Core.Planning;

/// <summary>
/// Provides methods for deciding, from a project graph and each project's own inputs, which
/// projects to rebuild, retest and gate on coverage.
/// </summary>
public static class CachePlanBuilder
{
    /// <summary>
    /// Computes both hashes for every project in a graph and decides each project's caching outcome.
    /// </summary>
    /// <param name="graph">The project graph for one configuration.</param>
    /// <param name="ownHashInputs">
    /// The own-hash inputs of every project in <paramref name="graph"/>, as accepted by
    /// <see cref="TargetHasher.ComputeOwnHash"/>.
    /// </param>
    /// <param name="stored">
    /// The hashes stored for each project. A project with no entry is always a miss and is always gated.
    /// </param>
    /// <param name="forced">
    /// The projects to treat as a miss regardless of their hash, or <see langword="null"/> for none.
    /// </param>
    /// <param name="globalFullHashInputs">
    /// Additional inputs folded into every project's full hash but not its own hash, or
    /// <see langword="null"/> for none.
    /// </param>
    /// <returns>A decision for each project in <paramref name="graph"/>, keyed by project.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="graph"/>, <paramref name="ownHashInputs"/>, or <paramref name="stored"/> is
    /// <see langword="null"/>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="ownHashInputs"/> has no entry for a project in <paramref name="graph"/>.
    /// </exception>
    /// <exception cref="ProjectGraphCycleException">
    /// The references in <paramref name="graph"/> form a cycle.
    /// </exception>
    /// <remarks>
    /// <para>
    /// A project is a <see cref="CacheOutcome.Hit"/> when it is not forced and its full hash matches
    /// the stored one. It is gated on coverage when it is forced, has nothing stored, or its own
    /// hash differs from the stored one.
    /// </para>
    /// <para>
    /// <paramref name="globalFullHashInputs"/> suits inputs such as the build tool's own version.
    /// A change to one rebuilds and retests every project, but leaves coverage ungated for projects
    /// whose own code is unchanged, because their coverage cannot have changed.
    /// </para>
    /// </remarks>
    public static IReadOnlyDictionary<ProjectId, TargetDecision> Build(
        ProjectGraph graph,
        IReadOnlyDictionary<ProjectId, IReadOnlyList<string>> ownHashInputs,
        IReadOnlyDictionary<ProjectId, StoredTargetHash> stored,
        IReadOnlySet<ProjectId>? forced = null,
        IReadOnlyList<string>? globalFullHashInputs = null)
    {
        ArgumentNullException.ThrowIfNull(graph);
        ArgumentNullException.ThrowIfNull(ownHashInputs);
        ArgumentNullException.ThrowIfNull(stored);

        forced ??= new HashSet<ProjectId>();
        globalFullHashInputs ??= [];

        var fullHashes = new Dictionary<ProjectId, string>();
        var decisions = new Dictionary<ProjectId, TargetDecision>();

        foreach (var project in graph.GetBuildOrder())
        {
            if (!ownHashInputs.TryGetValue(project, out var inputs))
                throw new ArgumentException($"No own-hash inputs were supplied for '{project}'.", nameof(ownHashInputs));

            var ownHash = TargetHasher.ComputeOwnHash(inputs);

            var dependencyFullHashes = graph.GetDependencies(project)
                .Select(dependency => fullHashes[dependency])
                .Concat(globalFullHashInputs);
            var fullHash = TargetHasher.ComputeFullHash(ownHash, dependencyFullHashes);
            fullHashes[project] = fullHash;

            var isForced = forced.Contains(project);
            var hasStored = stored.TryGetValue(project, out var storedHash);

            var buildOutcome = !isForced && hasStored && storedHash.FullHash == fullHash
                ? CacheOutcome.Hit
                : CacheOutcome.Miss;

            var shouldGateCoverage = isForced || !hasStored || storedHash.OwnHash != ownHash;

            decisions[project] = new TargetDecision
            {
                Project = project,
                Computed = new TargetHash(ownHash, fullHash),
                Forced = isForced,
                BuildOutcome = buildOutcome,
                ShouldGateCoverage = shouldGateCoverage,
            };
        }

        return decisions;
    }
}
