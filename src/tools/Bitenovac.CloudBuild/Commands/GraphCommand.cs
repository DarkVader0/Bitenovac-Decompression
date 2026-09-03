using Bitenovac.CloudBuild.Core.Graph;
using Bitenovac.CloudBuild.MsBuild;

namespace Bitenovac.CloudBuild.Commands;

/// <summary>Diagnostic: prints the project reference graph for both configurations.</summary>
internal static class GraphCommand
{
    public static int Run(PipelineOptions options, PipelineOutput output)
    {
        var relativePaths = ProjectDiscovery.FindRelativePaths(options.RepositoryRoot);
        using var evaluator = new MsBuildProjectEvaluator(options.RepositoryRoot);

        foreach (var configuration in PipelineOptions.Configurations)
        {
            output.WriteLine($"=== {configuration} ===");
            var evaluated = evaluator.EvaluateAll(relativePaths, configuration);
            var edges = evaluated.Values.SelectMany(project => project.ProjectReferences.Select(reference => new ProjectEdge(project.Id, reference)));
            var graph = new ProjectGraph(evaluated.Keys, edges);

            foreach (var project in graph.Projects)
            {
                output.WriteLine(project.ToString());
                foreach (var dependency in graph.GetDependencies(project))
                    output.WriteLine($"    -> {dependency}");
            }
        }

        return 0;
    }
}
