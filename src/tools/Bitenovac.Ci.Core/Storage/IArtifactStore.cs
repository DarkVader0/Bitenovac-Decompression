using Bitenovac.Ci.Core.Graph;
using Bitenovac.Ci.Core.Planning;

namespace Bitenovac.Ci.Core.Storage;

/// <summary>
/// A store of built and tested artifacts, keyed by project and configuration. One implementation
/// backs <c>main</c> (permanent, one entry per target, read-only from a PR); another backs a
/// single run's own volume (ephemeral, read-write, deleted when the run ends). Both are reached
/// through this port so the pipeline commands do not know which is which.
/// </summary>
public interface IArtifactStore
{
    /// <summary>True when this store holds an entry for the target.</summary>
    /// <param name="project">The project to check.</param>
    /// <param name="configuration">The build configuration.</param>
    bool Contains(ProjectId project, string configuration);

    /// <summary>
    /// Reports the hash a target is stored under, without touching its files — what deciding
    /// hit or miss needs, and nothing more.
    /// </summary>
    /// <param name="project">The project to check.</param>
    /// <param name="configuration">The build configuration.</param>
    /// <param name="hash">The stored hash, when the method returns true.</param>
    /// <returns>False when nothing is stored for this target.</returns>
    bool TryGetHash(ProjectId project, string configuration, out StoredTargetHash hash);

    /// <summary>
    /// Copies a stored entry's files into <paramref name="destinationDirectory"/> and reports
    /// the hash it was stored under.
    /// </summary>
    /// <param name="project">The project to materialise.</param>
    /// <param name="configuration">The build configuration.</param>
    /// <param name="destinationDirectory">
    /// An existing, empty directory to copy the entry's files into.
    /// </param>
    /// <param name="hash">The hash the entry was stored under, when the method returns true.</param>
    /// <returns>False when nothing is stored for this target.</returns>
    bool TryGet(ProjectId project, string configuration, string destinationDirectory, out StoredTargetHash hash);

    /// <summary>
    /// Stores a freshly built or tested target, replacing whatever this store held for it.
    /// </summary>
    /// <param name="project">The project the artifact belongs to.</param>
    /// <param name="configuration">The build configuration.</param>
    /// <param name="sourceDirectory">The directory holding the artifact's files.</param>
    /// <param name="hash">The hash this artifact was produced from.</param>
    void Put(ProjectId project, string configuration, string sourceDirectory, StoredTargetHash hash);

    /// <summary>
    /// Copies one target's entry from <paramref name="source"/> into this store, as when a
    /// merge-queue run promotes a run's artifacts into <c>main</c>. A no-op when
    /// <paramref name="source"/> holds nothing for this target.
    /// </summary>
    /// <param name="project">The project to promote.</param>
    /// <param name="configuration">The build configuration.</param>
    /// <param name="source">The store to promote the entry from.</param>
    void Promote(ProjectId project, string configuration, IArtifactStore source);
}
