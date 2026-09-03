using Bitenovac.CloudBuild.Planning;

namespace Bitenovac.CloudBuild.Unit.Tests;

public sealed class PlanStateTests
{
    private const int Precision = 6;

    [Fact]
    public void For_ShouldReturnTheEntries_WhenTheConfigurationIsPresent()
    {
        // Arrange
        var plan = TestFactory.Plan("Debug", TestFactory.Entry("src/A/A.csproj"));

        // Act
        var entries = plan.For("Debug");

        // Assert
        Assert.Equal("src/A/A.csproj", Assert.Single(entries).ProjectPath);
    }

    [Fact]
    public void For_ShouldReturnNothing_WhenTheConfigurationIsAbsent()
    {
        // Arrange
        var plan = TestFactory.Plan("Debug", TestFactory.Entry("src/A/A.csproj"));

        // Act
        var entries = plan.For("Release");

        // Assert
        Assert.Empty(entries);
    }

    [Fact]
    public void Save_ShouldCreateTheDirectory_WhenItDoesNotExist()
    {
        // Arrange
        using var directory = TestFactory.Directory();
        var path = directory.Combine("does/not/exist/plan.json");
        var plan = TestFactory.Plan("Debug");

        // Act
        plan.Save(path);

        // Assert
        Assert.True(File.Exists(path));
    }

    [Fact]
    public void Load_ShouldReturnEveryFieldItWasSavedWith_WhenThePlanIsReadBack()
    {
        // Arrange
        using var directory = TestFactory.Directory();
        var path = directory.Combine("plan.json");
        var saved = TestFactory.Entry(
            "src/A/A.csproj",
            fullPath: @"C:\repo\src\A\A.csproj",
            assemblyName: "A",
            isTestProject: true,
            excludeFromCoverage: true,
            minimumLineCoverage: 92.5,
            minimumBranchCoverage: 81.25,
            cacheTestResults: false,
            ownHash: "own",
            fullHash: "full",
            forced: true,
            hit: true,
            shouldGateCoverage: false);
        TestFactory.Plan("Release", saved).Save(path);

        // Act
        var loaded = PlanState.Load(path).For("Release").Single();

        // Assert
        Assert.Equal(saved.ProjectPath, loaded.ProjectPath);
        Assert.Equal(saved.FullPath, loaded.FullPath);
        Assert.Equal(saved.AssemblyName, loaded.AssemblyName);
        Assert.True(loaded.IsTestProject);
        Assert.True(loaded.ExcludeFromCoverage);
        Assert.Equal(92.5, loaded.MinimumLineCoverage, Precision);
        Assert.Equal(81.25, loaded.MinimumBranchCoverage, Precision);
        Assert.False(loaded.CacheTestResults);
        Assert.Equal("own", loaded.OwnHash);
        Assert.Equal("full", loaded.FullHash);
        Assert.True(loaded.Forced);
        Assert.True(loaded.Hit);
        Assert.False(loaded.ShouldGateCoverage);
    }

    [Fact]
    public void Load_ShouldKeepBothConfigurations_WhenThePlanHoldsEach()
    {
        // Arrange
        using var directory = TestFactory.Directory();
        var path = directory.Combine("plan.json");
        var plan = new PlanState(new Dictionary<string, List<PlanEntry>>
        {
            ["Debug"] = [TestFactory.Entry("src/A/A.csproj")],
            ["Release"] = [TestFactory.Entry("src/A/A.csproj"), TestFactory.Entry("src/B/B.csproj")],
        });
        plan.Save(path);

        // Act
        var loaded = PlanState.Load(path);

        // Assert
        Assert.Single(loaded.For("Debug"));
        Assert.Equal(2, loaded.For("Release").Count);
    }

    [Fact]
    public void Load_ShouldThrow_WhenNoPlanExists()
    {
        // Arrange
        using var directory = TestFactory.Directory();
        var path = directory.Combine("plan.json");

        // Act
        var act = () => PlanState.Load(path);

        // Assert
        var exception = Assert.Throws<InvalidOperationException>(act);
        Assert.Contains("Run 'plan' first", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Load_ShouldThrow_WhenThePlanDeserialisesToNull()
    {
        // Arrange
        using var directory = TestFactory.Directory();
        var path = TestFactory.WriteFile(directory.Combine("plan.json"), "null");

        // Act
        var act = () => PlanState.Load(path);

        // Assert
        var exception = Assert.Throws<InvalidOperationException>(act);
        Assert.Contains("could not be read", exception.Message, StringComparison.Ordinal);
    }
}
