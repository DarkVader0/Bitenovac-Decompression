using Bitenovac.CloudBuild.Commands;
using Bitenovac.CloudBuild.Core.Planning;
using Bitenovac.CloudBuild.Planning;
using Bitenovac.CloudBuild.Storage;

namespace Bitenovac.CloudBuild.Unit.Tests;

public sealed class PromoteCommandTests
{
    [Fact]
    public void Run_ShouldThrow_WhenNoPlanWasWritten()
    {
        // Arrange
        using var directory = TestFactory.Directory();
        var options = TestFactory.Options(directory.Path, directory.Combine("main"), directory.Combine("pr"));

        // Act
        Action act = () => PromoteCommand.Run(options, TestFactory.Silence());

        // Assert
        Assert.Throws<InvalidOperationException>(act);
    }

    [Fact]
    public void Run_ShouldSucceed_WhenThePlanSelectedNothing()
    {
        // Arrange
        using var directory = TestFactory.Directory();
        var options = TestFactory.Options(directory.Path, directory.Combine("main"), directory.Combine("pr"));
        TestFactory.Plan("Debug").Save(options.PlanFile);

        // Act
        var exitCode = PromoteCommand.Run(options, TestFactory.Silence());

        // Assert
        Assert.Equal(0, exitCode);
    }

    [Fact]
    public void Run_ShouldCopyTheEntryIntoMain_WhenMainHoldsNothingForIt()
    {
        // Arrange
        using var directory = TestFactory.Directory();
        var options = TestFactory.Options(directory.Path, directory.Combine("main"), directory.Combine("pr"));
        var project = TestFactory.Id("src/A/A.csproj");
        var prStore = new LocalVolumeArtifactStore(options.PrStoreRoot);
        prStore.Put(project, "Debug", StagedFiles(directory, "from the run"), new StoredTargetHash("own", "full"));
        TestFactory.Plan("Debug", TestFactory.Entry(project.Value, ownHash: "own", fullHash: "full"))
            .Save(options.PlanFile);
        var destination = directory.Combine("out");

        // Act
        var exitCode = PromoteCommand.Run(options, TestFactory.Silence());

        // Assert
        Assert.Equal(0, exitCode);
        var mainStore = new LocalVolumeArtifactStore(options.MainStoreRoot);
        Assert.True(mainStore.TryGet(project, "Debug", destination, out var hash));
        Assert.Equal(new StoredTargetHash("own", "full"), hash);
        Assert.Equal("from the run", File.ReadAllText(Path.Combine(destination, "bin", "A.dll")));
    }

    [Fact]
    public void Run_ShouldLeaveMainUntouched_WhenItAlreadyHoldsTheSameFullHash()
    {
        // Arrange
        using var directory = TestFactory.Directory();
        var options = TestFactory.Options(directory.Path, directory.Combine("main"), directory.Combine("pr"));
        var project = TestFactory.Id("src/A/A.csproj");
        var hash = new StoredTargetHash("own", "full");

        var mainStore = new LocalVolumeArtifactStore(options.MainStoreRoot);
        mainStore.Put(project, "Debug", StagedFiles(directory, "complete", includeTestResult: true), hash);
        var prStore = new LocalVolumeArtifactStore(options.PrStoreRoot);
        prStore.Put(project, "Debug", StagedFiles(directory, "partial"), hash);
        TestFactory.Plan("Debug", TestFactory.Entry(project.Value, ownHash: "own", fullHash: "full"))
            .Save(options.PlanFile);
        var destination = directory.Combine("out");

        // Act
        PromoteCommand.Run(options, TestFactory.Silence());

        // Assert
        mainStore.TryGet(project, "Debug", destination, out _);
        Assert.Equal("complete", File.ReadAllText(Path.Combine(destination, "bin", "A.dll")));
        Assert.True(File.Exists(Path.Combine(destination, "tests", "A.cobertura.xml")));
    }

    [Fact]
    public void Run_ShouldReplaceMainsEntry_WhenTheRunProducedANewerHash()
    {
        // Arrange
        using var directory = TestFactory.Directory();
        var options = TestFactory.Options(directory.Path, directory.Combine("main"), directory.Combine("pr"));
        var project = TestFactory.Id("src/A/A.csproj");
        var mainStore = new LocalVolumeArtifactStore(options.MainStoreRoot);
        mainStore.Put(project, "Debug", StagedFiles(directory, "old"), new StoredTargetHash("own-1", "full-1"));
        var prStore = new LocalVolumeArtifactStore(options.PrStoreRoot);
        prStore.Put(project, "Debug", StagedFiles(directory, "new"), new StoredTargetHash("own-2", "full-2"));
        TestFactory.Plan("Debug", TestFactory.Entry(project.Value, ownHash: "own-2", fullHash: "full-2"))
            .Save(options.PlanFile);
        var destination = directory.Combine("out");

        // Act
        PromoteCommand.Run(options, TestFactory.Silence());

        // Assert
        Assert.True(mainStore.TryGet(project, "Debug", destination, out var hash));
        Assert.Equal(new StoredTargetHash("own-2", "full-2"), hash);
        Assert.Equal("new", File.ReadAllText(Path.Combine(destination, "bin", "A.dll")));
    }

    [Fact]
    public void Run_ShouldPromoteBothConfigurations_WhenThePlanCoversEach()
    {
        // Arrange
        using var directory = TestFactory.Directory();
        var options = TestFactory.Options(directory.Path, directory.Combine("main"), directory.Combine("pr"));
        var project = TestFactory.Id("src/A/A.csproj");
        var prStore = new LocalVolumeArtifactStore(options.PrStoreRoot);
        prStore.Put(project, "Debug", StagedFiles(directory, "debug"), new StoredTargetHash("own", "full-debug"));
        prStore.Put(project, "Release", StagedFiles(directory, "release"), new StoredTargetHash("own", "full-release"));
        var plan = new PlanState(new Dictionary<string, List<PlanEntry>>
        {
            ["Debug"] = [TestFactory.Entry(project.Value, fullHash: "full-debug")],
            ["Release"] = [TestFactory.Entry(project.Value, fullHash: "full-release")],
        });
        plan.Save(options.PlanFile);

        // Act
        PromoteCommand.Run(options, TestFactory.Silence());

        // Assert
        var mainStore = new LocalVolumeArtifactStore(options.MainStoreRoot);
        Assert.True(mainStore.Contains(project, "Debug"));
        Assert.True(mainStore.Contains(project, "Release"));
    }

    /// <summary>A fresh directory laid out the way a staged entry is, holding one distinguishing byte string.</summary>
    private static string StagedFiles(TemporaryDirectory directory, string content, bool includeTestResult = false)
    {
        var source = Path.Combine(directory.Path, $"staged-{Guid.NewGuid():N}");
        TestFactory.WriteFile(source, "bin/A.dll", content);
        if (includeTestResult)
            TestFactory.WriteFile(source, "tests/A.cobertura.xml", "coverage");

        return source;
    }
}
