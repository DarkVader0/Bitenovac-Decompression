namespace Bitenovac.CloudBuild.Core.Planning;

/// <summary>The hash <c>main</c> last recorded for a target, in one configuration.</summary>
/// <param name="OwnHash">The stored own hash, used to decide whether to re-gate coverage.</param>
/// <param name="FullHash">The stored full hash, used to decide whether to rebuild and retest.</param>
public readonly record struct StoredTargetHash(string OwnHash, string FullHash);
