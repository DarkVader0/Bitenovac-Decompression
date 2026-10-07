using Bitenovac.RemoteBuildTool.Core.Graph;
using Bitenovac.RemoteBuildTool.Core.Planning;
using Bitenovac.RemoteBuildTool.Hashing;
using Bitenovac.RemoteBuildTool.Planning;
using Bitenovac.RemoteBuildTool.Storage;
using Bitenovac.RemoteBuildTool.Toolchains;

namespace Bitenovac.RemoteBuildTool.Commands;

/// <summary>
/// Discovers every project of every toolchain, lets each toolchain verify and restore its own,
/// hashes, and decides hit or miss against <c>main</c> for both configurations. Everything later
/// stages need is written to <see cref="PipelineOptions.PlanFile"/>.
/// </summary>
internal static class PlanCommand
{
    public static int Run(PipelineOptions options, PipelineOutput output)
    {
        using var toolchains = ToolchainRegistry.Create(options.RepositoryRoot);

        output.WriteLine("==> Discovering projects");
        var discovered = toolchains.All
            .Select(toolchain => (Toolchain: toolchain, RelativePaths: toolchain.Discover()))
            .Where(found => found.RelativePaths.Count > 0)
            .ToList();

        var total = discovered.Sum(found => found.RelativePaths.Count);
        if (total == 0)
        {
            output.WriteError("error: no projects found in the repository.");
            return 1;
        }

        output.WriteLine($"Found {total} project(s): {string.Join(", ", discovered.Select(found => $"{found.Toolchain.Name} {found.RelativePaths.Count}"))}.");

        var evaluated = PipelineOptions.Configurations.ToDictionary(
            configuration => configuration,
            _ => new Dictionary<ProjectId, EvaluatedProject>());

        foreach (var (toolchain, relativePaths) in discovered)
        {
            var byConfiguration = PipelineOptions.Configurations.ToDictionary(
                configuration => configuration,
                configuration => toolchain.Evaluate(relativePaths, configuration));

            var prepared = toolchain.Prepare(options, byConfiguration, output);
            if (!prepared.Succeeded)
                return prepared.ExitCode;

            foreach (var (configuration, projects) in prepared.ByConfiguration)
            {
                foreach (var project in projects)
                    evaluated[configuration][project.Id] = project;
            }
        }

        foreach (var projects in evaluated.Values)
            ToolchainOrder.Resolve([.. projects.Values.Select(project => (project.Id, project.Toolchain, project.ProjectReferences))]);

        var remoteBuildToolHash = RemoteBuildToolVersion.Compute(AppContext.BaseDirectory);
        var mainStore = new LocalVolumeArtifactStore(options.MainStoreRoot);
        var planState = new PlanState([]);

        foreach (var configuration in PipelineOptions.Configurations)
        {
            var entries = PlanConfiguration(configuration, evaluated[configuration], mainStore, remoteBuildToolHash, options.Cacheless);
            planState.ByConfiguration[configuration] = entries;

            var hits = entries.Count(entry => entry.Hit);
            output.WriteLine($"{configuration}: {entries.Count} project(s), {hits} cache hit(s), {entries.Count - hits} to build.");
        }

        planState.Save(options.PlanFile);
        output.WriteLine($"Plan written to {options.PlanFile}");
        return 0;
    }

    private static List<PlanEntry> PlanConfiguration(
        string configuration,
        IReadOnlyDictionary<ProjectId, EvaluatedProject> evaluated,
        LocalVolumeArtifactStore mainStore,
        string remoteBuildToolHash,
        bool cacheless)
    {
        var ids = evaluated.Keys.ToList();
        var edges = evaluated.Values.SelectMany(project => project.ProjectReferences.Select(reference => new ProjectEdge(project.Id, reference)));
        var graph = new ProjectGraph(ids, edges);

        var ownHashInputs = evaluated.ToDictionary(entry => entry.Key, entry => entry.Value.OwnHashInputs);

        var stored = new Dictionary<ProjectId, StoredTargetHash>();
        foreach (var id in ids)
        {
            if (mainStore.TryGetHash(id, configuration, out var hash))
                stored[id] = hash;
        }

        var forced = cacheless ? ids.ToHashSet() : null;
        var decisions = CachePlanBuilder.Build(graph, ownHashInputs, stored, forced, [$"remotebuildtool:{remoteBuildToolHash}"]);

        return ids.Select(id =>
        {
            var project = evaluated[id];
            var decision = decisions[id];
            return new PlanEntry(
                ProjectPath: id.Value,
                FullPath: project.FullPath,
                Toolchain: project.Toolchain,
                References: [.. project.ProjectReferences.Select(reference => reference.Value)],
                CoverageName: project.CoverageName,
                Properties: project.Properties,
                IsTestProject: project.IsTestProject,
                ExcludeFromCoverage: project.ExcludeFromCoverage,
                MinimumLineCoverage: project.MinimumLineCoverage,
                MinimumBranchCoverage: project.MinimumBranchCoverage,
                CacheTestResults: project.CacheTestResults,
                OwnHash: decision.Computed.OwnHash,
                FullHash: decision.Computed.FullHash,
                Forced: decision.Forced,
                Hit: decision.BuildOutcome == CacheOutcome.Hit,
                ShouldGateCoverage: decision.ShouldGateCoverage);
        }).ToList();
    }
}
