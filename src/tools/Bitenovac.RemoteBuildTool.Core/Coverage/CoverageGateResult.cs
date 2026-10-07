using Bitenovac.RemoteBuildTool.Core.Graph;

namespace Bitenovac.RemoteBuildTool.Core.Coverage;

/// <summary>
/// Represents the outcome of checking one project against its <see cref="CoveragePolicy"/>.
/// </summary>
public sealed record CoverageGateResult
{
    /// <summary>
    /// Gets the project that was checked.
    /// </summary>
    public required ProjectId Project { get; init; }

    /// <summary>
    /// Gets a value that indicates whether the project met its coverage policy.
    /// </summary>
    public required bool Passed { get; init; }

    /// <summary>
    /// Gets a human-readable explanation of the result.
    /// </summary>
    /// <value>
    /// The measured figures against the policy when <see cref="Passed"/> is <see langword="true"/>;
    /// otherwise, a description of the shortfall.
    /// </value>
    public required string Reason { get; init; }
}
