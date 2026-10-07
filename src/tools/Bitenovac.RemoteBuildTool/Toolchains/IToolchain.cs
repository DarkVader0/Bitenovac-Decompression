using Bitenovac.RemoteBuildTool.Planning;
using Bitenovac.RemoteBuildTool.Testing;

namespace Bitenovac.RemoteBuildTool.Toolchains;

/// <summary>
/// One build system the pipeline drives — everything language-specific between a project file
/// on disk and a cobertura report. The graph, hashing, cache decisions, artifact store and
/// coverage gate are shared by every toolchain and know nothing about any of them.
/// </summary>
/// <remarks>
/// <para>A toolchain owes the rest of the pipeline these guarantees:</para>
/// <list type="bullet">
/// <item>
/// <see cref="Discover"/> claims project files no other toolchain claims. A project is
/// identified by its file's repository-relative path.
/// </item>
/// <item>
/// <see cref="EvaluatedProject.OwnHashInputs"/> covers everything that can change the project's
/// output: sources, build settings, resolved dependencies, and the toolchain's own version —
/// nothing in the shared hash knows what compiler or SDK the image holds.
/// </item>
/// <item>
/// <see cref="EvaluatedProject.ProjectReferences"/> may name projects of other toolchains. Each
/// toolchain builds its misses in one <see cref="Build"/> call, so toolchains are ordered by
/// those references and must not depend on each other in a cycle.
/// </item>
/// <item>
/// Build output stays under <see cref="OutputDirectories"/> inside the project's directory —
/// those are all the artifact store keeps. It keeps file content only, not modes or links.
/// </item>
/// <item>
/// <see cref="RunTests"/> writes cobertura reports into the results directory, each file name
/// starting with the prefix it is given, and reports coverage under
/// <see cref="EvaluatedProject.CoverageName"/>.
/// </item>
/// </list>
/// </remarks>
internal interface IToolchain : IDisposable
{
    /// <summary>The name plan entries record, so later stages find the toolchain that owns them.</summary>
    string Name { get; }

    /// <summary>
    /// Directories relative to a project's directory that hold its build output. The first is the
    /// one whose absence means the workspace no longer holds the project's output.
    /// </summary>
    IReadOnlyList<string> OutputDirectories { get; }

    /// <summary>Every project file this toolchain owns in the repository, repository-relative and sorted ordinally.</summary>
    IReadOnlyList<string> Discover();

    /// <summary>Reads each project for one configuration without restoring or building anything.</summary>
    IReadOnlyList<EvaluatedProject> Evaluate(IReadOnlyList<string> relativePaths, string configuration);

    /// <summary>
    /// Checks this toolchain's own build assumptions, restores dependencies, and returns the
    /// projects with whatever restore resolved folded into their own-hash inputs.
    /// </summary>
    ToolchainPreparation Prepare(
        PipelineOptions options,
        IReadOnlyDictionary<string, IReadOnlyList<EvaluatedProject>> byConfiguration,
        PipelineOutput output);

    /// <summary>
    /// Builds <paramref name="entries"/>, which every other selected project they reference has
    /// already been built or materialised for. Returns the process exit code.
    /// </summary>
    int Build(PipelineOptions options, IReadOnlyList<PlanEntry> entries, string configuration, PipelineOutput output);

    /// <summary>Runs one built test project, collecting coverage into <paramref name="resultsDirectory"/>.</summary>
    /// <exception cref="InvalidOperationException">The project's test executable has not been built.</exception>
    TestRunResult RunTests(PlanEntry entry, string configuration, string resultsFilePrefix, string resultsDirectory);
}
