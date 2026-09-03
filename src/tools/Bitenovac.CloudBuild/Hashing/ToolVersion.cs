using System.Security.Cryptography;

namespace Bitenovac.CloudBuild.Hashing;

/// <summary>
/// Hashes the CloudBuild tool's own source, so a change to it invalidates every target's
/// <c>fullHash</c> — a tool change alters how everything is compiled, tested and measured, the
/// same reasoning the old pipeline applied to <c>build/</c> and <c>.github/workflows/</c>.
/// </summary>
internal static class ToolVersion
{
    public static string Compute(string repositoryRoot)
    {
        var toolsRoot = Path.Combine(repositoryRoot, "src", "tools");
        if (!Directory.Exists(toolsRoot))
            return "no-tool-sources";

        var files = Directory.EnumerateFiles(toolsRoot, "*", SearchOption.AllDirectories)
            .Where(path => path.EndsWith(".cs", StringComparison.OrdinalIgnoreCase)
                || path.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase))
            .Where(path => !path.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                .Any(segment => segment is "bin" or "obj"))
            .OrderBy(path => path, StringComparer.Ordinal);

        var entries = files.Select(path =>
        {
            var relative = Path.GetRelativePath(repositoryRoot, path).Replace('\\', '/');
            var hash = Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(path)));
            return $"{relative}={hash}";
        });

        var joined = string.Join('\n', entries);
        return Convert.ToHexStringLower(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(joined)));
    }
}
