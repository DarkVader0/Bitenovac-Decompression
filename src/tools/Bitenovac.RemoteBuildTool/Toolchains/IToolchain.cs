using Bitenovac.RemoteBuildTool.Planning;
using Bitenovac.RemoteBuildTool.Testing;

namespace Bitenovac.RemoteBuildTool.Toolchains;

/// <summary>
/// Defines a build system that the pipeline drives: everything language-specific between a
/// project file on disk and a Cobertura report.
/// </summary>
/// <remarks>
/// <para>
/// The graph, hashing, cache decisions, artifact store and coverage gate are shared by every
/// toolchain and are independent of any of them.
/// </para>
/// <para>A toolchain owes the rest of the pipeline these guarantees:</para>
/// <list type="bullet">
/// <item>
/// <see cref="Discover"/> claims project files that no other toolchain claims. A project is
/// identified by the repository-relative path of its file.
/// </item>
/// <item>
/// <see cref="EvaluatedProject.OwnHashInputs"/> covers everything that can change the project's
/// output: sources, build settings, resolved dependencies and the toolchain's own version.
/// Nothing in the shared hash records which compiler or SDK the image holds.
/// </item>
/// <item>
/// <see cref="EvaluatedProject.ProjectReferences"/> may name projects of other toolchains. Each
/// toolchain builds its misses in one <see cref="Build"/> call, so toolchains are ordered by
/// those references and must not depend on each other in a cycle.
/// </item>
/// <item>
/// Build output stays under <see cref="OutputDirectories"/> inside the project's directory,
/// because those are all the artifact store keeps. The store keeps file content only, not modes
/// or links.
/// </item>
/// <item>
/// <see cref="RunTests"/> writes Cobertura reports into the results directory, each file name
/// starting with the prefix it is given, and reports coverage under
/// <see cref="EvaluatedProject.CoverageName"/>.
/// </item>
/// </list>
/// </remarks>
internal interface IToolchain : IDisposable
{
    /// <summary>
    /// Gets the name that plan entries record, so later stages find the toolchain that owns them.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets the directories, relative to a project's directory, that hold its build output.
    /// </summary>
    /// <value>
    /// The output directories. The workspace no longer holds the project's output if the first
    /// one is absent.
    /// </value>
    IReadOnlyList<string> OutputDirectories { get; }

    /// <summary>
    /// Returns every project file this toolchain owns in the repository.
    /// </summary>
    /// <returns>The repository-relative paths of the project files, ordered ordinally.</returns>
    IReadOnlyList<string> Discover();

    /// <summary>
    /// Reads each project for one configuration without restoring or building anything.
    /// </summary>
    IReadOnlyList<EvaluatedProject> Evaluate(IReadOnlyList<string> relativePaths, string configuration);

    /// <summary>
    /// Checks this toolchain's build assumptions and restores dependencies.
    /// </summary>
    /// <returns>
    /// The projects, with whatever restore resolved added to their own-hash inputs, or the exit
    /// code of the failure.
    /// </returns>
    ToolchainPreparation Prepare(
        PipelineOptions options,
        IReadOnlyDictionary<string, IReadOnlyList<EvaluatedProject>> byConfiguration,
        PipelineOutput output);

    /// <summary>
    /// Builds the specified plan entries.
    /// </summary>
    /// <returns>The process exit code.</returns>
    /// <remarks>
    /// Every other selected project that <paramref name="entries"/> reference has already been
    /// built or materialized.
    /// </remarks>
    int Build(PipelineOptions options, IReadOnlyList<PlanEntry> entries, string configuration, PipelineOutput output);

    /// <summary>
    /// Runs one built test project, collecting coverage into <paramref name="resultsDirectory"/>.
    /// </summary>
    /// <exception cref="InvalidOperationException">The project's test executable has not been built.</exception>
    TestRunResult RunTests(PlanEntry entry, string configuration, string resultsFilePrefix, string resultsDirectory);
}
