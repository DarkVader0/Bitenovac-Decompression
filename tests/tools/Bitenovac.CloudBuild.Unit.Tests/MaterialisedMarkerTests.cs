using Bitenovac.CloudBuild.Storage;

namespace Bitenovac.CloudBuild.Unit.Tests;

public sealed class MaterialisedMarkerTests
{
    [Fact]
    public void Matches_ShouldReturnFalse_WhenNoMarkerWasWritten()
    {
        // Arrange
        using var repository = TestFactory.Directory();
        var projectDirectory = repository.Combine("src/A");
        Directory.CreateDirectory(Path.Combine(projectDirectory, "bin"));

        // Act
        var matches = MaterialisedMarker.Matches(
            repository.Path, TestFactory.Id("src/A/A.csproj"), "Debug", "hash", projectDirectory);

        // Assert
        Assert.False(matches);
    }

    [Fact]
    public void Matches_ShouldReturnFalse_WhenTheOutputHasBeenDeletedSince()
    {
        // Arrange
        using var repository = TestFactory.Directory();
        var project = TestFactory.Id("src/A/A.csproj");
        MaterialisedMarker.Write(repository.Path, project, "Debug", "hash");

        // Act
        var matches = MaterialisedMarker.Matches(
            repository.Path, project, "Debug", "hash", repository.Combine("src/A"));

        // Assert
        Assert.False(matches);
    }

    [Fact]
    public void Matches_ShouldReturnFalse_WhenTheWorkspaceHoldsADifferentHash()
    {
        // Arrange
        using var repository = TestFactory.Directory();
        var project = TestFactory.Id("src/A/A.csproj");
        var projectDirectory = repository.Combine("src/A");
        Directory.CreateDirectory(Path.Combine(projectDirectory, "bin"));
        MaterialisedMarker.Write(repository.Path, project, "Debug", "old-hash");

        // Act
        var matches = MaterialisedMarker.Matches(repository.Path, project, "Debug", "new-hash", projectDirectory);

        // Assert
        Assert.False(matches);
    }

    [Fact]
    public void Matches_ShouldReturnFalse_WhenTheMarkerWasWrittenForAnotherConfiguration()
    {
        // Arrange
        using var repository = TestFactory.Directory();
        var project = TestFactory.Id("src/A/A.csproj");
        var projectDirectory = repository.Combine("src/A");
        Directory.CreateDirectory(Path.Combine(projectDirectory, "bin"));
        MaterialisedMarker.Write(repository.Path, project, "Debug", "hash");

        // Act
        var matches = MaterialisedMarker.Matches(repository.Path, project, "Release", "hash", projectDirectory);

        // Assert
        Assert.False(matches);
    }

    [Fact]
    public void Matches_ShouldReturnTrue_WhenTheMarkerAndTheOutputBothSayThisHash()
    {
        // Arrange
        using var repository = TestFactory.Directory();
        var project = TestFactory.Id("src/A/A.csproj");
        var projectDirectory = repository.Combine("src/A");
        Directory.CreateDirectory(Path.Combine(projectDirectory, "bin"));
        MaterialisedMarker.Write(repository.Path, project, "Debug", "hash");

        // Act
        var matches = MaterialisedMarker.Matches(repository.Path, project, "Debug", "hash", projectDirectory);

        // Assert
        Assert.True(matches);
    }

    [Fact]
    public void Write_ShouldPlaceTheMarkerUnderArtifacts_SoItIsNeverSweptIntoAStoreEntry()
    {
        // Arrange
        using var repository = TestFactory.Directory();
        var project = TestFactory.Id("src/A/A.csproj");

        // Act
        MaterialisedMarker.Write(repository.Path, project, "Debug", "hash");

        // Assert
        var marker = repository.Combine("artifacts/materialised/Debug/src/A/A.csproj.hash");
        Assert.Equal("hash", File.ReadAllText(marker));
    }

    [Fact]
    public void Write_ShouldReplaceTheRecordedHash_WhenTheWorkspaceMovesOn()
    {
        // Arrange
        using var repository = TestFactory.Directory();
        var project = TestFactory.Id("src/A/A.csproj");
        var projectDirectory = repository.Combine("src/A");
        Directory.CreateDirectory(Path.Combine(projectDirectory, "bin"));
        MaterialisedMarker.Write(repository.Path, project, "Debug", "old-hash");

        // Act
        MaterialisedMarker.Write(repository.Path, project, "Debug", "new-hash");

        // Assert
        Assert.True(MaterialisedMarker.Matches(repository.Path, project, "Debug", "new-hash", projectDirectory));
    }
}
