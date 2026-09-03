namespace Bitenovac.CloudBuild.Core.Graph;

/// <summary>
/// Thrown when a <see cref="ProjectGraph"/>'s <c>ProjectReference</c> edges form a cycle.
/// MSBuild cannot build such a graph, and no build order exists to fold a Merkle hash over.
/// </summary>
public sealed class ProjectGraphCycleException : Exception
{
    /// <summary>The projects forming the cycle, in reference order, repeating the first at the end.</summary>
    public IReadOnlyList<ProjectId> Cycle { get; }

    /// <summary>Creates a <see cref="ProjectGraphCycleException"/> for the given cycle.</summary>
    /// <param name="cycle">The projects forming the cycle, in reference order, repeating the first at the end.</param>
    public ProjectGraphCycleException(IReadOnlyList<ProjectId> cycle)
        : base($"The project reference graph has a cycle: {string.Join(" -> ", cycle)}.") =>
        Cycle = cycle;
}
