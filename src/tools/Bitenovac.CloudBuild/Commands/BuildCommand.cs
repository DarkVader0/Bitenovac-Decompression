using Bitenovac.CloudBuild.Core.Graph;
using Bitenovac.CloudBuild.Core.Planning;
using Bitenovac.CloudBuild.MsBuild;
using Bitenovac.CloudBuild.Planning;
using Bitenovac.CloudBuild.Storage;

namespace Bitenovac.CloudBuild.Commands;

/// <summary>
/// Materialises every cache hit from <c>main</c> into the checkout, then compiles the whole
/// selected set in one MSBuild session — the hits included, relying on MSBuild's own up-to-date
/// check (made trustworthy by <see cref="Touch"/>) to skip real work on them and genuinely
/// compile only the misses.
/// </summary>
internal static class BuildCommand
{
    /// <summary>The parts of a stored entry that belong in a source tree. <c>tests/</c> does not.</summary>
    private static readonly string[] MaterialisedPrefixes = ["bin/", "obj/"];

    public static int Run(PipelineOptions options, string configuration, PipelineOutput output)
    {
        var plan = PlanState.Load(options.PlanFile);
        var entries = plan.For(configuration);
        if (entries.Count == 0)
        {
            output.WriteLine("Nothing selected; skipping build.");
            return 0;
        }

        var mainStore = new LocalVolumeArtifactStore(options.MainStoreRoot);
        var prStore = new LocalVolumeArtifactStore(options.PrStoreRoot);

        var (materialised, skipped) = MaterialiseHits(options, entries, configuration, mainStore);
        output.WriteLine($"{configuration}: materialised {materialised} cache hit(s) from main"
            + (skipped > 0 ? $", {skipped} already present" : "") + ".");

        output.WriteLine($"==> Building {entries.Count} project(s) ({configuration})");
        var exitCode = MsBuildRunner.Build(
            options.RepositoryRoot,
            entries.Select(entry => entry.FullPath),
            configuration,
            options.SyntheticProjectPath($"build-{configuration}"));

        if (exitCode != 0)
        {
            output.WriteError($"error: build failed (exit {exitCode}).");
            return exitCode;
        }

        var staged = StageMisses(entries, configuration, prStore);

        foreach (var entry in entries)
            MaterialisedMarker.Write(options.RepositoryRoot, new ProjectId(entry.ProjectPath), configuration, entry.FullHash);

        output.WriteLine($"{configuration}: build complete, staged {staged} rebuilt project(s).");
        return 0;
    }

    private static (int Materialised, int Skipped) MaterialiseHits(
        PipelineOptions options, IReadOnlyList<PlanEntry> entries, string configuration, LocalVolumeArtifactStore mainStore)
    {
        var materialised = 0;
        var skipped = 0;

        foreach (var entry in entries.Where(entry => entry.Hit))
        {
            var project = new ProjectId(entry.ProjectPath);
            var projectDirectory = Path.GetDirectoryName(entry.FullPath)!;

            if (MaterialisedMarker.Matches(options.RepositoryRoot, project, configuration, entry.FullHash, projectDirectory))
            {
                skipped++;
                continue;
            }

            if (!mainStore.TryGet(project, configuration, projectDirectory, out _, MaterialisedPrefixes))
                continue;

            Touch(Path.Combine(projectDirectory, "bin"));
            Touch(Path.Combine(projectDirectory, "obj"));
            materialised++;
        }

        return (materialised, skipped);
    }

    /// <summary>
    /// Stages only the projects this run actually rebuilt. A hit's bytes are already in
    /// <c>main</c> under exactly this fullHash, so copying them into this run's own store would
    /// be writing a second copy of something the next stage can read from main directly — on a
    /// fully warm run, that was the entire staging cost for nothing.
    /// </summary>
    private static int StageMisses(IReadOnlyList<PlanEntry> entries, string configuration, LocalVolumeArtifactStore prStore)
    {
        var staged = 0;

        foreach (var entry in entries.Where(entry => !entry.Hit))
        {
            var projectDirectory = Path.GetDirectoryName(entry.FullPath)!;

            var files = LocalVolumeArtifactStore.EnumerateAsSources(Path.Combine(projectDirectory, "bin"), "bin/")
                .Concat(LocalVolumeArtifactStore.EnumerateAsSources(Path.Combine(projectDirectory, "obj"), "obj/"));

            prStore.Put(
                new ProjectId(entry.ProjectPath),
                configuration,
                new StoredTargetHash(entry.OwnHash, entry.FullHash),
                files);
            staged++;
        }

        return staged;
    }

    private static void Touch(string directory)
    {
        if (!Directory.Exists(directory))
            return;

        var now = DateTime.UtcNow;
        foreach (var file in Directory.EnumerateFiles(directory, "*", SearchOption.AllDirectories))
            File.SetLastWriteTimeUtc(file, now);
    }
}
