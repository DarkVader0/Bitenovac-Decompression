using System.Security.Cryptography;

namespace Bitenovac.CloudBuild.Hashing;

/// <summary>
/// Hashes the assemblies of the tool that is running, so every target's <c>fullHash</c> changes
/// when the installed tool does — not when a pull request edits the tool's source.
/// </summary>
internal static class ToolVersion
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
