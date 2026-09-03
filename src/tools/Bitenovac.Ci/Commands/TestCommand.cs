using Bitenovac.Ci.Core.Coverage;
using Bitenovac.Ci.Core.Graph;
using Bitenovac.Ci.Core.Planning;
using Bitenovac.Ci.Planning;
using Bitenovac.Ci.Storage;
using Bitenovac.Ci.Testing;

namespace Bitenovac.Ci.Commands;

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
    /// <summary>The parts of a stored entry that belong in a source tree. <c>tests/</c> does not.</summary>
    private static readonly string[] MaterialisedPrefixes = ["bin/", "obj/"];

    /// <summary>The part of a stored entry holding a cached test result.</summary>
    private static readonly string[] TestResultPrefixes = ["tests/"];

    public static int Run(PipelineOptions options, string configuration)
    {
        var plan = PlanState.Load(options.PlanFile);
        var entries = plan.For(configuration);
        if (entries.Count == 0)
        {
            Console.WriteLine("Nothing selected; nothing to test.");
            return 0;
        }

        var prStore = new LocalVolumeArtifactStore(options.PrStoreRoot);
        var mainStore = new LocalVolumeArtifactStore(options.MainStoreRoot);

        if (!MaterialiseBuildOutputs(options, entries, configuration, prStore, mainStore))
            return 1;

        var testEntries = entries.Where(entry => entry.IsTestProject).ToList();
        if (testEntries.Count == 0)
        {
            Console.WriteLine("No selected test projects; nothing to run.");
            return 0;
        }

        var coverageDirectory = options.CoverageDirectory(configuration);
        if (Directory.Exists(coverageDirectory))
            Directory.Delete(coverageDirectory, recursive: true);
        Directory.CreateDirectory(coverageDirectory);

        Console.WriteLine($"==> Testing {testEntries.Count} project(s) ({configuration})");

        var failed = new List<string>();
        var noTestsRan = new List<string>();

        foreach (var entry in testEntries)
        {
            var name = Path.GetFileNameWithoutExtension(entry.ProjectPath);
            var projectId = new ProjectId(entry.ProjectPath);

            if (entry.Hit && entry.CacheTestResults && ReuseCachedResult(projectId, configuration, name, mainStore, coverageDirectory))
            {
                Console.WriteLine($"--- {name} (reused)");
                continue;
            }

            Console.WriteLine($"--- {name}");
            var assemblyPath = ResolveAssemblyPath(entry, configuration);
            var result = TestRunner.Run(assemblyPath, name, coverageDirectory);

            switch (result.Outcome)
            {
                case TestRunOutcome.Passed:
                    if (entry.CacheTestResults)
                        StageTestResult(projectId, configuration, name, coverageDirectory, entry, prStore);
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
            Console.WriteLine($"warning: test projects that ran no tests: {string.Join(", ", noTestsRan)}");

        if (failed.Count > 0)
        {
            Console.Error.WriteLine($"Test projects with failing tests:\n  {string.Join("\n  ", failed)}");
            return 1;
        }

        Console.WriteLine($"All selected tests passed ({configuration}).");

        if (configuration != "Debug")
            return 0;

        return RunCoverageGate(entries, coverageDirectory, options);
    }

    private static bool MaterialiseBuildOutputs(
        PipelineOptions options,
        IReadOnlyList<PlanEntry> entries,
        string configuration,
        LocalVolumeArtifactStore prStore,
        LocalVolumeArtifactStore mainStore)
    {
        var materialised = 0;
        var skipped = 0;

        foreach (var entry in entries)
        {
            var project = new ProjectId(entry.ProjectPath);
            var projectDirectory = Path.GetDirectoryName(entry.FullPath)!;

            // 'build' left this workspace holding exactly this hash. True locally, where both
            // stages share a checkout; false in CI, where 'test' runs in its own container
            // against its own checkout and no marker exists — so it materialises there.
            if (MaterialisedMarker.Matches(options.RepositoryRoot, project, configuration, entry.FullHash, projectDirectory))
            {
                skipped++;
                continue;
            }

            // A hit was never staged into this run's store — its bytes are already in main under
            // this exact hash, so that is where it comes from. Only rebuilt projects are staged.
            var source = entry.Hit ? mainStore : prStore;

            // Written straight into the project, filtered to bin/ and obj/: an entry also holds
            // tests/, which does not belong in a source tree.
            if (!source.TryGet(project, configuration, projectDirectory, out _, MaterialisedPrefixes))
            {
                Console.Error.WriteLine($"error: no build output available for {entry.ProjectPath}. Run 'build {configuration}' first.");
                return false;
            }

            MaterialisedMarker.Write(options.RepositoryRoot, project, configuration, entry.FullHash);
            materialised++;
        }

        if (materialised > 0 || skipped > 0)
            Console.WriteLine($"{configuration}: materialised {materialised} project(s)"
                + (skipped > 0 ? $", {skipped} already present" : "") + ".");

        return true;
    }

    private static string ResolveAssemblyPath(PlanEntry entry, string configuration)
    {
        // Scoped to bin/<Configuration>: a project built in both configurations (as every one
        // is, across the Debug and Release jobs) has both subfolders on disk at once, and
        // searching all of bin/ for a matching file name would find whichever configuration's
        // build happened to run first rather than the one this test run is actually for.
        var binDirectory = Path.Combine(Path.GetDirectoryName(entry.FullPath)!, "bin", configuration);
        var candidates = Directory.Exists(binDirectory)
            ? Directory.EnumerateFiles(binDirectory, $"{entry.AssemblyName}.dll", SearchOption.AllDirectories)
            : [];

        return candidates.FirstOrDefault()
            ?? throw new InvalidOperationException($"No built assembly found for {entry.ProjectPath} ({configuration}). Run 'build' first.");
    }

    private static bool ReuseCachedResult(ProjectId project, string configuration, string name, LocalVolumeArtifactStore mainStore, string coverageDirectory)
    {
        var staging = Directory.CreateTempSubdirectory("bitenovac-ci-reuse-").FullName;
        try
        {
            // Only tests/: unpacking the whole entry here would copy the project's entire bin and
            // obj out to a temporary directory purely to read a couple of coverage reports.
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
        var staging = Directory.CreateTempSubdirectory("bitenovac-ci-stage-").FullName;
        try
        {
            // Only the coverage files: 'build' already stored this project's bin and obj under
            // the same hash, so AddFiles extends that entry rather than re-hashing all of it.
            var testsDirectory = Path.Combine(staging, "tests");
            Directory.CreateDirectory(testsDirectory);
            foreach (var file in Directory.EnumerateFiles(coverageDirectory, $"{name}*"))
                File.Copy(file, Path.Combine(testsDirectory, Path.GetFileName(file)), overwrite: true);

            // False for a cache hit whose tests were re-run anyway (CacheTestResults=false):
            // 'build' never staged it, because main already holds it. Nothing to extend, and
            // nothing to promote either, so there is nothing to do.
            prStore.AddFiles(project, configuration, new StoredTargetHash(entry.OwnHash, entry.FullHash), staging);
        }
        finally
        {
            Directory.Delete(staging, recursive: true);
        }
    }


    private static int RunCoverageGate(IReadOnlyList<PlanEntry> entries, string coverageDirectory, PipelineOptions options)
    {
        var gated = entries
            .Where(entry => entry.ShouldGateCoverage && !entry.ExcludeFromCoverage)
            .ToDictionary(
                entry => new ProjectId(entry.ProjectPath),
                entry => new CoveragePolicy(entry.MinimumLineCoverage, entry.MinimumBranchCoverage));

        if (gated.Count == 0)
        {
            Console.WriteLine("No selected project is under the coverage gate; nothing to check.");
            return 0;
        }

        if (!Directory.EnumerateFiles(coverageDirectory, "*cobertura*.xml").Any())
        {
            Console.Error.WriteLine($"error: no coverage reports under {coverageDirectory}. Did 'test' run?");
            return 1;
        }

        Console.WriteLine("==> Merging coverage reports");
        var reportDirectory = options.CoverageReportDirectory("Debug");
        var merged = CoverageReportGenerator.Merge(options.RepositoryRoot, coverageDirectory, reportDirectory, options.CoverageHtml);
        var measuredByAssembly = CoverageReportReader.Read(merged);

        var assemblyByProject = entries.ToDictionary(entry => new ProjectId(entry.ProjectPath), entry => entry.AssemblyName);
        var measured = gated.Keys
            .Where(project => measuredByAssembly.ContainsKey(assemblyByProject[project]))
            .ToDictionary(project => project, project => measuredByAssembly[assemblyByProject[project]]);

        var results = CoverageGate.Evaluate(gated, measured);
        var failures = results.Where(result => !result.Passed).ToList();

        foreach (var result in results)
            Console.WriteLine(result.Passed ? $"  OK   {result.Project}: {result.Reason}" : $"  FAIL {result.Reason}");

        if (failures.Count > 0)
        {
            Console.Error.WriteLine($"Open {reportDirectory}/index.html to see which lines are uncovered.");
            return 1;
        }

        Console.WriteLine("Every gated project meets its coverage policy.");
        return 0;
    }
}
