using Bitenovac.CloudBuild.Core.Graph;
using Bitenovac.CloudBuild.Core.Planning;
using Bitenovac.CloudBuild.Storage;

namespace Bitenovac.CloudBuild.Unit.Tests;

public sealed class LocalVolumeArtifactStoreTests
{
    private static readonly ProjectId Project = new("src/A/A.csproj");
    private static readonly StoredTargetHash Hash = new("own-hash", "full-hash");

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_ShouldThrow_WhenTheRootIsMissing(string? root)
    {
        // Arrange

        // Act
        var act = () => new LocalVolumeArtifactStore(root!);

        // Assert
        Assert.ThrowsAny<ArgumentException>(act);
    }

    [Fact]
    public void Constructor_ShouldCreateTheRoot_WhenItDoesNotExist()
    {
        // Arrange
        using var directory = TestFactory.Directory();
        var root = directory.Combine("volumes/main");

        // Act
        _ = new LocalVolumeArtifactStore(root);

        // Assert
        Assert.True(Directory.Exists(root));
    }

    [Fact]
    public void Contains_ShouldReturnFalse_WhenNothingIsStored()
    {
        // Arrange
        using var directory = TestFactory.Directory();
        var store = new LocalVolumeArtifactStore(directory.Combine("store"));

        // Act
        var contains = store.Contains(Project, "Debug");

        // Assert
        Assert.False(contains);
    }

    [Fact]
    public void Contains_ShouldReturnTrue_WhenAnEntryWasPut()
    {
        // Arrange
        using var directory = TestFactory.Directory();
        var store = new LocalVolumeArtifactStore(directory.Combine("store"));
        var source = Source(directory, "bin/A.dll", "assembly");

        // Act
        store.Put(Project, "Debug", source, Hash);

        // Assert
        Assert.True(store.Contains(Project, "Debug"));
        Assert.False(store.Contains(Project, "Release"));
    }

    [Fact]
    public void TryGetHash_ShouldReturnFalse_WhenNothingIsStored()
    {
        // Arrange
        using var directory = TestFactory.Directory();
        var store = new LocalVolumeArtifactStore(directory.Combine("store"));

        // Act
        var found = store.TryGetHash(Project, "Debug", out var hash);

        // Assert
        Assert.False(found);
        Assert.Equal(default, hash);
    }

    [Fact]
    public void TryGetHash_ShouldReportBothHashes_WhenAnEntryIsStored()
    {
        // Arrange
        using var directory = TestFactory.Directory();
        var store = new LocalVolumeArtifactStore(directory.Combine("store"));
        store.Put(Project, "Debug", Source(directory, "bin/A.dll", "assembly"), Hash);

        // Act
        var found = store.TryGetHash(Project, "Debug", out var hash);

        // Assert
        Assert.True(found);
        Assert.Equal(Hash, hash);
    }

    [Fact]
    public void TryGetHash_ShouldReturnFalse_WhenCurrentNamesAnEntryThatIsNotThere()
    {
        // Arrange
        using var directory = TestFactory.Directory();
        var root = directory.Combine("store");
        var store = new LocalVolumeArtifactStore(root);
        TestFactory.WriteFile(Path.Combine(root, Project.Value, "Debug", "current"), "vanished");

        // Act
        var found = store.TryGetHash(Project, "Debug", out _);

        // Assert
        Assert.False(found);
    }

    [Fact]
    public void TryGetHash_ShouldReturnFalse_WhenTheManifestIsNotValidJson()
    {
        // Arrange
        using var directory = TestFactory.Directory();
        var root = directory.Combine("store");
        var store = new LocalVolumeArtifactStore(root);
        var target = Path.Combine(root, Project.Value, "Debug");
        TestFactory.WriteFile(Path.Combine(target, "current"), "full-hash");
        TestFactory.WriteFile(Path.Combine(target, "entries", "full-hash", "manifest.json"), "{ not json");

        // Act
        var found = store.TryGetHash(Project, "Debug", out _);

        // Assert
        Assert.False(found);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void TryGet_ShouldThrow_WhenTheDestinationIsMissing(string? destination)
    {
        // Arrange
        using var directory = TestFactory.Directory();
        var store = new LocalVolumeArtifactStore(directory.Combine("store"));

        // Act
        Action act = () => store.TryGet(Project, "Debug", destination!, out _);

        // Assert
        Assert.ThrowsAny<ArgumentException>(act);
    }

    [Fact]
    public void TryGet_ShouldReturnFalse_WhenNothingIsStored()
    {
        // Arrange
        using var directory = TestFactory.Directory();
        var store = new LocalVolumeArtifactStore(directory.Combine("store"));

        // Act
        var found = store.TryGet(Project, "Debug", directory.Combine("out"), out _);

        // Assert
        Assert.False(found);
    }

    [Fact]
    public void TryGet_ShouldWriteEveryFile_WhenNoPrefixesAreGiven()
    {
        // Arrange
        using var directory = TestFactory.Directory();
        var store = new LocalVolumeArtifactStore(directory.Combine("store"));
        var source = directory.Combine("source");
        TestFactory.WriteFile(source, "bin/A.dll", "assembly");
        TestFactory.WriteFile(source, "obj/A.pdb", "symbols");
        TestFactory.WriteFile(source, "tests/A.cobertura.xml", "coverage");
        store.Put(Project, "Debug", source, Hash);
        var destination = directory.Combine("out");

        // Act
        var found = store.TryGet(Project, "Debug", destination, out var hash);

        // Assert
        Assert.True(found);
        Assert.Equal(Hash, hash);
        Assert.Equal("assembly", File.ReadAllText(Path.Combine(destination, "bin", "A.dll")));
        Assert.Equal("symbols", File.ReadAllText(Path.Combine(destination, "obj", "A.pdb")));
        Assert.Equal("coverage", File.ReadAllText(Path.Combine(destination, "tests", "A.cobertura.xml")));
    }

    [Fact]
    public void TryGet_ShouldWriteOnlyTheMatchingFiles_WhenPrefixesAreGiven()
    {
        // Arrange
        using var directory = TestFactory.Directory();
        var store = new LocalVolumeArtifactStore(directory.Combine("store"));
        var source = directory.Combine("source");
        TestFactory.WriteFile(source, "bin/A.dll", "assembly");
        TestFactory.WriteFile(source, "tests/A.cobertura.xml", "coverage");
        store.Put(Project, "Debug", source, Hash);
        var destination = directory.Combine("out");

        // Act
        var found = store.TryGet(Project, "Debug", destination, out _, ["bin/"]);

        // Assert
        Assert.True(found);
        Assert.True(File.Exists(Path.Combine(destination, "bin", "A.dll")));
        Assert.False(Directory.Exists(Path.Combine(destination, "tests")));
    }

    [Fact]
    public void TryGet_ShouldReadAsAMiss_WhenTheManifestNamesABlobThatIsGone()
    {
        // Arrange
        using var directory = TestFactory.Directory();
        var root = directory.Combine("store");
        var store = new LocalVolumeArtifactStore(root);
        store.Put(Project, "Debug", Source(directory, "bin/A.dll", "assembly"), Hash);
        foreach (var blob in Directory.EnumerateFiles(Path.Combine(root, "blobs"), "*", SearchOption.AllDirectories))
            File.Delete(blob);

        // Act
        var found = store.TryGet(Project, "Debug", directory.Combine("out"), out var hash);

        // Assert
        Assert.False(found);
        Assert.Equal(default, hash);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Put_ShouldThrow_WhenTheSourceDirectoryIsMissing(string? source)
    {
        // Arrange
        using var directory = TestFactory.Directory();
        var store = new LocalVolumeArtifactStore(directory.Combine("store"));

        // Act
        var act = () => store.Put(Project, "Debug", source!, Hash);

        // Assert
        Assert.ThrowsAny<ArgumentException>(act);
    }

    [Fact]
    public void Put_ShouldThrow_WhenTheFileListIsNull()
    {
        // Arrange
        using var directory = TestFactory.Directory();
        var store = new LocalVolumeArtifactStore(directory.Combine("store"));

        // Act
        var act = () => store.Put(Project, "Debug", Hash, null!);

        // Assert
        Assert.Throws<ArgumentNullException>(act);
    }

    [Fact]
    public void Put_ShouldWriteOneBlob_WhenTwoFilesHoldIdenticalContent()
    {
        // Arrange
        using var directory = TestFactory.Directory();
        var root = directory.Combine("store");
        var store = new LocalVolumeArtifactStore(root);
        var source = directory.Combine("source");
        TestFactory.WriteFile(source, "bin/A.dll", "shared bytes");
        TestFactory.WriteFile(source, "bin/nested/A.dll", "shared bytes");

        // Act
        store.Put(Project, "Debug", source, Hash);

        // Assert
        Assert.Single(Directory.EnumerateFiles(Path.Combine(root, "blobs"), "*", SearchOption.AllDirectories));
    }

    [Fact]
    public void Put_ShouldStoreTheContent_WhenAFileExceedsTheInMemoryHashLimit()
    {
        // Arrange
        using var directory = TestFactory.Directory();
        var store = new LocalVolumeArtifactStore(directory.Combine("store"));
        var source = directory.Combine("source");
        Directory.CreateDirectory(Path.Combine(source, "bin"));
        var large = new byte[(8 * 1024 * 1024) + 1];
        Random.Shared.NextBytes(large);
        File.WriteAllBytes(Path.Combine(source, "bin", "Large.dll"), large);
        var destination = directory.Combine("out");

        // Act
        store.Put(Project, "Debug", source, Hash);
        var found = store.TryGet(Project, "Debug", destination, out _);

        // Assert
        Assert.True(found);
        Assert.Equal(large, File.ReadAllBytes(Path.Combine(destination, "bin", "Large.dll")));
    }

    [Fact]
    public void Put_ShouldReplaceWhatTheEntryHeld_WhenTheSameHashIsStoredAgain()
    {
        // Arrange
        using var directory = TestFactory.Directory();
        var store = new LocalVolumeArtifactStore(directory.Combine("store"));
        store.Put(Project, "Debug", Source(directory, "bin/Old.dll", "old"), Hash);
        var replacement = directory.Combine("replacement");
        TestFactory.WriteFile(replacement, "bin/New.dll", "new");
        var destination = directory.Combine("out");

        // Act
        store.Put(Project, "Debug", replacement, Hash);
        store.TryGet(Project, "Debug", destination, out _);

        // Assert
        Assert.True(File.Exists(Path.Combine(destination, "bin", "New.dll")));
        Assert.False(File.Exists(Path.Combine(destination, "bin", "Old.dll")));
    }

    [Fact]
    public void Put_ShouldMoveThePointer_WhenANewerHashIsStored()
    {
        // Arrange
        using var directory = TestFactory.Directory();
        var store = new LocalVolumeArtifactStore(directory.Combine("store"));
        store.Put(Project, "Debug", Source(directory, "bin/A.dll", "old"), Hash);
        var newer = new StoredTargetHash("own-2", "full-2");

        // Act
        store.Put(Project, "Debug", Source(directory, "bin/A.dll", "new"), newer);

        // Assert
        Assert.True(store.TryGetHash(Project, "Debug", out var hash));
        Assert.Equal(newer, hash);
    }

    [Fact]
    public void EnumerateAsSources_ShouldYieldNothing_WhenTheDirectoryIsMissing()
    {
        // Arrange
        using var directory = TestFactory.Directory();

        // Act
        var sources = LocalVolumeArtifactStore.EnumerateAsSources(directory.Combine("absent"), "bin/");

        // Assert
        Assert.Empty(sources);
    }

    [Fact]
    public void EnumerateAsSources_ShouldPrefixAndNormaliseEveryPath_WhenTheDirectoryIsNested()
    {
        // Arrange
        using var directory = TestFactory.Directory();
        var binDirectory = directory.Combine("bin");
        TestFactory.WriteFile(binDirectory, "Debug/net10.0/A.dll", "assembly");

        // Act
        var sources = LocalVolumeArtifactStore.EnumerateAsSources(binDirectory, "bin/").ToList();

        // Assert
        Assert.Equal("bin/Debug/net10.0/A.dll", Assert.Single(sources).Path);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void AddFiles_ShouldThrow_WhenTheSourceDirectoryIsMissing(string? source)
    {
        // Arrange
        using var directory = TestFactory.Directory();
        var store = new LocalVolumeArtifactStore(directory.Combine("store"));

        // Act
        Action act = () => store.AddFiles(Project, "Debug", Hash, source!);

        // Assert
        Assert.ThrowsAny<ArgumentException>(act);
    }

    [Fact]
    public void AddFiles_ShouldReturnFalse_WhenNothingIsStored()
    {
        // Arrange
        using var directory = TestFactory.Directory();
        var store = new LocalVolumeArtifactStore(directory.Combine("store"));
        var addition = directory.Combine("addition");
        TestFactory.WriteFile(addition, "tests/A.cobertura.xml", "coverage");

        // Act
        var added = store.AddFiles(Project, "Debug", Hash, addition);

        // Assert
        Assert.False(added);
    }

    [Fact]
    public void AddFiles_ShouldReturnFalse_WhenTheStoredEntryIsForAnotherHash()
    {
        // Arrange
        using var directory = TestFactory.Directory();
        var store = new LocalVolumeArtifactStore(directory.Combine("store"));
        store.Put(Project, "Debug", Source(directory, "bin/A.dll", "assembly"), Hash);
        var addition = directory.Combine("addition");
        TestFactory.WriteFile(addition, "tests/A.cobertura.xml", "coverage");

        // Act
        var added = store.AddFiles(Project, "Debug", new StoredTargetHash("own-2", "full-2"), addition);

        // Assert
        Assert.False(added);
    }

    [Fact]
    public void AddFiles_ShouldKeepWhatTheEntryHeld_WhenTheHashMatches()
    {
        // Arrange
        using var directory = TestFactory.Directory();
        var store = new LocalVolumeArtifactStore(directory.Combine("store"));
        store.Put(Project, "Debug", Source(directory, "bin/A.dll", "assembly"), Hash);
        var addition = directory.Combine("addition");
        TestFactory.WriteFile(addition, "tests/A.cobertura.xml", "coverage");
        var destination = directory.Combine("out");

        // Act
        var added = store.AddFiles(Project, "Debug", Hash, addition);
        store.TryGet(Project, "Debug", destination, out _);

        // Assert
        Assert.True(added);
        Assert.Equal("assembly", File.ReadAllText(Path.Combine(destination, "bin", "A.dll")));
        Assert.Equal("coverage", File.ReadAllText(Path.Combine(destination, "tests", "A.cobertura.xml")));
    }

    [Fact]
    public void AddFiles_ShouldReplaceTheFile_WhenTheEntryAlreadyHoldsThatPath()
    {
        // Arrange
        using var directory = TestFactory.Directory();
        var store = new LocalVolumeArtifactStore(directory.Combine("store"));
        store.Put(Project, "Debug", Source(directory, "tests/A.cobertura.xml", "stale"), Hash);
        var addition = directory.Combine("addition");
        TestFactory.WriteFile(addition, "tests/A.cobertura.xml", "fresh");
        var destination = directory.Combine("out");

        // Act
        store.AddFiles(Project, "Debug", Hash, addition);
        store.TryGet(Project, "Debug", destination, out _);

        // Assert
        Assert.Equal("fresh", File.ReadAllText(Path.Combine(destination, "tests", "A.cobertura.xml")));
    }

    [Fact]
    public void Promote_ShouldThrow_WhenTheSourceIsNull()
    {
        // Arrange
        using var directory = TestFactory.Directory();
        var store = new LocalVolumeArtifactStore(directory.Combine("main"));

        // Act
        var act = () => store.Promote(Project, "Debug", null!);

        // Assert
        Assert.Throws<ArgumentNullException>(act);
    }

    [Fact]
    public void Promote_ShouldStoreNothing_WhenTheSourceHoldsNoEntry()
    {
        // Arrange
        using var directory = TestFactory.Directory();
        var main = new LocalVolumeArtifactStore(directory.Combine("main"));
        var pr = new LocalVolumeArtifactStore(directory.Combine("pr"));

        // Act
        main.Promote(Project, "Debug", pr);

        // Assert
        Assert.False(main.Contains(Project, "Debug"));
    }

    [Fact]
    public void Promote_ShouldCopyTheWholeEntry_WhenTheSourceHoldsIt()
    {
        // Arrange
        using var directory = TestFactory.Directory();
        var main = new LocalVolumeArtifactStore(directory.Combine("main"));
        var pr = new LocalVolumeArtifactStore(directory.Combine("pr"));
        var source = directory.Combine("source");
        TestFactory.WriteFile(source, "bin/A.dll", "assembly");
        TestFactory.WriteFile(source, "tests/A.cobertura.xml", "coverage");
        pr.Put(Project, "Debug", source, Hash);
        var destination = directory.Combine("out");

        // Act
        main.Promote(Project, "Debug", pr);

        // Assert
        Assert.True(main.TryGet(Project, "Debug", destination, out var hash));
        Assert.Equal(Hash, hash);
        Assert.Equal("assembly", File.ReadAllText(Path.Combine(destination, "bin", "A.dll")));
        Assert.Equal("coverage", File.ReadAllText(Path.Combine(destination, "tests", "A.cobertura.xml")));
    }

    [Fact]
    public void Promote_ShouldLeaveNoStagingDirectory_WhenItHasFinished()
    {
        // Arrange
        using var directory = TestFactory.Directory();
        var root = directory.Combine("main");
        var main = new LocalVolumeArtifactStore(root);
        var pr = new LocalVolumeArtifactStore(directory.Combine("pr"));
        pr.Put(Project, "Debug", Source(directory, "bin/A.dll", "assembly"), Hash);

        // Act
        main.Promote(Project, "Debug", pr);

        // Assert
        Assert.Empty(Directory.EnumerateDirectories(root, ".promote-*"));
    }

    [Fact]
    public void Prune_ShouldReturnZero_WhenNothingWasEverStored()
    {
        // Arrange
        using var directory = TestFactory.Directory();
        var store = new LocalVolumeArtifactStore(directory.Combine("store"));

        // Act
        var removed = store.Prune();

        // Assert
        Assert.Equal(0, removed);
    }

    [Fact]
    public void Prune_ShouldKeepEveryBlob_WhenAllOfThemAreStillReferenced()
    {
        // Arrange
        using var directory = TestFactory.Directory();
        var root = directory.Combine("store");
        var store = new LocalVolumeArtifactStore(root);
        store.Put(Project, "Debug", Source(directory, "bin/A.dll", "assembly"), Hash);

        // Act
        var removed = store.Prune();

        // Assert
        Assert.Equal(0, removed);
        Assert.Single(Directory.EnumerateFiles(Path.Combine(root, "blobs"), "*", SearchOption.AllDirectories));
    }

    [Fact]
    public void Prune_ShouldRemoveTheOrphan_WhenAnEntryWasReplacedUnderTheSameHash()
    {
        // Arrange
        using var directory = TestFactory.Directory();
        var root = directory.Combine("store");
        var store = new LocalVolumeArtifactStore(root);
        store.Put(Project, "Debug", Source(directory, "bin/A.dll", "old"), Hash);
        store.Put(Project, "Debug", Source(directory, "bin/A.dll", "new"), Hash);

        // Act
        var removed = store.Prune();

        // Assert
        Assert.Equal(1, removed);
        Assert.Single(Directory.EnumerateFiles(Path.Combine(root, "blobs"), "*", SearchOption.AllDirectories));
    }

    /// <summary>A fresh directory holding one file, ready to be <c>Put</c>.</summary>
    private static string Source(TemporaryDirectory directory, string relativePath, string content)
    {
        var source = Path.Combine(directory.Path, $"source-{Guid.NewGuid():N}");
        TestFactory.WriteFile(source, relativePath, content);
        return source;
    }
}
