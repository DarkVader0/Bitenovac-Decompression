using System.Security.Cryptography;

namespace Bitenovac.CloudBuild.Hashing;

/// <summary>
/// The own-hash inputs every project shares: <c>global.json</c> and <c>nuget.config</c> govern
/// SDK resolution and restore for the whole repository, so their content is read once and
/// reused rather than re-read per project.
/// </summary>
internal sealed class SharedHashInputs
{
    public IReadOnlyList<string> CommonEntries { get; }

    private SharedHashInputs(IReadOnlyList<string> commonEntries) => CommonEntries = commonEntries;

    public static SharedHashInputs Read(string repositoryRoot)
    {
        var entries = new List<string>();

        AddIfExists(entries, repositoryRoot, "global.json");
        AddIfExists(entries, repositoryRoot, "nuget.config");
        AddIfExists(entries, repositoryRoot, "NuGet.config");
        AddIfExists(entries, repositoryRoot, "NuGet.Config");

        return new SharedHashInputs(entries);
    }

    private static void AddIfExists(List<string> entries, string repositoryRoot, string fileName)
    {
        var path = Path.Combine(repositoryRoot, fileName);
        if (!File.Exists(path))
            return;

        var hash = Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(path)));
        entries.Add($"repo-file:{fileName}={hash}");
    }
}
