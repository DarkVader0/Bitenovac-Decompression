using Bitenovac.RemoteBuildTool.Core.Graph;
using Bitenovac.RemoteBuildTool.Planning;

namespace Bitenovac.RemoteBuildTool.Toolchains;

/// <summary>
/// Provides methods for ordering toolchains so that each builds after every toolchain its projects
/// reference.
/// </summary>
/// <remarks>
/// Each toolchain builds all of its misses in one pass, so this order is the whole cross-language
/// build order.
/// </remarks>
internal static class ToolchainOrder
{
    /// <summary>
    /// Returns the names of the toolchains of the specified plan entries in build order.
    /// </summary>
    /// <param name="entries">Every plan entry of one configuration.</param>
    /// <returns>The toolchain names, each one after every toolchain its projects reference.</returns>
    /// <exception cref="InvalidOperationException">Two or more toolchains reference each other in a cycle.</exception>
    public static IReadOnlyList<string> Resolve(IReadOnlyList<PlanEntry> entries) =>
        Resolve([.. entries.Select(entry => (
            new ProjectId(entry.ProjectPath),
            entry.Toolchain,
            (IReadOnlyList<ProjectId>)[.. entry.References.Select(reference => new ProjectId(reference))]))]);

    /// <summary>
    /// Returns the names of the toolchains of the specified projects in build order.
    /// </summary>
    /// <param name="projects">Every project, with its toolchain and the projects it references.</param>
    /// <returns>The toolchain names, each one after every toolchain its projects reference.</returns>
    /// <exception cref="InvalidOperationException">Two or more toolchains reference each other in a cycle.</exception>
    public static IReadOnlyList<string> Resolve(IReadOnlyCollection<(ProjectId Project, string Toolchain, IReadOnlyList<ProjectId> References)> projects)
    {
        var toolchainOf = projects.ToDictionary(project => project.Project, project => project.Toolchain);

        // A toolchain name is as good a node as a project path; this reuses the graph's cycle detection.
        var nodes = toolchainOf.Values.Distinct(StringComparer.Ordinal).Select(name => new ProjectId(name));
        var edges = projects
            .SelectMany(project => project.References
                .Where(reference => toolchainOf.TryGetValue(reference, out var toolchain) && toolchain != project.Toolchain)
                .Select(reference => new ProjectEdge(new ProjectId(project.Toolchain), new ProjectId(toolchainOf[reference]))))
            .Distinct();

        try
        {
            return new ProjectGraph(nodes, edges).GetBuildOrder().Select(node => node.Value).ToList();
        }
        catch (ProjectGraphCycleException exception)
        {
            throw new InvalidOperationException(
                $"Toolchains reference each other in a cycle ({string.Join(" -> ", exception.Cycle)}). Each toolchain builds in one pass, so one side of the cycle has to go.");
        }
    }
}
