using System.Security.Cryptography;
using System.Text.Json;
using Bitenovac.CloudBuild.Core.Graph;
using Bitenovac.CloudBuild.Core.Planning;
using Bitenovac.CloudBuild.Core.Storage;

namespace Bitenovac.CloudBuild.Storage;

/// <summary>
/// An <see cref="IArtifactStore"/> backed by a directory on disk — a Docker volume in CI, any
/// directory locally. One instance serves either <c>main</c> or a single run's own volume; the
/// caller decides which by pointing two instances at two different roots.
/// </summary>
/// <remarks>
/// <para>
/// Content-addressed at the <em>file</em> level, not the entry level:
/// </para>
/// <code>
/// &lt;root&gt;/blobs/&lt;ab&gt;/&lt;sha256 of content&gt;          one copy of each distinct file, ever
/// &lt;root&gt;/&lt;project path&gt;/&lt;Configuration&gt;/
///     entries/&lt;fullHash&gt;/manifest.json          relative path -> blob, plus both hashes
///     current                                     names the live entry
/// </code>
/// <para>
/// Every test project's <c>bin</c> contains its whole dependency closure, so the same
/// <c>Core.dll</c>, <c>xunit.dll</c> and so on are otherwise stored once per entry: measured on
/// this repository, 73% of the store was duplicate content — 553 MB of 753 MB. Keyed by content
/// hash instead, each distinct file is written once and every entry that needs it records a
/// reference.
/// </para>
/// <para>
/// Publishing stays atomic. A blob is written to a temporary name and renamed into place, so a
/// reader either sees a complete blob or none. The manifest is written inside the entry
/// directory, which is renamed in whole, and only then does <c>current</c> flip — itself a
/// single atomic file rename. A reader that resolves <c>current</c> before the flip sees the
/// previous entry, complete; never a half-written one.
/// </para>
/// </remarks>
public sealed class LocalVolumeArtifactStore : IArtifactStore
{
    private const string CurrentFileName = "current";
    private const string ManifestFileName = "manifest.json";
    private const string BlobsDirectoryName = "blobs";

    private readonly string _root;

    /// <summary>Creates a store rooted at <paramref name="root"/>, creating the directory if it does not exist.</summary>
    /// <param name="root">The directory this store reads and writes.</param>
    /// <exception cref="ArgumentException"><paramref name="root"/> is null, empty, or white space.</exception>
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
    /// As <see cref="TryGet(ProjectId, string, string, out StoredTargetHash)"/>, but writes only
    /// the entry's files whose path starts with one of <paramref name="includePrefixes"/>.
    /// </summary>
    /// <remarks>
    /// Lets a caller write straight into the project directory instead of unpacking the whole
    /// entry to a temporary directory and copying the wanted parts out — which was a second full
    /// copy of everything, on the hot path. An entry holds <c>tests/</c> as well as <c>bin/</c>
    /// and <c>obj/</c>, and only the latter two belong in a source tree.
    /// </remarks>
    /// <param name="project">The project to materialise.</param>
    /// <param name="configuration">The build configuration.</param>
    /// <param name="destinationDirectory">An existing or creatable directory to write into.</param>
    /// <param name="hash">The hash the entry was stored under, when the method returns true.</param>
    /// <param name="includePrefixes">Path prefixes to include, or null for everything.</param>
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
                // A blob the manifest names has gone missing — a store pruned too aggressively,
                // or one interrupted mid-write. Serving a partial entry would be worse than
                // serving none, so this reads as a miss and the caller rebuilds.
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
    /// Always replaces whatever this fullHash's entry currently holds — the store does not
    /// merge. A build stage puts <c>bin/</c> and <c>obj/</c>; a later test stage for the same
    /// project and fullHash wants to add <c>tests/</c> alongside them without losing what build
    /// staged, so it is the caller's job to <see cref="TryGet"/> the existing entry, add its own
    /// files into the same directory, and <see cref="Put"/> the merged result back.
    /// </remarks>
    public void Put(ProjectId project, string configuration, string sourceDirectory, StoredTargetHash hash)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceDirectory);
        Put(project, configuration, hash, EnumerateAsSources(sourceDirectory, prefix: ""));
    }

    /// <summary>
    /// Stores an entry from files named individually, so a caller can store directly from the
    /// workspace instead of copying everything into a staging directory first.
    /// </summary>
    /// <remarks>
    /// The staging copy this replaces was a full copy of <c>bin</c> and <c>obj</c> per project,
    /// paid before a single byte was hashed — on a cold run, the largest single cost in the
    /// pipeline. Files are hashed where they lie; only genuinely new content is written.
    /// </remarks>
    /// <param name="project">The project the artifact belongs to.</param>
    /// <param name="configuration">The build configuration.</param>
    /// <param name="hash">The hash this artifact was produced from.</param>
    /// <param name="files">The entry's files, as (path within the entry, file on disk) pairs.</param>
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

    /// <summary>Enumerates a directory as entry-relative (path, file) pairs under <paramref name="prefix"/>.</summary>
    /// <param name="directory">The directory to enumerate; missing directories yield nothing.</param>
    /// <param name="prefix">The path prefix inside the entry, for example <c>"bin/"</c>.</param>
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
    /// Adds files to the entry already stored for <paramref name="hash"/>, leaving everything it
    /// holds in place.
    /// </summary>
    /// <remarks>
    /// The alternative — re-<see cref="Put"/>ting the whole merged directory — costs a full
    /// content hash of every file in <c>bin</c> and <c>obj</c> just to attach a handful of
    /// coverage reports. Measured on this repository, that was most of the test stage's time.
    /// Here only the new files are hashed and the existing manifest is extended.
    /// </remarks>
    /// <param name="project">The project whose entry is being extended.</param>
    /// <param name="configuration">The build configuration.</param>
    /// <param name="hash">The hash whose entry to extend; must be the live one.</param>
    /// <param name="sourceDirectory">
    /// A directory whose layout is merged into the entry — a <c>tests/</c> folder, for example.
    /// </param>
    /// <returns>False when no entry is stored for this hash, so there was nothing to extend.</returns>
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
    /// Deletes blobs no live entry references any more. Entries are replaced whenever a project's
    /// hash moves, and the blobs their old manifests named stay behind; without this the store
    /// only ever grows.
    /// </summary>
    /// <returns>How many blobs were removed.</returns>
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

    // Above this, a file is hashed by streaming and copied separately; below it, the bytes are
    // held in memory so a new blob is written without reading the source a second time. Build
    // output is overwhelmingly small files — assemblies, pdbs, json — so the in-memory path is
    // the one that runs, and it halves the I/O of staging a cold run.
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

        // Already stored: identical content by definition, so there is nothing to write. This is
        // where the saving comes from — the same assembly copied into a dozen projects' bin
        // folders is hashed a dozen times and written once.
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
            // Another process wrote the same blob first. Content-addressed, so its copy is
            // identical and equally valid; discard the redundant one rather than the winner's.
            File.Delete(temporary);
        }

        return digest;
    }

    // Sharded one level by the first two hex characters: a flat directory of tens of thousands
    // of blobs is slow to enumerate on every filesystem worth naming.
    private string BlobPath(string digest) =>
        Path.Combine(_root, BlobsDirectoryName, digest[..2], digest);

    private string TargetDirectory(ProjectId project, string configuration) =>
        Path.Combine(_root, project.Value, configuration);

    private string CurrentFile(ProjectId project, string configuration) =>
        Path.Combine(TargetDirectory(project, configuration), CurrentFileName);

    private bool TryReadManifest(ProjectId project, string configuration, out Manifest manifest, out StoredTargetHash hash)
    {
        manifest = default!;
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
