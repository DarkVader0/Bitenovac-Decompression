namespace Bitenovac.Ci.Core.Hashing;

/// <summary>The two hashes that drive every caching decision for one project, in one configuration.</summary>
/// <param name="OwnHash">
/// Hashes everything that affects this project's own compilation: its sources, its resolved
/// package closure, its build properties and the shared build files above it. Independent of
/// any other project's content.
/// </param>
/// <param name="FullHash">
/// A Merkle hash folding <paramref name="OwnHash"/> together with the <c>FullHash</c> of every
/// project this one references, directly or transitively. Two projects share a
/// <see cref="FullHash"/> only when their own inputs and their whole dependency closure match.
/// </param>
public readonly record struct TargetHash(string OwnHash, string FullHash);
