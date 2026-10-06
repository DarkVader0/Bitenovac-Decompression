using Bitenovac.CloudBuild.Hashing;

namespace Bitenovac.CloudBuild.Unit.Tests;

public sealed class ToolVersionTests
{
    [Fact]
    public void Compute_ShouldReturnTheSameHash_WhenNothingChanged()
    {
        // Arrange
        using var tool = TestFactory.Directory();
        TestFactory.WriteFile(tool.Path, "Tool.dll", "assembly");
        TestFactory.WriteFile(tool.Path, "Tool.Core.dll", "core");
        var first = ToolVersion.Compute(tool.Path);

        // Act
        var second = ToolVersion.Compute(tool.Path);

        // Assert
        Assert.Equal(first, second);
    }

    [Fact]
    public void Compute_ShouldChange_WhenAnAssemblyChanges()
    {
        // Arrange
        using var tool = TestFactory.Directory();
        TestFactory.WriteFile(tool.Path, "Tool.dll", "assembly");
        var before = ToolVersion.Compute(tool.Path);

        // Act
        TestFactory.WriteFile(tool.Path, "Tool.dll", "rebuilt assembly");
        var after = ToolVersion.Compute(tool.Path);

        // Assert
        Assert.NotEqual(before, after);
    }

    [Fact]
    public void Compute_ShouldChange_WhenAnAssemblyIsAdded()
    {
        // Arrange
        using var tool = TestFactory.Directory();
        TestFactory.WriteFile(tool.Path, "Tool.dll", "assembly");
        var before = ToolVersion.Compute(tool.Path);

        // Act
        TestFactory.WriteFile(tool.Path, "Dependency.dll", "dependency");
        var after = ToolVersion.Compute(tool.Path);

        // Assert
        Assert.NotEqual(before, after);
    }

    [Theory]
    [InlineData("Tool.pdb")]
    [InlineData("Tool.deps.json")]
    [InlineData("nested/Other.dll")]
    public void Compute_ShouldNotChange_WhenAFileOutsideTheHashedSetAppears(string relativePath)
    {
        // Arrange
        using var tool = TestFactory.Directory();
        TestFactory.WriteFile(tool.Path, "Tool.dll", "assembly");
        var before = ToolVersion.Compute(tool.Path);

        // Act
        TestFactory.WriteFile(tool.Path, relativePath, "anything at all");
        var after = ToolVersion.Compute(tool.Path);

        // Assert
        Assert.Equal(before, after);
    }

    [Fact]
    public void Compute_ShouldDependOnTheNameAsWellAsTheContent_WhenAnAssemblyIsRenamed()
    {
        // Arrange
        using var tool = TestFactory.Directory();
        TestFactory.WriteFile(tool.Path, "Tool.dll", "assembly");
        var before = ToolVersion.Compute(tool.Path);

        // Act
        File.Move(tool.Combine("Tool.dll"), tool.Combine("Renamed.dll"));
        var after = ToolVersion.Compute(tool.Path);

        // Assert
        Assert.NotEqual(before, after);
    }
}
