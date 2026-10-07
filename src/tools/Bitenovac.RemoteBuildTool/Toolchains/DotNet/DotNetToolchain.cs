using Bitenovac.RemoteBuildTool.MsBuild;
using Bitenovac.RemoteBuildTool.Planning;
using Bitenovac.RemoteBuildTool.Testing;

namespace Bitenovac.RemoteBuildTool.Toolchains.DotNet;

/// <summary>
/// MSBuild projects: evaluated in-process, restored and built through one synthetic solution, and
/// tested by running each Microsoft.Testing.Platform test project's own executable.
/// </summary>
internal sealed class DotNetToolchain(string repositoryRoot) : IToolchain
{
    public const string ToolchainName = "dotnet";

    /// <summary>The test executable MSBuild reports; the apphost for an MTP test project.</summary>
    public const string RunCommandProperty = "RunCommand";

    public const string TargetFrameworksProperty = "TargetFrameworks";

    private MsBuildProjectEvaluator? _evaluator;

    public string Name => ToolchainName;

    public IReadOnlyList<string> OutputDirectories { get; } = ["bin", "obj"];

    public IReadOnlyList<string> Discover() =>
        ProjectDiscovery.FindRelativePaths(repositoryRoot);

    // Created on first use: build and test never evaluate, and one collection must serve every
    // evaluation, since loading the same project twice into it throws.
    public IReadOnlyList<EvaluatedProject> Evaluate(IReadOnlyList<string> relativePaths, string configuration) =>
        [.. (_evaluator ??= new MsBuildProjectEvaluator(repositoryRoot)).EvaluateAll(relativePaths, configuration).Values];

    public ToolchainPreparation Prepare(
        PipelineOptions options,
        IReadOnlyDictionary<string, IReadOnlyList<EvaluatedProject>> byConfiguration,
        PipelineOutput output)
    {
        var debug = byConfiguration["Debug"];

        var verifyExitCode = Verify(debug, output);
        if (verifyExitCode != 0)
            return ToolchainPreparation.Failed(verifyExitCode);

        output.WriteLine("==> Restoring");
        var restoreExitCode = MsBuildRunner.Restore(
            options.RepositoryRoot,
            debug.Select(project => project.FullPath),
            configuration: "Debug",
            options.SyntheticSolutionPath("restore"));

        if (restoreExitCode != 0)
        {
            output.WriteError($"error: restore failed (exit {restoreExitCode}).");
            return ToolchainPreparation.Failed(restoreExitCode);
        }

        var refreshed = byConfiguration.ToDictionary(
            pair => pair.Key,
            pair => (IReadOnlyList<EvaluatedProject>)[.. pair.Value.Select(MsBuildProjectEvaluator.RefreshPackageClosure)]);

        RestoreOutputs.Save(options.RepositoryRoot, refreshed["Debug"].Select(project => project.FullPath), options.RestoreOutputsRoot);

        return new ToolchainPreparation(0, refreshed);
    }

    public int Build(PipelineOptions options, IReadOnlyList<PlanEntry> entries, string configuration, PipelineOutput output)
    {
        var unrestored = entries
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

        output.WriteLine($"==> Building {entries.Count} project(s) ({configuration})");
        var exitCode = MsBuildRunner.Build(
            options.RepositoryRoot,
            entries.Select(entry => entry.FullPath),
            configuration,
            options.SyntheticSolutionPath($"build-{configuration}"));

        if (exitCode != 0)
            output.WriteError($"error: build failed (exit {exitCode}).");

        return exitCode;
    }

    public TestRunResult RunTests(PlanEntry entry, string configuration, string resultsFilePrefix, string resultsDirectory)
    {
        var executable = entry.Properties.GetValueOrDefault(RunCommandProperty, "");
        if (!File.Exists(executable))
            throw new InvalidOperationException($"No built test executable found for {entry.ProjectPath} ({configuration}) at '{executable}'. Run 'build' first.");

        return TestRunner.Run(executable, resultsFilePrefix, resultsDirectory);
    }

    public void Dispose() => _evaluator?.Dispose();

    private static int Verify(IReadOnlyList<EvaluatedProject> projects, PipelineOutput output)
    {
        output.WriteLine("==> Verifying build assumptions");

        var offenders = projects
            .Where(project => !string.IsNullOrEmpty(project.Properties.GetValueOrDefault(TargetFrameworksProperty)))
            .ToList();

        if (offenders.Count > 0)
        {
            output.WriteError("error: these projects declare <TargetFrameworks>. Set a single <TargetFramework>, or change RepositoryTargetFramework in Directory.Build.props.");
            foreach (var project in offenders)
                output.WriteError($"  {project.Id}");
            return 1;
        }

        output.WriteLine("OK: every project targets a single framework.");
        return 0;
    }
}
