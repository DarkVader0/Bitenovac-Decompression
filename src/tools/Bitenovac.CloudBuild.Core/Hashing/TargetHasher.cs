using System.Security.Cryptography;
using System.Text;

namespace Bitenovac.CloudBuild.Core.Hashing;

/// <summary>
/// Combines already-gathered, already-normalised inputs into the two hashes a caching decision
/// needs. Reads nothing itself: collecting file contents, resolved package versions and build
/// properties is the caller's job, so this type has no filesystem or process dependency and is
/// exercised with plain strings.
/// </summary>
public static class TargetHasher
{
    /// <summary>
    /// Hashes a project's own inputs: everything that affects its compilation independent of
    /// any project it references. Entries are sorted before hashing, so the result depends on
    /// the set of inputs, never the order the caller gathered them in.
    /// </summary>
    /// <param name="inputs">
    /// One entry per input, each already rendered to a stable string, for example
    /// <c>"path=&lt;content hash&gt;"</c> for a source file or <c>"package:Id/Version"</c> for a
    /// resolved package.
    /// </param>
    /// <exception cref="ArgumentNullException"><paramref name="inputs"/> is null.</exception>
    public static string ComputeOwnHash(IEnumerable<string> inputs)
    {
        ArgumentNullException.ThrowIfNull(inputs);
        return HashSortedEntries(inputs);
    }

    /// <summary>
    /// Folds a project's own hash together with the full hash of every project it directly
    /// depends on, producing a Merkle hash over its whole dependency closure.
    /// </summary>
    /// <param name="ownHash">The project's own hash, from <see cref="ComputeOwnHash"/>.</param>
    /// <param name="dependencyFullHashes">
    /// The <see cref="TargetHash.FullHash"/> of every project this one directly references.
    /// Order does not matter.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="ownHash"/> or <paramref name="dependencyFullHashes"/> is null.
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
