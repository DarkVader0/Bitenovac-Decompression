using Bitenovac.RemoteBuildTool.Core.Graph;
using Bitenovac.RemoteBuildTool.Planning;
using Bitenovac.RemoteBuildTool.Toolchains.DotNet;

namespace Bitenovac.RemoteBuildTool.Unit.Tests;

/// <summary>
/// Provides the temporary directories, plan entries and options shared by the RemoteBuildTool tests.
/// </summary>
internal static class TestFactory
{
    public static ProjectId Id(string relativePath) => new(relativePath);

    /// <summary>
    /// Creates an output that discards everything written to it.
    /// </summary>
    public static PipelineOutput Silence() => new(TextWriter.Null, TextWriter.Null);

    public static TemporaryDirectory Directory() => new();

    /// <summary>
    /// Writes <paramref name="content"/> to <paramref name="path"/>, creating its directory.
    /// </summary>
    public static string WriteFile(string path, string content)
    {
        System.IO.Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, content);
        return path;
    }

    /// <summary>
    /// Writes <paramref name="content"/> to <paramref name="relativePath"/> under <paramref name="root"/>.
    /// </summary>
    public static string WriteFile(string root, string relativePath, string content) =>
        WriteFile(Path.Combine(root, relativePath.Replace('/', Path.DirectorySeparatorChar)), content);

    public static PipelineOptions Options(
        string repositoryRoot,
        string mainStoreRoot,
        string prStoreRoot,
        bool cacheless = false,
        bool coverageHtml = false) =>
        new(repositoryRoot, mainStoreRoot, prStoreRoot, cacheless, coverageHtml);

    public static PlanEntry Entry(
        string projectPath,
        string fullPath = @"C:\repo\Project.csproj",
        string toolchain = DotNetToolchain.ToolchainName,
        IReadOnlyList<string>? references = null,
        string coverageName = "Project",
        IReadOnlyDictionary<string, string>? properties = null,
        bool isTestProject = false,
        bool excludeFromCoverage = false,
        double minimumLineCoverage = 100,
        double minimumBranchCoverage = 100,
        bool cacheTestResults = true,
        string ownHash = "own-hash",
        string fullHash = "full-hash",
        bool forced = false,
        bool hit = false,
        bool shouldGateCoverage = true) =>
        new(
            projectPath,
            fullPath,
            toolchain,
            references ?? [],
            coverageName,
            properties ?? new Dictionary<string, string>(),
            isTestProject,
            excludeFromCoverage,
            minimumLineCoverage,
            minimumBranchCoverage,
            cacheTestResults,
            ownHash,
            fullHash,
            forced,
            hit,
            shouldGateCoverage);

    public static PlanState Plan(string configuration, params PlanEntry[] entries) =>
        new(new Dictionary<string, List<PlanEntry>> { [configuration] = [.. entries] });
}

/// <summary>
/// Represents the <c>REMOTEBUILDTOOL_*</c> environment variables of one test, restored to their
/// previous values on disposal.
/// </summary>
internal sealed class EnvironmentVariables : IDisposable
{
    /// <summary>
    /// The name of the xUnit collection that every class writing these variables belongs to.
    /// </summary>
    public const string Collection = "Environment variables";

    private static readonly string[] Names =
    [
        "REMOTEBUILDTOOL_REPO_ROOT",
        "REMOTEBUILDTOOL_MAIN_STORE",
        "REMOTEBUILDTOOL_PR_STORE",
        "REMOTEBUILDTOOL_CACHELESS",
        "REMOTEBUILDTOOL_COVERAGE_HTML",
        "REMOTEBUILDTOOL_MAX_CPU",
    ];

    private readonly Dictionary<string, string?> _original =
        Names.ToDictionary(name => name, Environment.GetEnvironmentVariable);

    public void Clear()
    {
        foreach (var name in Names)
            Environment.SetEnvironmentVariable(name, null);
    }

    public void Set(string name, string? value) => Environment.SetEnvironmentVariable(name, value);

    public void Dispose()
    {
        foreach (var (name, value) in _original)
            Environment.SetEnvironmentVariable(name, value);
    }
}

/// <summary>
/// Represents a directory under the system temporary folder that is deleted on disposal.
/// </summary>
internal sealed class TemporaryDirectory : IDisposable
{
    public TemporaryDirectory() =>
        Path = System.IO.Directory.CreateTempSubdirectory("bitenovac-remotebuildtool-tests-").FullName;

    public string Path { get; }

    /// <summary>
    /// Returns the path of <paramref name="relativePath"/> under this directory, accepting forward slashes.
    /// </summary>
    public string Combine(string relativePath) =>
        System.IO.Path.Combine(Path, relativePath.Replace('/', System.IO.Path.DirectorySeparatorChar));

    public void Dispose()
    {
        if (System.IO.Directory.Exists(Path))
            System.IO.Directory.Delete(Path, recursive: true);
    }
}

/// <summary>
/// Represents the output a command reported, captured without touching the console.
/// </summary>
internal sealed class CapturedOutput
{
    private readonly StringWriter _out = new();
    private readonly StringWriter _error = new();

    public PipelineOutput Pipeline => new(_out, _error);

    public string Out => _out.ToString();

    public string Error => _error.ToString();
}
