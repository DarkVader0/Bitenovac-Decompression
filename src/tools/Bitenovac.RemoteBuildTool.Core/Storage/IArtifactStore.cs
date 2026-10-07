using Bitenovac.RemoteBuildTool.Core.Graph;
using Bitenovac.RemoteBuildTool.Core.Planning;

namespace Bitenovac.RemoteBuildTool.Core.Storage;

/// <summary>
/// Defines a store of built and tested artifacts, keyed by project and configuration.
/// </summary>
public interface IArtifactStore
{
    /// <summary>
    /// Determines whether the store holds an entry for the specified project and configuration.
    /// </summary>
    /// <param name="project">The project to look up.</param>
    /// <param name="configuration">The build configuration.</param>
    /// <returns><see langword="true"/> if the store holds an entry; otherwise, <see langword="false"/>.</returns>
    bool Contains(ProjectId project, string configuration);

    /// <summary>
    /// Gets the hashes the specified project and configuration are stored under, without reading the entry's files.
    /// </summary>
    /// <param name="project">The project to look up.</param>
    /// <param name="configuration">The build configuration.</param>
    /// <param name="hash">
    /// When this method returns, contains the stored hashes, if an entry was found; otherwise, the
    /// default value.
    /// </param>
    /// <returns><see langword="true"/> if the store holds an entry; otherwise, <see langword="false"/>.</returns>
    bool TryGetHash(ProjectId project, string configuration, out StoredTargetHash hash);

    /// <summary>
    /// Copies the files of the entry for the specified project and configuration into a directory.
    /// </summary>
    /// <param name="project">The project to materialize.</param>
    /// <param name="configuration">The build configuration.</param>
    /// <param name="destinationDirectory">The directory to copy the entry's files into.</param>
    /// <param name="hash">
    /// When this method returns, contains the hashes the entry was stored under, if it was found;
    /// otherwise, the default value.
    /// </param>
    /// <returns><see langword="true"/> if the entry was found and copied; otherwise, <see langword="false"/>.</returns>
    bool TryGet(ProjectId project, string configuration, string destinationDirectory, out StoredTargetHash hash);

    /// <summary>
    /// Stores the artifact for the specified project and configuration, replacing any existing entry.
    /// </summary>
    /// <param name="project">The project the artifact belongs to.</param>
    /// <param name="configuration">The build configuration.</param>
    /// <param name="sourceDirectory">The directory that holds the artifact's files.</param>
    /// <param name="hash">The hashes the artifact was produced from.</param>
    void Put(ProjectId project, string configuration, string sourceDirectory, StoredTargetHash hash);

    /// <summary>
    /// Copies the entry for the specified project and configuration from another store into this one.
    /// </summary>
    /// <param name="project">The project to promote.</param>
    /// <param name="configuration">The build configuration.</param>
    /// <param name="source">The store to copy the entry from.</param>
    /// <remarks>
    /// This method does nothing if <paramref name="source"/> holds no entry for the project.
    /// </remarks>
    void Promote(ProjectId project, string configuration, IArtifactStore source);
}
