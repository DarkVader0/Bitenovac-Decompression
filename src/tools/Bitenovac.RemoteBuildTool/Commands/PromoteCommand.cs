using Bitenovac.RemoteBuildTool.Core.Graph;
using Bitenovac.RemoteBuildTool.Planning;
using Bitenovac.RemoteBuildTool.Storage;

namespace Bitenovac.RemoteBuildTool.Commands;

/// <summary>
/// Provides the <c>promote</c> pipeline command, which copies every entry this run produced into
/// <c>main</c>.
/// </summary>
/// <remarks>
/// An entry that <c>main</c> already holds under the same full hash is skipped. When anything was
/// promoted, unreferenced blobs are pruned from <c>main</c> afterwards.
/// </remarks>
internal static class PromoteCommand
{
    public static int Run(PipelineOptions options, PipelineOutput output)
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
                output.WriteLine($"  promoted {configuration}/{entry.ProjectPath}");
            }
        }

        output.WriteLine($"Promoted {promoted} entr{(promoted == 1 ? "y" : "ies")} into main.");

        if (promoted > 0)
        {
            var removed = mainStore.Prune();
            if (removed > 0)
                output.WriteLine($"Pruned {removed} unreferenced blob(s).");
        }

        return 0;
    }
}
