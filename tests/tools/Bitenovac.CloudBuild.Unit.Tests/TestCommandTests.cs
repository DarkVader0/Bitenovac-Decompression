using Bitenovac.CloudBuild.Commands;
using Bitenovac.CloudBuild.Core.Planning;
using Bitenovac.CloudBuild.Storage;

namespace Bitenovac.CloudBuild.Unit.Tests;

public sealed class TestCommandTests
{
    [Fact]
    public void Run_ShouldThrow_WhenNoPlanWasWritten()
    {
        // Arrange
        using var directory = TestFactory.Directory();
        var options = TestFactory.Options(directory.Path, directory.Combine("main"), directory.Combine("pr"));

        // Act
        Action act = () => TestCommand.Run(options, "Debug", TestFactory.Silence());

        // Assert
        Assert.Throws<InvalidOperationException>(act);
    }

    [Fact]
    public void Run_ShouldSucceed_WhenTheConfigurationSelectedNothing()
    {
        // Arrange
        using var directory = TestFactory.Directory();
        var options = TestFactory.Options(directory.Path, directory.Combine("main"), directory.Combine("pr"));
        TestFactory.Plan("Debug", TestFactory.Entry("src/A/A.csproj")).Save(options.PlanFile);

        // Act
        var exitCode = TestCommand.Run(options, "Release", TestFactory.Silence());

        // Assert
        Assert.Equal(0, exitCode);
    }

    [Fact]
    public void Run_ShouldFail_WhenNoStoreHoldsTheSelectedProjectsOutput()
    {
        // Arrange
        using var directory = TestFactory.Directory();
        var options = TestFactory.Options(directory.Path, directory.Combine("main"), directory.Combine("pr"));
        var entry = TestFactory.Entry("src/A/A.csproj", fullPath: directory.Combine("src/A/A.csproj"));
        TestFactory.Plan("Debug", entry).Save(options.PlanFile);

        // Act
        var exitCode = TestCommand.Run(options, "Debug", TestFactory.Silence());

        // Assert
        Assert.Equal(1, exitCode);
    }

    [Fact]
    public void Run_ShouldMaterialiseAndStop_WhenNoSelectedProjectIsATestProject()
    {
        // Arrange
        using var directory = TestFactory.Directory();
        var options = TestFactory.Options(directory.Path, directory.Combine("main"), directory.Combine("pr"));
        var project = TestFactory.Id("src/A/A.csproj");
        var staged = directory.Combine("staged");
        TestFactory.WriteFile(staged, "bin/Debug/A.dll", "assembly");
        new LocalVolumeArtifactStore(options.PrStoreRoot)
            .Put(project, "Debug", staged, new StoredTargetHash("own", "full"));
        var entry = TestFactory.Entry(
            project.Value,
            fullPath: directory.Combine("src/A/A.csproj"),
            ownHash: "own",
            fullHash: "full");
        TestFactory.Plan("Debug", entry).Save(options.PlanFile);

        // Act
        var exitCode = TestCommand.Run(options, "Debug", TestFactory.Silence());

        // Assert
        Assert.Equal(0, exitCode);
        Assert.Equal("assembly", File.ReadAllText(directory.Combine("src/A/bin/Debug/A.dll")));
        Assert.True(MaterialisedMarker.Matches(
            options.RepositoryRoot, project, "Debug", "full", directory.Combine("src/A")));
    }

    [Fact]
    public void Run_ShouldTakeAHitFromMainRatherThanTheRunStore_WhenTheEntryWasNeverStaged()
    {
        // Arrange
        using var directory = TestFactory.Directory();
        var options = TestFactory.Options(directory.Path, directory.Combine("main"), directory.Combine("pr"));
        var project = TestFactory.Id("src/A/A.csproj");
        var staged = directory.Combine("staged");
        TestFactory.WriteFile(staged, "bin/Debug/A.dll", "from main");
        new LocalVolumeArtifactStore(options.MainStoreRoot)
            .Put(project, "Debug", staged, new StoredTargetHash("own", "full"));
        var entry = TestFactory.Entry(
            project.Value,
            fullPath: directory.Combine("src/A/A.csproj"),
            ownHash: "own",
            fullHash: "full",
            hit: true);
        TestFactory.Plan("Debug", entry).Save(options.PlanFile);

        // Act
        var exitCode = TestCommand.Run(options, "Debug", TestFactory.Silence());

        // Assert
        Assert.Equal(0, exitCode);
        Assert.Equal("from main", File.ReadAllText(directory.Combine("src/A/bin/Debug/A.dll")));
    }

    [Fact]
    public void Run_ShouldSkipMaterialising_WhenTheWorkspaceAlreadyHoldsThisHash()
    {
        // Arrange
        using var directory = TestFactory.Directory();
        var options = TestFactory.Options(directory.Path, directory.Combine("main"), directory.Combine("pr"));
        var project = TestFactory.Id("src/A/A.csproj");

        TestFactory.WriteFile(directory.Combine("src/A"), "bin/Debug/A.dll", "left by build");
        MaterialisedMarker.Write(options.RepositoryRoot, project, "Debug", "full");
        var entry = TestFactory.Entry(
            project.Value,
            fullPath: directory.Combine("src/A/A.csproj"),
            ownHash: "own",
            fullHash: "full");
        TestFactory.Plan("Debug", entry).Save(options.PlanFile);

        // Act
        var exitCode = TestCommand.Run(options, "Debug", TestFactory.Silence());

        // Assert
        Assert.Equal(0, exitCode);
        Assert.Equal("left by build", File.ReadAllText(directory.Combine("src/A/bin/Debug/A.dll")));
    }
}
