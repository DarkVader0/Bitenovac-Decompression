using Bitenovac.CloudBuild.Core.Graph;
using Bitenovac.CloudBuild.Planning;

namespace Bitenovac.CloudBuild.Unit.Tests;

/// <summary>Builds the throwaway directories, plan entries and options the CloudBuild tests share.</summary>
internal static class TestFactory
{
    public static ProjectId Id(string relativePath) => new(relativePath);

    /// <summary>An output nothing reads, for the tests that only assert on an exit code or the filesystem.</summary>
    public static PipelineOutput Silence() => new(TextWriter.Null, TextWriter.Null);

    public static TemporaryDirectory Directory() => new();

    /// <summary>Writes <paramref name="content"/> to <paramref name="path"/>, creating its directory.</summary>
    public static string WriteFile(string path, string content)
    {
        System.IO.Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, content);
        return path;
    }

    /// <summary>Writes <paramref name="content"/> to <paramref name="root"/> plus the given path segments.</summary>
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
        string assemblyName = "Project",
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
            assemblyName,
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
/// Overwrites the <c>CLOUDBUILD_*</c> variables for one test and puts back whatever the process
/// had before.
/// </summary>
internal sealed class EnvironmentVariables : IDisposable
{
    /// <summary>Names the xUnit collection every class that writes these variables belongs to.</summary>
    public const string Collection = "Environment variables";

    private static readonly string[] Names =
    [
        "CLOUDBUILD_REPO_ROOT",
        "CLOUDBUILD_MAIN_STORE",
        "CLOUDBUILD_PR_STORE",
        "CLOUDBUILD_CACHELESS",
        "CLOUDBUILD_COVERAGE_HTML",
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

/// <summary>A directory under the system temporary folder, removed when the test finishes with it.</summary>
internal sealed class TemporaryDirectory : IDisposable
{
    public TemporaryDirectory() =>
        Path = System.IO.Directory.CreateTempSubdirectory("bitenovac-cloudbuild-tests-").FullName;

    public string Path { get; }

    /// <summary>This directory's path plus <paramref name="relativePath"/>, with forward slashes accepted.</summary>
    public string Combine(string relativePath) =>
        System.IO.Path.Combine(Path, relativePath.Replace('/', System.IO.Path.DirectorySeparatorChar));

    public void Dispose()
    {
        if (System.IO.Directory.Exists(Path))
            System.IO.Directory.Delete(Path, recursive: true);
    }
}

/// <summary>Collects what a command reported, so a test can read it without touching the console.</summary>
internal sealed class CapturedOutput
{
    private readonly StringWriter _out = new();
    private readonly StringWriter _error = new();

    public PipelineOutput Pipeline => new(_out, _error);

    public string Out => _out.ToString();

    public string Error => _error.ToString();
}
