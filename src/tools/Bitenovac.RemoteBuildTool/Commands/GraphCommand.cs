using Bitenovac.RemoteBuildTool.Core.Graph;
using Bitenovac.RemoteBuildTool.Toolchains;

namespace Bitenovac.RemoteBuildTool.Commands;

/// <summary>Diagnostic: prints the project reference graph across every toolchain, for both configurations.</summary>
internal static class GraphCommand
{
    public static int Run(PipelineOptions options, PipelineOutput output)
    {
        using var toolchains = ToolchainRegistry.Create(options.RepositoryRoot);
        var discovered = toolchains.All.Select(toolchain => (Toolchain: toolchain, RelativePaths: toolchain.Discover())).ToList();

        foreach (var configuration in PipelineOptions.Configurations)
        {
            output.WriteLine($"=== {configuration} ===");
            var evaluated = discovered.SelectMany(found => found.Toolchain.Evaluate(found.RelativePaths, configuration)).ToList();
            var edges = evaluated.SelectMany(project => project.ProjectReferences.Select(reference => new ProjectEdge(project.Id, reference)));
            var graph = new ProjectGraph(evaluated.Select(project => project.Id), edges);

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
