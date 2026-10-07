namespace Bitenovac.RemoteBuildTool.Core.Planning;

/// <summary>
/// Specifies whether a project's computed hash matches the hash stored for it.
/// </summary>
public enum CacheOutcome
{
    /// <summary>
    /// The computed hash matches the stored hash, so the stored artifact can be reused.
    /// </summary>
    Hit,
    /// <summary>
    /// The computed hash differs from the stored hash, or no hash is stored.
    /// </summary>
    Miss,
}