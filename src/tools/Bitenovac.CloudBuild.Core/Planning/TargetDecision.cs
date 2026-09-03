using Bitenovac.CloudBuild.Core.Graph;
using Bitenovac.CloudBuild.Core.Hashing;

namespace Bitenovac.CloudBuild.Core.Planning;

/// <summary>
/// What the pipeline should do with one project, in one configuration: whether to compile and
/// retest it, and whether to hold it to its coverage policy.
/// </summary>
public sealed record TargetDecision
{
    /// <summary>The project this decision is for.</summary>
    public required ProjectId Project { get; init; }

    /// <summary>The project's freshly computed hash.</summary>
    public required TargetHash Computed { get; init; }

    /// <summary>
    /// True when this target was forced to a miss regardless of its hash — <c>--cacheless</c>,
    /// or a project explicitly told to always rebuild.
    /// </summary>
    public required bool Forced { get; init; }

    /// <summary>
    /// <see cref="CacheOutcome.Hit"/> when the cached artifact can be reused for both build and
    /// test; <see cref="CacheOutcome.Miss"/> when the project must be compiled and, if it is a
    /// test project, its tests run.
    /// </summary>
    public required CacheOutcome BuildOutcome { get; init; }

    /// <summary>
    /// True when this project's own hash has moved since it was last stored, so it must be held
    /// to its coverage policy this run rather than treated as unchanged-but-rebuilt.
    /// </summary>
    public required bool ShouldGateCoverage { get; init; }
}
