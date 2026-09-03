using Bitenovac.CloudBuild.Hashing;

namespace Bitenovac.CloudBuild.Unit.Tests;

public sealed class ToolVersionTests
{
    [Fact]
    public void Compute_ShouldReportNoSources_WhenTheToolsDirectoryIsMissing()
    {
        // Arrange
        using var repository = TestFactory.Directory();

        // Act
        var hash = ToolVersion.Compute(repository.Path);

        // Assert
        Assert.Equal("no-tool-sources", hash);
    }

    [Fact]
    public void Compute_ShouldReturnTheSameHash_WhenNothingChanged()
    {
        // Arrange
        using var repository = TestFactory.Directory();
        TestFactory.WriteFile(repository.Path, "src/tools/Tool/Tool.csproj", "<Project/>");
        TestFactory.WriteFile(repository.Path, "src/tools/Tool/Program.cs", "return 0;");
        var first = ToolVersion.Compute(repository.Path);

        // Act
        var second = ToolVersion.Compute(repository.Path);

        // Assert
        Assert.Equal(first, second);
    }

    [Fact]
    public void Compute_ShouldChange_WhenASourceFileChanges()
    {
        // Arrange
        using var repository = TestFactory.Directory();
        TestFactory.WriteFile(repository.Path, "src/tools/Tool/Program.cs", "return 0;");
        var before = ToolVersion.Compute(repository.Path);

        // Act
        TestFactory.WriteFile(repository.Path, "src/tools/Tool/Program.cs", "return 1;");
        var after = ToolVersion.Compute(repository.Path);

        // Assert
        Assert.NotEqual(before, after);
    }

    [Fact]
    public void Compute_ShouldChange_WhenASourceFileIsAdded()
    {
        // Arrange
        using var repository = TestFactory.Directory();
        TestFactory.WriteFile(repository.Path, "src/tools/Tool/Program.cs", "return 0;");
        var before = ToolVersion.Compute(repository.Path);

        // Act
        TestFactory.WriteFile(repository.Path, "src/tools/Tool/Extra.cs", "return 0;");
        var after = ToolVersion.Compute(repository.Path);

        // Assert
        Assert.NotEqual(before, after);
    }

    [Theory]
    [InlineData("src/tools/Tool/bin/Debug/Generated.cs")]
    [InlineData("src/tools/Tool/obj/Generated.cs")]
    [InlineData("src/tools/Tool/README.md")]
    public void Compute_ShouldNotChange_WhenAFileOutsideTheHashedSetAppears(string relativePath)
    {
        // Arrange
        using var repository = TestFactory.Directory();
        TestFactory.WriteFile(repository.Path, "src/tools/Tool/Program.cs", "return 0;");
        var before = ToolVersion.Compute(repository.Path);

        // Act
        TestFactory.WriteFile(repository.Path, relativePath, "anything at all");
        var after = ToolVersion.Compute(repository.Path);

        // Assert
        Assert.Equal(before, after);
    }

    [Fact]
    public void Compute_ShouldDependOnThePathAsWellAsTheContent_WhenAFileIsRenamed()
    {
        // Arrange
        using var repository = TestFactory.Directory();
        TestFactory.WriteFile(repository.Path, "src/tools/Tool/Program.cs", "return 0;");
        var before = ToolVersion.Compute(repository.Path);

        // Act
        File.Move(
            repository.Combine("src/tools/Tool/Program.cs"),
            repository.Combine("src/tools/Tool/Entry.cs"));
        var after = ToolVersion.Compute(repository.Path);

        // Assert
        Assert.NotEqual(before, after);
    }
}
