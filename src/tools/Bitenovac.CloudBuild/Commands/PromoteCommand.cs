using Bitenovac.CloudBuild.Core.Graph;
using Bitenovac.CloudBuild.Planning;
using Bitenovac.CloudBuild.Storage;

namespace Bitenovac.CloudBuild.Commands;

/// <summary>
/// Copies every entry this run produced into <c>main</c>, skipping anything main already has at
/// the same fullHash. Only reachable from a <c>merge_group</c> run in CI — see
/// <c>.github/workflows/promote.yml</c> — which is what makes it safe for a PR to never write to
/// <c>main</c>: a PR run can call this locally, but nothing routes it there in the real pipeline.
/// </summary>
internal static class PromoteCommand
{
    public static int Run(PipelineOptions options)
    {
        var plan = PlanState.Load(options.PlanFile);
        var mainStore = new LocalVolumeArtifactStore(options.MainStoreRoot);
        var prStore = new LocalVolumeArtifactStore(options.PrStoreRoot);

        var promoted = 0;
        foreach (var configuration in PipelineOptions.Configurations)
        {
            foreach (var entry in plan.For(configuration))
            {
                var project = new ProjectId(entry.ProjectPath);

                if (mainStore.TryGetHash(project, configuration, out var existing) && existing.FullHash == entry.FullHash)
                    continue;

                mainStore.Promote(project, configuration, prStore);
                promoted++;
                Console.WriteLine($"  promoted {configuration}/{entry.ProjectPath}");
            }
        }

        Console.WriteLine($"Promoted {promoted} entr{(promoted == 1 ? "y" : "ies")} into main.");

        if (promoted > 0)
        {
            var removed = mainStore.Prune();
            if (removed > 0)
                Console.WriteLine($"Pruned {removed} unreferenced blob(s).");
        }

        return 0;
    }
}
