using Bitenovac.Ci.Core.Graph;

namespace Bitenovac.Ci.Core.Coverage;

/// <summary>Whether one gated project met its coverage policy, and why.</summary>
public sealed record CoverageGateResult
{
    /// <summary>The project this result is for.</summary>
    public required ProjectId Project { get; init; }

    /// <summary>True when the project met its policy, or was not under the gate at all.</summary>
    public required bool Passed { get; init; }

    /// <summary>
    /// A human-readable explanation: the measured figures against the policy when
    /// <see cref="Passed"/> is true, or the shortfall when it is false.
    /// </summary>
    public required string Reason { get; init; }
}
