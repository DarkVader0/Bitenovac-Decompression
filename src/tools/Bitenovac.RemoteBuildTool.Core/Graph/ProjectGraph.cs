namespace Bitenovac.RemoteBuildTool.Core.Graph;

/// <summary>
/// Represents the reference graph of every project in one repository snapshot.
/// </summary>
public sealed class ProjectGraph
{
    private readonly IReadOnlyDictionary<ProjectId, IReadOnlyList<ProjectId>> _dependencies;

    /// <summary>
    /// Initializes a new instance of the <see cref="ProjectGraph"/> class from a set of projects
    /// and the references between them.
    /// </summary>
    /// <param name="projects">Every project in the graph.</param>
    /// <param name="edges">Every reference between the projects in <paramref name="projects"/>.</param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="projects"/> or <paramref name="edges"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// An edge names a project that is not in <paramref name="projects"/>.
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

    /// <summary>
    /// Gets every project in the graph.
    /// </summary>
    /// <value>
    /// The projects, ordered ordinally by path.
    /// </value>
    public IReadOnlyList<ProjectId> Projects { get; }

    /// <summary>
    /// Returns the projects that the specified project directly references.
    /// </summary>
    /// <param name="project">A project in the graph.</param>
    /// <returns>The directly referenced projects, ordered ordinally by path.</returns>
    /// <exception cref="ArgumentException"><paramref name="project"/> is not in the graph.</exception>
    public IReadOnlyList<ProjectId> GetDependencies(ProjectId project) 
        => !_dependencies.TryGetValue(project, out var dependencies) 
            ? throw new ArgumentException($"Unknown project: '{project}'.", nameof(project)) 
            : dependencies;

    /// <summary>
    /// Returns every project in dependency order.
    /// </summary>
    /// <returns>
    /// The projects, each one after every project it references, directly or transitively.
    /// </returns>
    /// <exception cref="ProjectGraphCycleException">The references in the graph form a cycle.</exception>
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
