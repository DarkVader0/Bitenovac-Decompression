namespace Bitenovac.RemoteBuildTool.Core.Planning;

/// <summary>
/// Represents the hashes an artifact store recorded for a project in one configuration.
/// </summary>
/// <param name="OwnHash">The stored own hash, which decides whether the project's coverage is gated again.</param>
/// <param name="FullHash">The stored full hash, which decides whether the project is rebuilt and retested.</param>
public readonly record struct StoredTargetHash(string OwnHash, string FullHash);
