using Bitenovac.Ci.Core.Coverage;
using Bitenovac.Ci.Core.Graph;
using Bitenovac.Ci.Core.Planning;

namespace Bitenovac.Ci.Core.Unit.Tests;

/// <summary>Builds the graphs, hashes and coverage inputs shared by the Core tests.</summary>
internal static class TestFactory
{
    public static ProjectId Id(string relativePath) => new(relativePath);

    public static ProjectEdge Edge(string from, string to) => new(Id(from), Id(to));

    /// <summary>
    /// Builds a graph from "from -&gt; to, to, ..." lines. A project with no arrow has no
    /// dependencies. Every project named on either side is included.
    /// </summary>
    public static ProjectGraph Graph(params string[] edges)
    {
        var projects = new HashSet<ProjectId>();
        var parsedEdges = new List<ProjectEdge>();

        foreach (var line in edges)
        {
            var sides = line.Split("->", 2, StringSplitOptions.TrimEntries);
            var from = Id(sides[0]);
            projects.Add(from);

            if (sides.Length == 1)
                continue;

            foreach (var to in sides[1].Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
            {
                var toId = Id(to);
                projects.Add(toId);
                parsedEdges.Add(new ProjectEdge(from, toId));
            }
        }

        return new ProjectGraph(projects, parsedEdges);
    }

    public static IReadOnlyDictionary<ProjectId, IReadOnlyList<string>> OwnInputs(
        params (string Project, string[] Inputs)[] entries) =>
        entries.ToDictionary(e => Id(e.Project), e => (IReadOnlyList<string>)e.Inputs);

    /// <summary>
    /// One own-hash input per project the graph knows about, distinct per project so two
    /// projects never accidentally share an own hash, but stable across calls with the same
    /// <paramref name="input"/> so the same graph rehashes identically.
    /// </summary>
    public static IReadOnlyDictionary<ProjectId, IReadOnlyList<string>> UniformOwnInputs(
        ProjectGraph graph, string input = "unchanged") =>
        graph.Projects.ToDictionary(
            project => project,
            project => (IReadOnlyList<string>)new[] { $"{project.Value}:{input}" });

    public static IReadOnlyDictionary<ProjectId, StoredTargetHash> Stored(
        params (string Project, string OwnHash, string FullHash)[] entries) =>
        entries.ToDictionary(e => Id(e.Project), e => new StoredTargetHash(e.OwnHash, e.FullHash));

    public static CoveragePolicy Policy(double line = 100, double branch = 100) => new(line, branch);

    public static CoverageMeasurement Measurement(double line, double branch) => new(line, branch);
}
