using Bitenovac.CloudBuild.Core.Graph;
using Bitenovac.CloudBuild.Core.Planning;
using Bitenovac.CloudBuild.Hashing;
using Bitenovac.CloudBuild.MsBuild;
using Bitenovac.CloudBuild.Planning;
using Bitenovac.CloudBuild.Storage;

namespace Bitenovac.CloudBuild.Commands;

/// <summary>
/// Discovers every project, verifies the one build-order invariant CI still enforces, restores,
/// hashes, and decides hit or miss against <c>main</c> for both configurations. Everything later
/// stages need is written to <see cref="PipelineOptions.PlanFile"/>.
/// </summary>
internal static class PlanCommand
{
    public static int Run(PipelineOptions options)
    {
        Console.WriteLine("==> Discovering projects");
        var relativePaths = ProjectDiscovery.FindRelativePaths(options.RepositoryRoot);
        if (relativePaths.Count == 0)
        {
            Console.Error.WriteLine("error: no projects found in the repository.");
            return 1;
        }

        Console.WriteLine($"Found {relativePaths.Count} project(s).");

        using var evaluator = new MsBuildProjectEvaluator(options.RepositoryRoot);
        var evaluated = EvaluateAll(evaluator, relativePaths);

        var verifyExitCode = Verify(evaluated["Debug"].Values);
        if (verifyExitCode != 0)
            return verifyExitCode;

        Console.WriteLine("==> Restoring");
        var restoreExitCode = Restore(options, evaluated["Debug"].Values);
        if (restoreExitCode != 0)
        {
            Console.Error.WriteLine($"error: restore failed (exit {restoreExitCode}).");
            return restoreExitCode;
        }

        // Fold the now-resolved package closure into what was already evaluated, rather than
        // evaluating every project a second time — see MsBuildProjectEvaluator.RefreshPackageClosure.
        evaluated = evaluated.ToDictionary(
            byConfiguration => byConfiguration.Key,
            byConfiguration => (IReadOnlyDictionary<ProjectId, EvaluatedProject>)byConfiguration.Value.ToDictionary(
                entry => entry.Key,
                entry => MsBuildProjectEvaluator.RefreshPackageClosure(entry.Value)));

        var toolHash = ToolVersion.Compute(options.RepositoryRoot);
        var mainStore = new LocalVolumeArtifactStore(options.MainStoreRoot);
        var planState = new PlanState([]);

        foreach (var configuration in PipelineOptions.Configurations)
        {
            var entries = PlanConfiguration(configuration, evaluated[configuration], mainStore, toolHash, options.Cacheless);
            planState.ByConfiguration[configuration] = entries;

            var hits = entries.Count(entry => entry.Hit);
            Console.WriteLine($"{configuration}: {entries.Count} project(s), {hits} cache hit(s), {entries.Count - hits} to build.");
        }

        planState.Save(options.PlanFile);
        Console.WriteLine($"Plan written to {options.PlanFile}");
        return 0;
    }

    private static Dictionary<string, IReadOnlyDictionary<ProjectId, EvaluatedProject>> EvaluateAll(
        MsBuildProjectEvaluator evaluator, IReadOnlyList<string> relativePaths)
    {
        var result = new Dictionary<string, IReadOnlyDictionary<ProjectId, EvaluatedProject>>();
        foreach (var configuration in PipelineOptions.Configurations)
            result[configuration] = evaluator.EvaluateAll(relativePaths, configuration);

        return result;
    }

    // TargetPath goes empty under an outer multi-targeting build, and the whole
    // artifact-location scheme this tool relies on depends on it.
    private static int Verify(IEnumerable<EvaluatedProject> projects)
    {
        Console.WriteLine("==> Verifying build assumptions");

        var offenders = projects.Where(project => project.HasTargetFrameworks).ToList();
        if (offenders.Count > 0)
        {
            Console.Error.WriteLine("error: these projects declare <TargetFrameworks>. Set a single <TargetFramework>, or change RepositoryTargetFramework in Directory.Build.props.");
            foreach (var project in offenders)
                Console.Error.WriteLine($"  {project.Id}");
            return 1;
        }

        Console.WriteLine("OK: every project targets a single framework.");
        return 0;
    }

    private static int Restore(PipelineOptions options, IEnumerable<EvaluatedProject> projects) =>
        MsBuildRunner.Restore(
            options.RepositoryRoot,
            projects.Select(project => project.FullPath),
            configuration: "Debug",
            options.SyntheticProjectPath("restore"));

    private static List<PlanEntry> PlanConfiguration(
        string configuration,
        IReadOnlyDictionary<ProjectId, EvaluatedProject> evaluated,
        LocalVolumeArtifactStore mainStore,
        string toolHash,
        bool cacheless)
    {
        var ids = evaluated.Keys.ToList();
        var edges = evaluated.Values.SelectMany(project => project.ProjectReferences.Select(reference => new ProjectEdge(project.Id, reference)));
        var graph = new ProjectGraph(ids, edges);

        var ownHashInputs = evaluated.ToDictionary(entry => entry.Key, entry => (IReadOnlyList<string>)entry.Value.OwnHashInputs);

        var stored = new Dictionary<ProjectId, StoredTargetHash>();
        foreach (var id in ids)
        {
            if (mainStore.TryGetHash(id, configuration, out var hash))
                stored[id] = hash;
        }

        var forced = cacheless ? ids.ToHashSet() : null;
        var decisions = CachePlanBuilder.Build(graph, ownHashInputs, stored, forced, [$"ci-tool:{toolHash}"]);

        return ids.Select(id =>
        {
            var project = evaluated[id];
            var decision = decisions[id];
            return new PlanEntry(
                ProjectPath: id.Value,
                FullPath: project.FullPath,
                AssemblyName: project.AssemblyName,
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
