using Bitenovac.Ci.Core.Graph;
using Bitenovac.Ci.Core.Hashing;

namespace Bitenovac.Ci.Core.Planning;

/// <summary>
/// Turns a project graph and each project's own-hash inputs into a decision for every target.
/// The Merkle fold happens here, in dependency order, so a caller supplies only each project's
/// own inputs and never has to reason about the graph itself.
/// </summary>
public static class CachePlanBuilder
{
    /// <summary>
    /// Computes both hashes for every project in <paramref name="graph"/> and decides its
    /// caching outcome against <paramref name="stored"/>.
    /// </summary>
    /// <param name="graph">The project reference graph for one configuration.</param>
    /// <param name="ownHashInputs">
    /// Every project's own-hash inputs (see <see cref="TargetHasher.ComputeOwnHash"/>). Must
    /// have an entry for every project in <paramref name="graph"/>.
    /// </param>
    /// <param name="stored">
    /// The hash <c>main</c> last recorded for each project. A project absent here is always a
    /// miss and is always gated.
    /// </param>
    /// <param name="forced">
    /// Projects to treat as a build miss regardless of their hash. Defaults to none.
    /// </param>
    /// <param name="globalFullHashInputs">
    /// Extra entries folded into every project's <c>fullHash</c> alongside its dependencies —
    /// the CI tool's own hash, for example. Deliberately excluded from <c>ownHash</c>: a tool
    /// change must force every project to rebuild and retest, but forcing a coverage re-gate on
    /// every unchanged project as well would demand a fresh report for code that provably
    /// produces the same one. Defaults to none.
    /// </param>
    /// <returns>One decision per project in <paramref name="graph"/>.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="graph"/>, <paramref name="ownHashInputs"/>, or <paramref name="stored"/> is null.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="ownHashInputs"/> has no entry for a project in <paramref name="graph"/>.
    /// </exception>
    /// <exception cref="ProjectGraphCycleException"><paramref name="graph"/> contains a cycle.</exception>
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

            // A forced project is always re-gated too, not only rebuilt: forcing exists for
            // --cacheless, whose whole purpose is to ignore main and re-verify from nothing, so
            // a project whose ownHash happens to still match main's stored value must not be
            // read as "provably unchanged, skip re-measuring" the way it correctly is when the
            // hash comparison is real.
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
