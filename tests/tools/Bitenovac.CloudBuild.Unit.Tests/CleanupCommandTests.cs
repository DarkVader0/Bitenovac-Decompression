using Bitenovac.CloudBuild.Commands;

namespace Bitenovac.CloudBuild.Unit.Tests;

public sealed class CleanupCommandTests
{
    [Fact]
    public void Run_ShouldLeaveTheStore_WhenArtifactsAreToBeKept()
    {
        // Arrange
        using var directory = TestFactory.Directory();
        var prStore = directory.Combine("pr");
        Directory.CreateDirectory(prStore);
        var options = TestFactory.Options(directory.Path, directory.Combine("main"), prStore);

        // Act
        var exitCode = CleanupCommand.Run(options, keepArtifacts: true, TestFactory.Silence());

        // Assert
        Assert.Equal(0, exitCode);
        Assert.True(Directory.Exists(prStore));
    }

    [Fact]
    public void Run_ShouldSucceed_WhenThereIsNoStoreToRemove()
    {
        // Arrange
        using var directory = TestFactory.Directory();
        var prStore = directory.Combine("pr");
        var options = TestFactory.Options(directory.Path, directory.Combine("main"), prStore);

        // Act
        var exitCode = CleanupCommand.Run(options, keepArtifacts: false, TestFactory.Silence());

        // Assert
        Assert.Equal(0, exitCode);
        Assert.False(Directory.Exists(prStore));
    }

    [Fact]
    public void Run_ShouldRemoveTheStoreAndItsContent_WhenItExists()
    {
        // Arrange
        using var directory = TestFactory.Directory();
        var prStore = directory.Combine("pr");
        TestFactory.WriteFile(prStore, "blobs/ab/abcdef", "content");
        var options = TestFactory.Options(directory.Path, directory.Combine("main"), prStore);

        // Act
        var exitCode = CleanupCommand.Run(options, keepArtifacts: false, TestFactory.Silence());

        // Assert
        Assert.Equal(0, exitCode);
        Assert.False(Directory.Exists(prStore));
    }

    [Fact]
    public void Run_ShouldLeaveMainAlone_WhenTheRunStoreIsRemoved()
    {
        // Arrange
        using var directory = TestFactory.Directory();
        var mainStore = directory.Combine("main");
        var prStore = directory.Combine("pr");
        Directory.CreateDirectory(mainStore);
        Directory.CreateDirectory(prStore);
        var options = TestFactory.Options(directory.Path, mainStore, prStore);

        // Act
        CleanupCommand.Run(options, keepArtifacts: false, TestFactory.Silence());

        // Assert
        Assert.True(Directory.Exists(mainStore));
    }
}
