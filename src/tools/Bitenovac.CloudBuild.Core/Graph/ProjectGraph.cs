namespace Bitenovac.CloudBuild.Core.Graph;

/// <summary>
/// The project reference graph for one repository snapshot: every project, and the projects
/// each one directly references. Immutable once built.
/// </summary>
public sealed class ProjectGraph
{
    private readonly IReadOnlyDictionary<ProjectId, IReadOnlyList<ProjectId>> _dependencies;

    /// <summary>Builds a graph from the discovered projects and their <c>ProjectReference</c> edges.</summary>
    /// <param name="projects">Every project in the repository.</param>
    /// <param name="edges">Every <c>ProjectReference</c> edge between them.</param>
    /// <exception cref="ArgumentNullException"><paramref name="projects"/> or <paramref name="edges"/> is null.</exception>
    /// <exception cref="ArgumentException">
    /// An edge names a project that does not appear in <paramref name="projects"/>.
    /// </exception>
    public ProjectGraph(IEnumerable<ProjectId> projects, IEnumerable<ProjectEdge> edges)
    {
        ArgumentNullException.ThrowIfNull(projects);
        ArgumentNullException.ThrowIfNull(edges);

        var map = new Dictionary<ProjectId, List<ProjectId>>();
        foreach (var project in projects)
            map[project] = [];

        foreach (var edge in edges)
        {
            if (!map.ContainsKey(edge.Project))
                throw new ArgumentException($"Edge declares an unknown project: '{edge.Project}'.", nameof(edges));
            if (!map.ContainsKey(edge.References))
                throw new ArgumentException($"Edge references an unknown project: '{edge.References}'.", nameof(edges));

            map[edge.Project].Add(edge.References);
        }

        Projects = map.Keys.OrderBy(p => p.Value, StringComparer.Ordinal).ToList();
        _dependencies = map.ToDictionary(
            kvp => kvp.Key,
            kvp => (IReadOnlyList<ProjectId>)kvp.Value.OrderBy(p => p.Value, StringComparer.Ordinal).ToList());
    }

    /// <summary>Every project in the graph, ordered by path.</summary>
    public IReadOnlyList<ProjectId> Projects { get; }

    /// <summary>The projects <paramref name="project"/> directly references, ordered by path.</summary>
    /// <param name="project">A project known to this graph.</param>
    /// <exception cref="ArgumentException"><paramref name="project"/> is not in the graph.</exception>
    public IReadOnlyList<ProjectId> GetDependencies(ProjectId project)
    {
        if (!_dependencies.TryGetValue(project, out var dependencies))
            throw new ArgumentException($"Unknown project: '{project}'.", nameof(project));

        return dependencies;
    }

    /// <summary>
    /// Every project in dependency order: a project always appears after everything it
    /// references, directly or transitively. Folding a Merkle hash over this order needs no
    /// recursion — by the time a project is reached, every dependency's hash is already known.
    /// </summary>
    /// <exception cref="ProjectGraphCycleException">The graph contains a cycle.</exception>
    public IReadOnlyList<ProjectId> GetBuildOrder()
    {
        var visiting = new HashSet<ProjectId>();
        var visited = new HashSet<ProjectId>();
        var path = new List<ProjectId>();
        var order = new List<ProjectId>(Projects.Count);

        foreach (var project in Projects)
            Visit(project);

        return order;

        void Visit(ProjectId project)
        {
            if (visited.Contains(project))
                return;

            if (!visiting.Add(project))
            {
                var cycleStart = path.IndexOf(project);
                throw new ProjectGraphCycleException([.. path.Skip(cycleStart), project]);
            }

            path.Add(project);
            foreach (var dependency in _dependencies[project])
                Visit(dependency);
            path.RemoveAt(path.Count - 1);

            visiting.Remove(project);
            visited.Add(project);
            order.Add(project);
        }
    }
}
