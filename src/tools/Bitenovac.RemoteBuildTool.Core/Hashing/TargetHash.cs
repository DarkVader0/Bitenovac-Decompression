namespace Bitenovac.RemoteBuildTool.Core.Hashing;

/// <summary>
/// Represents the two hashes that caching decisions are made from, for one project in one configuration.
/// </summary>
/// <param name="OwnHash">
/// The hash of everything that affects the project's own build: its sources, resolved dependencies,
/// build settings and toolchain. It does not depend on any other project.
/// </param>
/// <param name="FullHash">
/// A Merkle hash of <paramref name="OwnHash"/> and the <see cref="FullHash"/> of every project this
/// one references. Two projects share a <see cref="FullHash"/> only when their own inputs and their
/// whole dependency closure match.
/// </param>
public readonly record struct TargetHash(string OwnHash, string FullHash);
