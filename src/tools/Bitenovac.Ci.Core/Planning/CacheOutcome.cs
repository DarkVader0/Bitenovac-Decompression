namespace Bitenovac.Ci.Core.Planning;

/// <summary>Whether a target's current hash matches what the artifact store holds.</summary>
public enum CacheOutcome
{
    /// <summary>The computed hash matches the store; the cached artifact can be reused.</summary>
    Hit,

    /// <summary>The computed hash does not match the store, or nothing is stored yet.</summary>
    Miss,
}
