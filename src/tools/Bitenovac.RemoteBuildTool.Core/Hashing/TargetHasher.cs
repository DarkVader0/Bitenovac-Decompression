using System.Security.Cryptography;
using System.Text;

namespace Bitenovac.RemoteBuildTool.Core.Hashing;

/// <summary>
/// Provides methods for computing the hashes in a <see cref="TargetHash"/>.
/// </summary>
public static class TargetHasher
{
    /// <summary>
    /// Computes a project's own hash from its inputs.
    /// </summary>
    /// <param name="inputs">
    /// One stable string per input, for example <c>"source:path=&lt;content hash&gt;"</c> for a
    /// source file or <c>"package:Id/Version"</c> for a resolved package.
    /// </param>
    /// <returns>The lowercase hexadecimal SHA-256 hash of the inputs.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="inputs"/> is <see langword="null"/>.</exception>
    /// <remarks>
    /// The inputs are sorted before hashing, so the result depends on the set of inputs and not on
    /// the order they are supplied in.
    /// </remarks>
    public static string ComputeOwnHash(IEnumerable<string> inputs)
    {
        ArgumentNullException.ThrowIfNull(inputs);
        return HashSortedEntries(inputs);
    }

    /// <summary>
    /// Computes a project's full hash from its own hash and the full hashes of the projects it references.
    /// </summary>
    /// <param name="ownHash">The project's own hash, as returned by <see cref="ComputeOwnHash"/>.</param>
    /// <param name="dependencyFullHashes">
    /// The <see cref="TargetHash.FullHash"/> of every project this one directly references, in any order.
    /// </param>
    /// <returns>
    /// The lowercase hexadecimal SHA-256 hash of <paramref name="ownHash"/> and
    /// <paramref name="dependencyFullHashes"/>.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="ownHash"/> or <paramref name="dependencyFullHashes"/> is <see langword="null"/>.
    /// </exception>
    public static string ComputeFullHash(string ownHash, IEnumerable<string> dependencyFullHashes)
    {
        ArgumentNullException.ThrowIfNull(ownHash);
        ArgumentNullException.ThrowIfNull(dependencyFullHashes);
        return HashSortedEntries([ownHash, .. dependencyFullHashes]);
    }

    private static string HashSortedEntries(IEnumerable<string> entries)
    {
        var joined = string.Join('\n', entries.OrderBy(entry => entry, StringComparer.Ordinal));
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(joined));
        return Convert.ToHexStringLower(bytes);
    }
}
