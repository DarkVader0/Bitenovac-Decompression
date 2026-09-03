using Bitenovac.CloudBuild.MsBuild;

namespace Bitenovac.CloudBuild.Unit.Tests;

public sealed class ProjectDiscoveryTests
{
    [Fact]
    public void FindRelativePaths_ShouldReturnNothing_WhenTheRepositoryHoldsNoProjects()
    {
        // Arrange
        using var repository = TestFactory.Directory();
        TestFactory.WriteFile(repository.Path, "README.md", "nothing to build");

        // Act
        var paths = ProjectDiscovery.FindRelativePaths(repository.Path);

        // Assert
        Assert.Empty(paths);
    }

    [Fact]
    public void FindRelativePaths_ShouldReturnEveryProjectExtension_WhenSeveralLanguagesArePresent()
    {
        // Arrange
        using var repository = TestFactory.Directory();
        TestFactory.WriteFile(repository.Path, "src/A/A.csproj", "");
        TestFactory.WriteFile(repository.Path, "src/B/B.fsproj", "");
        TestFactory.WriteFile(repository.Path, "src/C/C.vbproj", "");
        TestFactory.WriteFile(repository.Path, "src/D/D.txt", "");

        // Act
        var paths = ProjectDiscovery.FindRelativePaths(repository.Path);

        // Assert
        Assert.Equal(["src/A/A.csproj", "src/B/B.fsproj", "src/C/C.vbproj"], paths);
    }

    [Theory]
    [InlineData("bin/Leftover.csproj")]
    [InlineData("obj/Leftover.csproj")]
    [InlineData("artifacts/Leftover.csproj")]
    [InlineData("src/A/bin/Debug/Leftover.csproj")]
    [InlineData("src/A/obj/Leftover.csproj")]
    public void FindRelativePaths_ShouldSkipTheProject_WhenAnyPathSegmentIsAnOutputDirectory(string relativePath)
    {
        // Arrange
        using var repository = TestFactory.Directory();
        TestFactory.WriteFile(repository.Path, relativePath, "");

        // Act
        var paths = ProjectDiscovery.FindRelativePaths(repository.Path);

        // Assert
        Assert.Empty(paths);
    }

    [Fact]
    public void FindRelativePaths_ShouldReturnPathsSortedOrdinally_WhenSeveralProjectsExist()
    {
        // Arrange
        using var repository = TestFactory.Directory();
        TestFactory.WriteFile(repository.Path, "tests/Z/Z.csproj", "");
        TestFactory.WriteFile(repository.Path, "src/A/A.csproj", "");
        TestFactory.WriteFile(repository.Path, "src/B/B.csproj", "");

        // Act
        var paths = ProjectDiscovery.FindRelativePaths(repository.Path);

        // Assert
        Assert.Equal(["src/A/A.csproj", "src/B/B.csproj", "tests/Z/Z.csproj"], paths);
    }
}
