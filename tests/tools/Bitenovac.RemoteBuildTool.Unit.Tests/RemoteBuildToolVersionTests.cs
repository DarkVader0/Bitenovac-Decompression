using Bitenovac.RemoteBuildTool.Hashing;

namespace Bitenovac.RemoteBuildTool.Unit.Tests;

public sealed class RemoteBuildToolVersionTests
{
    [Fact]
    public void Compute_ShouldReturnTheSameHash_WhenNothingChanged()
    {
        // Arrange
        using var remoteBuildTool = TestFactory.Directory();
        TestFactory.WriteFile(remoteBuildTool.Path, "RemoteBuildTool.dll", "assembly");
        TestFactory.WriteFile(remoteBuildTool.Path, "RemoteBuildTool.Core.dll", "core");
        var first = RemoteBuildToolVersion.Compute(remoteBuildTool.Path);

        // Act
        var second = RemoteBuildToolVersion.Compute(remoteBuildTool.Path);

        // Assert
        Assert.Equal(first, second);
    }

    [Fact]
    public void Compute_ShouldChange_WhenAnAssemblyChanges()
    {
        // Arrange
        using var remoteBuildTool = TestFactory.Directory();
        TestFactory.WriteFile(remoteBuildTool.Path, "RemoteBuildTool.dll", "assembly");
        var before = RemoteBuildToolVersion.Compute(remoteBuildTool.Path);

        // Act
        TestFactory.WriteFile(remoteBuildTool.Path, "RemoteBuildTool.dll", "rebuilt assembly");
        var after = RemoteBuildToolVersion.Compute(remoteBuildTool.Path);

        // Assert
        Assert.NotEqual(before, after);
    }

    [Fact]
    public void Compute_ShouldChange_WhenAnAssemblyIsAdded()
    {
        // Arrange
        using var remoteBuildTool = TestFactory.Directory();
        TestFactory.WriteFile(remoteBuildTool.Path, "RemoteBuildTool.dll", "assembly");
        var before = RemoteBuildToolVersion.Compute(remoteBuildTool.Path);

        // Act
        TestFactory.WriteFile(remoteBuildTool.Path, "Dependency.dll", "dependency");
        var after = RemoteBuildToolVersion.Compute(remoteBuildTool.Path);

        // Assert
        Assert.NotEqual(before, after);
    }

    [Theory]
    [InlineData("RemoteBuildTool.pdb")]
    [InlineData("RemoteBuildTool.deps.json")]
    [InlineData("nested/Other.dll")]
    public void Compute_ShouldNotChange_WhenAFileOutsideTheHashedSetAppears(string relativePath)
    {
        // Arrange
        using var remoteBuildTool = TestFactory.Directory();
        TestFactory.WriteFile(remoteBuildTool.Path, "RemoteBuildTool.dll", "assembly");
        var before = RemoteBuildToolVersion.Compute(remoteBuildTool.Path);

        // Act
        TestFactory.WriteFile(remoteBuildTool.Path, relativePath, "anything at all");
        var after = RemoteBuildToolVersion.Compute(remoteBuildTool.Path);

        // Assert
        Assert.Equal(before, after);
    }

    [Fact]
    public void Compute_ShouldDependOnTheNameAsWellAsTheContent_WhenAnAssemblyIsRenamed()
    {
        // Arrange
        using var remoteBuildTool = TestFactory.Directory();
        TestFactory.WriteFile(remoteBuildTool.Path, "RemoteBuildTool.dll", "assembly");
        var before = RemoteBuildToolVersion.Compute(remoteBuildTool.Path);

        // Act
        File.Move(remoteBuildTool.Combine("RemoteBuildTool.dll"), remoteBuildTool.Combine("Renamed.dll"));
        var after = RemoteBuildToolVersion.Compute(remoteBuildTool.Path);

        // Assert
        Assert.NotEqual(before, after);
    }
}
