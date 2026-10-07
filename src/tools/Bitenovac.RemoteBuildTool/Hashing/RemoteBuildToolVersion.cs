using System.Security.Cryptography;

namespace Bitenovac.RemoteBuildTool.Hashing;

/// <summary>
/// Provides methods for hashing the assemblies of the running RemoteBuildTool.
/// </summary>
/// <remarks>
/// The hash is an input to every target's full hash, so every target changes when the installed
/// tool changes, not when a pull request edits the tool's source.
/// </remarks>
internal static class RemoteBuildToolVersion
{
    public static string Compute(string toolDirectory)
    {
        var entries = Directory.EnumerateFiles(toolDirectory, "*.dll")
            .Order(StringComparer.Ordinal)
            .Select(path => $"{Path.GetFileName(path)}={Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(path)))}");

        var joined = string.Join('\n', entries);
        return Convert.ToHexStringLower(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(joined)));
    }
}
