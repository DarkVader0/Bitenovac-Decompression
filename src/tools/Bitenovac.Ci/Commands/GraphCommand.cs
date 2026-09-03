using Bitenovac.Ci.Core.Graph;
using Bitenovac.Ci.MsBuild;

namespace Bitenovac.Ci.Commands;

/// <summary>Diagnostic: prints the project reference graph for both configurations.</summary>
internal static class GraphCommand
{
    public static int Run(PipelineOptions options)
    {
        var relativePaths = ProjectDiscovery.FindRelativePaths(options.RepositoryRoot);
        using var evaluator = new MsBuildProjectEvaluator(options.RepositoryRoot);

        foreach (var configuration in PipelineOptions.Configurations)
        {
            Console.WriteLine($"=== {configuration} ===");
            var evaluated = evaluator.EvaluateAll(relativePaths, configuration);
            var edges = evaluated.Values.SelectMany(project => project.ProjectReferences.Select(reference => new ProjectEdge(project.Id, reference)));
            var graph = new ProjectGraph(evaluated.Keys, edges);

            foreach (var project in graph.Projects)
            {
                Console.WriteLine(project);
                foreach (var dependency in graph.GetDependencies(project))
                    Console.WriteLine($"    -> {dependency}");
            }
        }

        return 0;
    }
}
