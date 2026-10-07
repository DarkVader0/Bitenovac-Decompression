using Bitenovac.RemoteBuildTool.Core.Coverage;
using Bitenovac.RemoteBuildTool.Core.Graph;
using Bitenovac.RemoteBuildTool.Core.Planning;
using Bitenovac.RemoteBuildTool.Planning;
using Bitenovac.RemoteBuildTool.Storage;
using Bitenovac.RemoteBuildTool.Testing;
using Bitenovac.RemoteBuildTool.Toolchains;

namespace Bitenovac.RemoteBuildTool.Commands;

/// <summary>
/// Runs — or reuses — every selected test project's result, then gates coverage on Debug.
/// A test project whose plan decision is a hit and whose own
/// <c>&lt;CacheTestResults&gt;</c> policy allows it reuses its cached cobertura report straight
/// from <c>main</c> rather than re-running: the hit means its fullHash, and by construction its
/// own hash too, are unchanged from the stored entry, so the result <c>main</c> holds is exactly
/// what re-running would produce.
/// </summary>
internal static class TestCommand
{
    /// <summary>The part of a stored entry holding a cached test result.</summary>
    private static readonly string[] TestResultPrefixes = ["tests/"];

    public static int Run(PipelineOptions options, string configuration, PipelineOutput output)
    {
        var plan = PlanState.Load(options.PlanFile);
        var entries = plan.For(configuration);
        if (entries.Count == 0)
        {
            output.WriteLine("Nothing selected; nothing to test.");
            return 0;
        }

        using var toolchains = ToolchainRegistry.Create(options.RepositoryRoot);
        var prStore = new LocalVolumeArtifactStore(options.PrStoreRoot);
        var mainStore = new LocalVolumeArtifactStore(options.MainStoreRoot);

        if (!MaterialiseBuildOutputs(output, options, entries, configuration, prStore, mainStore, toolchains))
            return 1;

        var testEntries = entries.Where(entry => entry.IsTestProject).ToList();
        if (testEntries.Count == 0)
        {
            output.WriteLine("No selected test projects; nothing to run.");
            return 0;
        }

        var coverageDirectory = options.CoverageDirectory(configuration);
        if (Directory.Exists(coverageDirectory))
            Directory.Delete(coverageDirectory, recursive: true);
        Directory.CreateDirectory(coverageDirectory);

        output.WriteLine($"==> Testing {testEntries.Count} project(s) ({configuration})");

        var failed = new List<string>();
        var noTestsRan = new List<string>();
        var toRun = new List<(PlanEntry Entry, string Name)>();

        foreach (var entry in testEntries)
        {
            var name = Path.GetFileNameWithoutExtension(entry.ProjectPath);

            if (entry.Hit && entry.CacheTestResults && ReuseCachedResult(new ProjectId(entry.ProjectPath), configuration, name, mainStore, coverageDirectory))
            {
                output.WriteLine($"--- {name} (reused)");
                continue;
            }

            toRun.Add((entry, name));
        }

        var results = new TestRunResult[toRun.Count];
        var outputGate = new Lock();

        Parallel.For(0, toRun.Count, new ParallelOptions { MaxDegreeOfParallelism = PipelineOptions.MaxParallelism() }, index =>
        {
            var (entry, name) = toRun[index];
            try
            {
                results[index] = toolchains.For(entry.Toolchain).RunTests(entry, configuration, name, coverageDirectory);
            }
            catch (InvalidOperationException exception)
            {
                results[index] = new TestRunResult(TestRunOutcome.Failed, -1, coverageDirectory, exception.Message + Environment.NewLine);
            }

            lock (outputGate)
            {
                output.WriteLine($"--- {name}");
                output.Out.Write(results[index].Output);
            }
        });

        for (var index = 0; index < toRun.Count; index++)
        {
            var (entry, name) = toRun[index];
            var result = results[index];

            switch (result.Outcome)
            {
                case TestRunOutcome.Passed:
                    if (entry.CacheTestResults)
                        StageTestResult(new ProjectId(entry.ProjectPath), configuration, name, coverageDirectory, entry, prStore);
                    break;
                case TestRunOutcome.NoTestsRan:
                    noTestsRan.Add(name);
                    break;
                case TestRunOutcome.Failed:
                    failed.Add($"{name} (exit {result.ExitCode})");
                    break;
            }
        }

        if (noTestsRan.Count > 0)
            output.WriteLine($"warning: test projects that ran no tests: {string.Join(", ", noTestsRan)}");

        if (failed.Count > 0)
        {
            output.WriteError($"Test projects with failing tests:\n  {string.Join("\n  ", failed)}");
            return 1;
        }

        output.WriteLine($"All selected tests passed ({configuration}).");

        if (configuration != "Debug")
            return 0;

        return RunCoverageGate(entries, coverageDirectory, options, output);
    }

    private static bool MaterialiseBuildOutputs(
        PipelineOutput output,
        PipelineOptions options,
        IReadOnlyList<PlanEntry> entries,
        string configuration,
        LocalVolumeArtifactStore prStore,
        LocalVolumeArtifactStore mainStore,
        ToolchainRegistry toolchains)
    {
        var materialised = 0;
        var skipped = 0;

        foreach (var entry in entries)
        {
            var project = new ProjectId(entry.ProjectPath);
            var projectDirectory = Path.GetDirectoryName(entry.FullPath)!;
            var outputDirectories = toolchains.For(entry.Toolchain).OutputDirectories;

            if (MaterialisedMarker.Matches(options.RepositoryRoot, project, configuration, entry.FullHash, Path.Combine(projectDirectory, outputDirectories[0])))
            {
                skipped++;
                continue;
            }

            var source = entry.Hit ? mainStore : prStore;

            if (!source.TryGet(project, configuration, projectDirectory, out _, [.. outputDirectories.Select(directory => directory + "/")]))
            {
                output.WriteError($"error: no build output available for {entry.ProjectPath}. Run 'build {configuration}' first.");
                return false;
            }

            MaterialisedMarker.Write(options.RepositoryRoot, project, configuration, entry.FullHash);
            materialised++;
        }

        if (materialised > 0 || skipped > 0)
            output.WriteLine($"{configuration}: materialised {materialised} project(s)"
                + (skipped > 0 ? $", {skipped} already present" : "") + ".");

        return true;
    }

    private static bool ReuseCachedResult(ProjectId project, string configuration, string name, LocalVolumeArtifactStore mainStore, string coverageDirectory)
    {
        var staging = Directory.CreateTempSubdirectory("bitenovac-remotebuildtool-reuse-").FullName;
        try
        {
            if (!mainStore.TryGet(project, configuration, staging, out _, TestResultPrefixes))
                return false;

            var testsDirectory = Path.Combine(staging, "tests");
            if (!Directory.Exists(testsDirectory))
                return false;

            foreach (var file in Directory.EnumerateFiles(testsDirectory))
                File.Copy(file, Path.Combine(coverageDirectory, Path.GetFileName(file)), overwrite: true);

            return true;
        }
        finally
        {
            Directory.Delete(staging, recursive: true);
        }
    }

    private static void StageTestResult(ProjectId project, string configuration, string name, string coverageDirectory, PlanEntry entry, LocalVolumeArtifactStore prStore)
    {
        var staging = Directory.CreateTempSubdirectory("bitenovac-remotebuildtool-stage-").FullName;
        try
        {
            var testsDirectory = Path.Combine(staging, "tests");
            Directory.CreateDirectory(testsDirectory);
            foreach (var file in Directory.EnumerateFiles(coverageDirectory, $"{name}*"))
                File.Copy(file, Path.Combine(testsDirectory, Path.GetFileName(file)), overwrite: true);

            prStore.AddFiles(project, configuration, new StoredTargetHash(entry.OwnHash, entry.FullHash), staging);
        }
        finally
        {
            Directory.Delete(staging, recursive: true);
        }
    }

    private static int RunCoverageGate(IReadOnlyList<PlanEntry> entries, string coverageDirectory, PipelineOptions options, PipelineOutput output)
    {
        var gated = entries
            .Where(entry => entry.ShouldGateCoverage && !entry.ExcludeFromCoverage)
            .ToDictionary(
                entry => new ProjectId(entry.ProjectPath),
                entry => new CoveragePolicy(entry.MinimumLineCoverage, entry.MinimumBranchCoverage));

        if (gated.Count == 0)
        {
            output.WriteLine("No selected project is under the coverage gate; nothing to check.");
            return 0;
        }

        if (!Directory.EnumerateFiles(coverageDirectory, "*cobertura*.xml").Any())
        {
            output.WriteError($"error: no coverage reports under {coverageDirectory}. Did 'test' run?");
            return 1;
        }

        output.WriteLine("==> Merging coverage reports");
        var reportDirectory = options.CoverageReportDirectory("Debug");
        var merged = CoverageReportGenerator.Merge(options.RepositoryRoot, coverageDirectory, reportDirectory, options.CoverageHtml);
        var measuredByName = CoverageReportReader.Read(merged);

        var coverageNameByProject = entries.ToDictionary(entry => new ProjectId(entry.ProjectPath), entry => entry.CoverageName);
        var measured = gated.Keys
            .Where(project => measuredByName.ContainsKey(coverageNameByProject[project]))
            .ToDictionary(project => project, project => measuredByName[coverageNameByProject[project]]);

        var results = CoverageGate.Evaluate(gated, measured);
        var failures = results.Where(result => !result.Passed).ToList();

        foreach (var result in results)
            output.WriteLine(result.Passed ? $"  OK   {result.Project}: {result.Reason}" : $"  FAIL {result.Reason}");

        if (failures.Count > 0)
        {
            output.WriteError($"Open {reportDirectory}/index.html to see which lines are uncovered.");
            return 1;
        }

        output.WriteLine("Every gated project meets its coverage policy.");
        return 0;
    }
}
