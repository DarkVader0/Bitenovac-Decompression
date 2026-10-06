using Bitenovac.CloudBuild.Core.Graph;
using Bitenovac.CloudBuild.Core.Planning;
using Bitenovac.CloudBuild.MsBuild;
using Bitenovac.CloudBuild.Planning;
using Bitenovac.CloudBuild.Storage;

namespace Bitenovac.CloudBuild.Commands;

/// <summary>
/// Materialises every cache hit's <c>bin/</c> and <c>obj/</c> from <c>main</c>, then compiles
/// only what is left — the misses, and any hit <c>main</c> could not supply — in one
/// dependency-ordered MSBuild graph. A hit a miss references is part of that graph, and
/// MSBuild's up-to-date check (made trustworthy by <see cref="Touch"/>) skips it.
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

        var (materialised, skipped, unavailable) = MaterialiseHits(options, entries, configuration, mainStore);
        output.WriteLine($"{configuration}: materialised {materialised} cache hit(s) from main"
            + (skipped > 0 ? $", {skipped} already present" : "") + ".");

        var toBuild = entries.Where(entry => !entry.Hit || unavailable.Contains(entry)).ToList();
        if (toBuild.Count > 0)
        {
            var exitCode = Compile(options, configuration, toBuild, output);
            if (exitCode != 0)
                return exitCode;
        }

        var staged = StageMisses(entries, configuration, prStore);

        foreach (var entry in entries)
            MaterialisedMarker.Write(options.RepositoryRoot, new ProjectId(entry.ProjectPath), configuration, entry.FullHash);

        output.WriteLine($"{configuration}: build complete, staged {staged} rebuilt project(s).");
        return 0;
    }

    private static int Compile(PipelineOptions options, string configuration, IReadOnlyList<PlanEntry> toBuild, PipelineOutput output)
    {
        var unrestored = toBuild
            .Where(entry => !RestoreOutputs.Materialise(options.RepositoryRoot, entry.FullPath, options.RestoreOutputsRoot))
            .ToList();

        if (unrestored.Count > 0)
        {
            output.WriteLine($"==> Restoring {unrestored.Count} project(s) plan did not restore");
            var restoreExitCode = MsBuildRunner.Restore(
                options.RepositoryRoot,
                unrestored.Select(entry => entry.FullPath),
                configuration,
                options.SyntheticSolutionPath($"restore-{configuration}"));

            if (restoreExitCode != 0)
            {
                output.WriteError($"error: restore failed (exit {restoreExitCode}).");
                return restoreExitCode;
            }
        }

        output.WriteLine($"==> Building {toBuild.Count} project(s) ({configuration})");
        var exitCode = MsBuildRunner.Build(
            options.RepositoryRoot,
            toBuild.Select(entry => entry.FullPath),
            configuration,
            options.SyntheticSolutionPath($"build-{configuration}"));

        if (exitCode != 0)
            output.WriteError($"error: build failed (exit {exitCode}).");

        return exitCode;
    }

    private static (int Materialised, int Skipped, HashSet<PlanEntry> Unavailable) MaterialiseHits(
        PipelineOptions options, IReadOnlyList<PlanEntry> entries, string configuration, LocalVolumeArtifactStore mainStore)
    {
        var materialised = 0;
        var skipped = 0;
        var unavailable = new HashSet<PlanEntry>();

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
            {
                unavailable.Add(entry);
                continue;
            }

            Touch(Path.Combine(projectDirectory, "bin"));
            Touch(Path.Combine(projectDirectory, "obj"));
            materialised++;
        }

        return (materialised, skipped, unavailable);
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
