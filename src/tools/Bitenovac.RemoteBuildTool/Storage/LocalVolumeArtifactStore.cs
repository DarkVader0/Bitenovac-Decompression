using System.Security.Cryptography;
using System.Text.Json;
using Bitenovac.RemoteBuildTool.Core.Graph;
using Bitenovac.RemoteBuildTool.Core.Planning;
using Bitenovac.RemoteBuildTool.Core.Storage;

namespace Bitenovac.RemoteBuildTool.Storage;

/// <summary>
/// Represents an <see cref="IArtifactStore"/> backed by a directory on disk.
/// </summary>
/// <remarks>
/// <para>
/// One instance serves either <c>main</c> or a single run's own volume, depending on its root.
/// </para>
/// <para>
/// The store is content-addressed at the file level, so each distinct file is stored once and
/// every entry that holds it records a reference:
/// </para>
/// <code>
/// &lt;root&gt;/blobs/&lt;ab&gt;/&lt;sha256 of content&gt;          one copy of each distinct file
/// &lt;root&gt;/&lt;project path&gt;/&lt;Configuration&gt;/
///     entries/&lt;fullHash&gt;/manifest.json          relative path -> blob, plus both hashes
///     current                                     names the live entry
/// </code>
/// <para>
/// Publishing is atomic. A blob is written to a temporary name and renamed into place, so a reader
/// sees either a complete blob or none. The manifest is written inside the entry directory, which
/// is renamed as a whole, and only then is <c>current</c> replaced by a single file rename. A
/// reader that resolves <c>current</c> before the rename sees the complete previous entry, never a
/// partially written one.
/// </para>
/// </remarks>
public sealed class LocalVolumeArtifactStore : IArtifactStore
{
    private const string CurrentFileName = "current";
    private const string ManifestFileName = "manifest.json";
    private const string BlobsDirectoryName = "blobs";

    private readonly string _root;

    /// <summary>
    /// Initializes a new instance of the <see cref="LocalVolumeArtifactStore"/> class rooted at the
    /// specified directory, creating the directory if it does not exist.
    /// </summary>
    /// <param name="root">The directory this store reads and writes.</param>
    /// <exception cref="ArgumentException">
    /// <paramref name="root"/> is <see langword="null"/>, empty or white space.
    /// </exception>
    public LocalVolumeArtifactStore(string root)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(root);
        _root = Path.GetFullPath(root);
        Directory.CreateDirectory(_root);
    }

    /// <inheritdoc/>
    public bool Contains(ProjectId project, string configuration) =>
        File.Exists(CurrentFile(project, configuration));

    /// <inheritdoc/>
    public bool TryGetHash(ProjectId project, string configuration, out StoredTargetHash hash) =>
        TryReadManifest(project, configuration, out _, out hash);

    /// <inheritdoc/>
    public bool TryGet(ProjectId project, string configuration, string destinationDirectory, out StoredTargetHash hash) =>
        TryGet(project, configuration, destinationDirectory, out hash, includePrefixes: null);

    /// <summary>
    /// Copies the files of the entry for the specified project and configuration whose paths start
    /// with one of the specified prefixes into a directory.
    /// </summary>
    /// <param name="project">The project to materialize.</param>
    /// <param name="configuration">The build configuration.</param>
    /// <param name="destinationDirectory">The directory to copy the files into; it is created if it does not exist.</param>
    /// <param name="hash">
    /// When this method returns, contains the hashes the entry was stored under, if it was found
    /// and copied; otherwise, the default value.
    /// </param>
    /// <param name="includePrefixes">
    /// The path prefixes of the files to copy, or <see langword="null"/> to copy every file.
    /// </param>
    /// <returns>
    /// <see langword="true"/> if the entry was found and copied; otherwise, <see langword="false"/>.
    /// </returns>
    /// <remarks>
    /// An entry holds <c>tests/</c> as well as <c>bin/</c> and <c>obj/</c>, so a caller can pass
    /// prefixes to write only the build output directly into a project directory.
    /// </remarks>
    public bool TryGet(
        ProjectId project,
        string configuration,
        string destinationDirectory,
        out StoredTargetHash hash,
        IReadOnlyCollection<string>? includePrefixes)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(destinationDirectory);

        if (!TryReadManifest(project, configuration, out var manifest, out hash))
            return false;

        Directory.CreateDirectory(destinationDirectory);

        foreach (var file in manifest.Files)
        {
            if (includePrefixes is not null
                && !includePrefixes.Any(prefix => file.Path.StartsWith(prefix, StringComparison.Ordinal)))
            {
                continue;
            }

            var blob = BlobPath(file.Blob);
            if (!File.Exists(blob))
            {
                hash = default;
                return false;
            }

            var destination = Path.Combine(destinationDirectory, file.Path.Replace('/', Path.DirectorySeparatorChar));
            Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
            File.Copy(blob, destination, overwrite: true);
        }

        return true;
    }

    /// <inheritdoc/>
    /// <remarks>
    /// The new entry replaces everything an existing entry under the same full hash holds; the
    /// store does not merge. Use <see cref="AddFiles"/> to add files to an existing entry.
    /// </remarks>
    public void Put(ProjectId project, string configuration, string sourceDirectory, StoredTargetHash hash)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceDirectory);
        Put(project, configuration, hash, EnumerateAsSources(sourceDirectory, prefix: ""));
    }

    /// <summary>
    /// Stores an entry for the specified project and configuration from individually named files,
    /// replacing any existing entry.
    /// </summary>
    /// <param name="project">The project the artifact belongs to.</param>
    /// <param name="configuration">The build configuration.</param>
    /// <param name="hash">The hashes the artifact was produced from.</param>
    /// <param name="files">The entry's files, as pairs of a path within the entry and a file on disk.</param>
    /// <exception cref="ArgumentNullException"><paramref name="files"/> is <see langword="null"/>.</exception>
    /// <remarks>
    /// Files are hashed where they are, and only content the store does not already hold is written.
    /// </remarks>
    public void Put(
        ProjectId project,
        string configuration,
        StoredTargetHash hash,
        IEnumerable<(string Path, string SourceFile)> files)
    {
        ArgumentNullException.ThrowIfNull(files);

        var manifest = files
            .Select(file => new ManifestFile(file.Path, WriteBlob(file.SourceFile)))
            .OrderBy(file => file.Path, StringComparer.Ordinal)
            .ToList();

        WriteEntry(project, configuration, hash, manifest);
    }

    /// <summary>
    /// Enumerates the files in a directory as pairs of a path within an entry and a file on disk.
    /// </summary>
    /// <param name="directory">The directory to enumerate.</param>
    /// <param name="prefix">The path prefix inside the entry, for example <c>"bin/"</c>.</param>
    /// <returns>
    /// One pair per file under <paramref name="directory"/>, or an empty sequence if the directory
    /// does not exist.
    /// </returns>
    public static IEnumerable<(string Path, string SourceFile)> EnumerateAsSources(string directory, string prefix)
    {
        if (!Directory.Exists(directory))
            yield break;

        foreach (var file in Directory.EnumerateFiles(directory, "*", SearchOption.AllDirectories))
            yield return (prefix + Path.GetRelativePath(directory, file).Replace('\\', '/'), file);
    }

    private void WriteEntry(ProjectId project, string configuration, StoredTargetHash hash, List<ManifestFile> files)
    {
        var targetDirectory = TargetDirectory(project, configuration);
        Directory.CreateDirectory(Path.Combine(targetDirectory, "entries"));

        var staging = Path.Combine(targetDirectory, $".staging-{Guid.NewGuid():N}");
        Directory.CreateDirectory(staging);
        File.WriteAllText(
            Path.Combine(staging, ManifestFileName),
            JsonSerializer.Serialize(new Manifest(hash.OwnHash, hash.FullHash, files)));

        var entryDirectory = Path.Combine(targetDirectory, "entries", hash.FullHash);
        if (Directory.Exists(entryDirectory))
            Directory.Delete(entryDirectory, recursive: true);
        Directory.Move(staging, entryDirectory);

        PublishPointer(targetDirectory, hash.FullHash);
    }

    /// <summary>
    /// Adds files to the entry stored for the specified hashes, keeping every file it already holds.
    /// </summary>
    /// <param name="project">The project whose entry is extended.</param>
    /// <param name="configuration">The build configuration.</param>
    /// <param name="hash">The hashes of the entry to extend; the entry must be the live one.</param>
    /// <param name="sourceDirectory">
    /// The directory whose layout is merged into the entry, for example one holding a <c>tests/</c>
    /// directory.
    /// </param>
    /// <returns>
    /// <see langword="true"/> if the live entry is stored under the full hash of
    /// <paramref name="hash"/> and was extended; otherwise, <see langword="false"/>.
    /// </returns>
    /// <remarks>
    /// A file whose path the entry already holds is replaced. Only the added files are hashed.
    /// </remarks>
    public bool AddFiles(ProjectId project, string configuration, StoredTargetHash hash, string sourceDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceDirectory);

        if (!TryReadManifest(project, configuration, out var manifest, out var stored) || stored.FullHash != hash.FullHash)
            return false;

        var files = manifest.Files.ToDictionary(file => file.Path, StringComparer.Ordinal);

        foreach (var file in Directory.EnumerateFiles(sourceDirectory, "*", SearchOption.AllDirectories))
        {
            var relative = Path.GetRelativePath(sourceDirectory, file).Replace('\\', '/');
            files[relative] = new ManifestFile(relative, WriteBlob(file));
        }

        var merged = files.Values.OrderBy(file => file.Path, StringComparer.Ordinal).ToList();
        WriteEntry(project, configuration, hash, merged);
        return true;
    }

    /// <inheritdoc/>
    public void Promote(ProjectId project, string configuration, IArtifactStore source)
    {
        ArgumentNullException.ThrowIfNull(source);

        var staging = Path.Combine(_root, $".promote-{Guid.NewGuid():N}");
        Directory.CreateDirectory(staging);
        try
        {
            if (!source.TryGet(project, configuration, staging, out var hash))
                return;

            Put(project, configuration, staging, hash);
        }
        finally
        {
            if (Directory.Exists(staging))
                Directory.Delete(staging, recursive: true);
        }
    }

    /// <summary>
    /// Deletes every blob that no stored manifest references.
    /// </summary>
    /// <returns>The number of blobs deleted.</returns>
    public int Prune()
    {
        var blobsRoot = Path.Combine(_root, BlobsDirectoryName);
        if (!Directory.Exists(blobsRoot))
            return 0;

        var referenced = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var manifestPath in Directory.EnumerateFiles(_root, ManifestFileName, SearchOption.AllDirectories))
        {
            var manifest = ReadManifestFile(manifestPath);
            if (manifest is null)
                continue;

            foreach (var file in manifest.Files)
                referenced.Add(file.Blob);
        }

        var removed = 0;
        foreach (var blob in Directory.EnumerateFiles(blobsRoot, "*", SearchOption.AllDirectories))
        {
            if (referenced.Contains(Path.GetFileName(blob)))
                continue;

            File.Delete(blob);
            removed++;
        }

        return removed;
    }

    private const long InMemoryHashLimit = 8 * 1024 * 1024;

    private string WriteBlob(string sourceFile)
    {
        var length = new FileInfo(sourceFile).Length;

        if (length > InMemoryHashLimit)
        {
            string streamedDigest;
            using (var stream = File.OpenRead(sourceFile))
                streamedDigest = Convert.ToHexStringLower(SHA256.HashData(stream));

            return StoreBlob(streamedDigest, blobPath => File.Copy(sourceFile, blobPath, overwrite: true));
        }

        var content = File.ReadAllBytes(sourceFile);
        var digest = Convert.ToHexStringLower(SHA256.HashData(content));
        return StoreBlob(digest, blobPath => File.WriteAllBytes(blobPath, content));
    }

    private string StoreBlob(string digest, Action<string> write)
    {
        var blobPath = BlobPath(digest);

        if (File.Exists(blobPath))
            return digest;

        Directory.CreateDirectory(Path.GetDirectoryName(blobPath)!);
        var temporary = blobPath + $".tmp-{Guid.NewGuid():N}";
        write(temporary);

        try
        {
            File.Move(temporary, blobPath, overwrite: false);
        }
        catch (IOException) when (File.Exists(blobPath))
        {
            File.Delete(temporary);
        }

        return digest;
    }

    private string BlobPath(string digest) =>
        Path.Combine(_root, BlobsDirectoryName, digest[..2], digest);

    private string TargetDirectory(ProjectId project, string configuration) =>
        Path.Combine(_root, project.Value, configuration);

    private string CurrentFile(ProjectId project, string configuration) =>
        Path.Combine(TargetDirectory(project, configuration), CurrentFileName);

    private bool TryReadManifest(ProjectId project, string configuration, out Manifest manifest, out StoredTargetHash hash)
    {
        manifest = null!;
        hash = default;

        var currentFile = CurrentFile(project, configuration);
        if (!File.Exists(currentFile))
            return false;

        var fullHash = File.ReadAllText(currentFile).Trim();
        var manifestPath = Path.Combine(TargetDirectory(project, configuration), "entries", fullHash, ManifestFileName);

        var read = ReadManifestFile(manifestPath);
        if (read is null)
            return false;

        manifest = read;
        hash = new StoredTargetHash(read.OwnHash, read.FullHash);
        return true;
    }

    private static Manifest? ReadManifestFile(string path)
    {
        if (!File.Exists(path))
            return null;

        try
        {
            return JsonSerializer.Deserialize<Manifest>(File.ReadAllText(path));
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static void PublishPointer(string targetDirectory, string fullHash)
    {
        var pointerFile = Path.Combine(targetDirectory, CurrentFileName);
        var temporaryFile = Path.Combine(targetDirectory, $".current-{Guid.NewGuid():N}");
        File.WriteAllText(temporaryFile, fullHash);
        File.Move(temporaryFile, pointerFile, overwrite: true);
    }

    private sealed record Manifest(string OwnHash, string FullHash, List<ManifestFile> Files);

    private sealed record ManifestFile(string Path, string Blob);
}
