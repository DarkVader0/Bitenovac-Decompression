using System.Security.Cryptography;
using Bitenovac.CloudBuild.Core.Graph;
using Bitenovac.CloudBuild.Hashing;
using Microsoft.Build.Evaluation;

namespace Bitenovac.CloudBuild.MsBuild;

/// <summary>
/// Evaluates every discovered project in-process, once per configuration, replacing the old
/// <c>build/project-info.proj</c> + <c>CiProjectInfo</c> target: a plain MSBuild item or property
/// is already queryable through the evaluation API without running any target, so nothing needs
/// to be built or executed to read this information — only restored, for the resolved package
/// closure (see <see cref="PackageClosureReader"/>).
/// </summary>
internal sealed class MsBuildProjectEvaluator : IDisposable
{
    private readonly string _repositoryRoot;
    private readonly ProjectCollection _collection = new();
    private readonly SharedHashInputs _shared;

    public MsBuildProjectEvaluator(string repositoryRoot)
    {
        _repositoryRoot = Path.GetFullPath(repositoryRoot);
        _shared = SharedHashInputs.Read(_repositoryRoot);
    }

    /// <summary>Evaluates every project named in <paramref name="relativePaths"/> for one configuration.</summary>
    public IReadOnlyDictionary<ProjectId, EvaluatedProject> EvaluateAll(
        IReadOnlyList<string> relativePaths, string configuration)
    {
        var results = new Dictionary<ProjectId, EvaluatedProject>();

        foreach (var relativePath in relativePaths)
        {
            var evaluated = Evaluate(relativePath, configuration);
            results[evaluated.Id] = evaluated;
        }

        return results;
    }

    /// <summary>
    /// Re-reads a project's resolved package closure and folds it back into its own-hash inputs,
    /// without re-evaluating it through MSBuild — the package closure comes from
    /// <c>obj/project.assets.json</c> on disk, not from anything the evaluation API resolves, so
    /// there is nothing to re-evaluate. This is why <c>plan</c> can call this after restoring
    /// instead of evaluating every project a second time: a second <c>new Project(...)</c> for
    /// the same path and global properties throws — the collection already has one loaded.
    /// </summary>
    public static EvaluatedProject RefreshPackageClosure(EvaluatedProject project)
    {
        var refreshedInputs = project.OwnHashInputs
            .Where(entry => !entry.StartsWith("package:", StringComparison.Ordinal))
            .Concat(PackageClosureReader.Read(project.FullPath))
            .ToList();

        return project with { OwnHashInputs = refreshedInputs };
    }

    private EvaluatedProject Evaluate(string relativePath, string configuration)
    {
        var fullPath = Path.Combine(_repositoryRoot, relativePath);
        var globalProperties = new Dictionary<string, string> { ["Configuration"] = configuration };
        var project = new Project(fullPath, globalProperties, toolsVersion: null, _collection);

        var id = new ProjectId(relativePath);

        var projectReferences = project.GetItems("ProjectReference")
            .Select(item => Path.GetRelativePath(_repositoryRoot, item.GetMetadataValue("FullPath")).Replace('\\', '/'))
            .Select(path => new ProjectId(path))
            .OrderBy(p => p.Value, StringComparer.Ordinal)
            .ToList();

        var ownHashInputs = new List<string>(_shared.CommonEntries)
        {
            $"property:Configuration={configuration}",
            $"property:TargetFramework={project.GetPropertyValue("TargetFramework")}",
            $"property:LangVersion={project.GetPropertyValue("LangVersion")}",
            $"property:Nullable={project.GetPropertyValue("Nullable")}",
            $"property:TreatWarningsAsErrors={project.GetPropertyValue("TreatWarningsAsErrors")}",
            $"property:Optimize={project.GetPropertyValue("Optimize")}",
            $"property:DefineConstants={project.GetPropertyValue("DefineConstants")}",
            $"property:Deterministic={project.GetPropertyValue("Deterministic")}",
            $"property:ContinuousIntegrationBuild={project.GetPropertyValue("ContinuousIntegrationBuild")}",
            $"property:NETCoreSdkVersion={project.GetPropertyValue("NETCoreSdkVersion")}",
        };

        AddFileContentEntries(project, "Compile", ownHashInputs);
        AddFileContentEntries(project, "Content", ownHashInputs);
        AddFileContentEntries(project, "None", ownHashInputs);
        AddFileContentEntries(project, "EmbeddedResource", ownHashInputs);
        AddFileContentEntries(project, "AdditionalFiles", ownHashInputs);

        foreach (var item in project.GetItems("CloudBuildHashInput"))
            ownHashInputs.Add($"declared:{item.EvaluatedInclude}");

        foreach (var importPath in RepositoryImports(project))
            ownHashInputs.Add($"import:{importPath}={HashFile(Path.Combine(_repositoryRoot, importPath))}");

        foreach (var editorConfigEntry in EditorConfigEntries(Path.GetDirectoryName(fullPath)!))
            ownHashInputs.Add(editorConfigEntry);

        ownHashInputs.AddRange(PackageClosureReader.Read(fullPath));

        return new EvaluatedProject(
            Id: id,
            FullPath: fullPath,
            ProjectReferences: projectReferences,
            IsTestProject: IsTrue(project.GetPropertyValue("IsTestProject")),
            ExcludeFromCoverage: IsTrue(project.GetPropertyValue("ExcludeFromCoverage")),
            MinimumLineCoverage: double.Parse(project.GetPropertyValue("MinimumLineCoverage")),
            MinimumBranchCoverage: double.Parse(project.GetPropertyValue("MinimumBranchCoverage")),
            CacheTestResults: IsTrue(project.GetPropertyValue("CacheTestResults")),
            TargetPath: project.GetPropertyValue("TargetPath"),
            AssemblyName: project.GetPropertyValue("AssemblyName"),
            HasTargetFrameworks: !string.IsNullOrEmpty(project.GetPropertyValue("TargetFrameworks")),
            OwnHashInputs: ownHashInputs);
    }

    private void AddFileContentEntries(Project project, string itemType, List<string> destination)
    {
        foreach (var item in project.GetItems(itemType))
        {
            var fullPath = item.GetMetadataValue("FullPath");
            if (!File.Exists(fullPath))
                continue;

            var relative = Path.GetRelativePath(_repositoryRoot, fullPath).Replace('\\', '/');
            destination.Add($"source:{relative}={HashFile(fullPath)}");
        }
    }

    private IEnumerable<string> RepositoryImports(Project project) =>
        project.Imports
            .Select(import => import.ImportedProject.FullPath)
            .Where(path => path.StartsWith(_repositoryRoot, StringComparison.OrdinalIgnoreCase))
            .Select(path => Path.GetRelativePath(_repositoryRoot, path).Replace('\\', '/'))
            .Where(relative => !IsCoveredByPackageClosure(relative))
            .Distinct(StringComparer.Ordinal)
            .OrderBy(path => path, StringComparer.Ordinal);

    private static bool IsCoveredByPackageClosure(string relativePath) =>
        Path.GetFileName(relativePath).Equals("Directory.Packages.props", StringComparison.OrdinalIgnoreCase)
        || relativePath.Split('/').Contains("obj", StringComparer.OrdinalIgnoreCase);

    private IEnumerable<string> EditorConfigEntries(string startDirectory)
    {
        var directory = new DirectoryInfo(startDirectory);
        var root = new DirectoryInfo(_repositoryRoot);

        while (true)
        {
            var candidate = Path.Combine(directory.FullName, ".editorconfig");
            if (File.Exists(candidate))
            {
                var relative = Path.GetRelativePath(_repositoryRoot, candidate).Replace('\\', '/');
                yield return $"editorconfig:{relative}={HashFile(candidate)}";
            }

            if (string.Equals(directory.FullName, root.FullName, StringComparison.OrdinalIgnoreCase) || directory.Parent is null)
                yield break;

            directory = directory.Parent;
        }
    }

    private static bool IsTrue(string value) => string.Equals(value, "true", StringComparison.OrdinalIgnoreCase);

    private static string HashFile(string path) =>
        Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(path)));

    public void Dispose() => _collection.Dispose();
}
