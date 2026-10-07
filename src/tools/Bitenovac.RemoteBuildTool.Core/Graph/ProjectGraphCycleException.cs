namespace Bitenovac.RemoteBuildTool.Core.Graph;

/// <summary>
/// The exception that is thrown when the references in a <see cref="ProjectGraph"/> form a cycle.
/// </summary>
public sealed class ProjectGraphCycleException : Exception
{
    /// <summary>
    /// Gets the projects that form the cycle.
    /// </summary>
    /// <value>
    /// The projects in reference order, with the first project repeated at the end.
    /// </value>
    public IReadOnlyList<ProjectId> Cycle { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ProjectGraphCycleException"/> class for the
    /// specified cycle.
    /// </summary>
    /// <param name="cycle">
    /// The projects that form the cycle, in reference order, with the first project repeated at the end.
    /// </param>
    public ProjectGraphCycleException(IReadOnlyList<ProjectId> cycle)
        : base($"The project reference graph has a cycle: {string.Join(" -> ", cycle)}.") =>
        Cycle = cycle;
}
