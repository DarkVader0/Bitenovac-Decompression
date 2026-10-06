using Bitenovac.RemoteBuildTool.MsBuild;

namespace Bitenovac.RemoteBuildTool.Unit.Tests;

public sealed class RestoreOutputsTests
{
    [Fact]
    public void Materialise_ShouldPutBackWhatWasSaved_WhenTheCheckoutWasCleanedInBetween()
    {
        // Arrange
        using var directory = TestFactory.Directory();
        var repository = directory.Combine("repo");
        var root = directory.Combine("pr/restore");
        var project = TestFactory.WriteFile(repository, "src/A/A.csproj", "<Project/>");
        TestFactory.WriteFile(repository, "src/A/obj/project.assets.json", "assets");
        TestFactory.WriteFile(repository, "src/A/obj/A.csproj.nuget.g.props", "props");
        RestoreOutputs.Save(repository, [project], root);
        Directory.Delete(Path.Combine(repository, "src", "A", "obj"), recursive: true);

        // Act
        var restored = RestoreOutputs.Materialise(repository, project, root);

        // Assert
        Assert.True(restored);
        Assert.Equal("assets", File.ReadAllText(Path.Combine(repository, "src", "A", "obj", "project.assets.json")));
        Assert.Equal("props", File.ReadAllText(Path.Combine(repository, "src", "A", "obj", "A.csproj.nuget.g.props")));
    }

    [Fact]
    public void Save_ShouldKeepOnlyTheTopLevelOfObj_WhenBuildOutputIsAlsoThere()
    {
        // Arrange
        using var directory = TestFactory.Directory();
        var repository = directory.Combine("repo");
        var root = directory.Combine("pr/restore");
        var project = TestFactory.WriteFile(repository, "src/A/A.csproj", "<Project/>");
        TestFactory.WriteFile(repository, "src/A/obj/project.assets.json", "assets");
        TestFactory.WriteFile(repository, "src/A/obj/Debug/net10.0/A.dll", "assembly");

        // Act
        RestoreOutputs.Save(repository, [project], root);

        // Assert
        Assert.True(File.Exists(Path.Combine(root, "src", "A", "project.assets.json")));
        Assert.False(Directory.Exists(Path.Combine(root, "src", "A", "Debug")));
    }

    [Fact]
    public void Save_ShouldSkipTheProject_WhenItHasNoObjDirectory()
    {
        // Arrange
        using var directory = TestFactory.Directory();
        var repository = directory.Combine("repo");
        var root = directory.Combine("pr/restore");
        var project = TestFactory.WriteFile(repository, "src/A/A.csproj", "<Project/>");

        // Act
        RestoreOutputs.Save(repository, [project], root);

        // Assert
        Assert.False(Directory.Exists(Path.Combine(root, "src", "A")));
    }

    [Fact]
    public void Materialise_ShouldLeaveTheWorkspaceAlone_WhenItIsAlreadyRestored()
    {
        // Arrange
        using var directory = TestFactory.Directory();
        var repository = directory.Combine("repo");
        var root = directory.Combine("pr/restore");
        var project = TestFactory.WriteFile(repository, "src/A/A.csproj", "<Project/>");
        TestFactory.WriteFile(root, "src/A/project.assets.json", "saved");
        var assets = TestFactory.WriteFile(repository, "src/A/obj/project.assets.json", "from main");

        // Act
        var restored = RestoreOutputs.Materialise(repository, project, root);

        // Assert
        Assert.True(restored);
        Assert.Equal("from main", File.ReadAllText(assets));
    }

    [Fact]
    public void Materialise_ShouldReportNotRestored_WhenNothingWasSaved()
    {
        // Arrange
        using var directory = TestFactory.Directory();
        var repository = directory.Combine("repo");
        var project = TestFactory.WriteFile(repository, "src/A/A.csproj", "<Project/>");

        // Act
        var restored = RestoreOutputs.Materialise(repository, project, directory.Combine("pr/restore"));

        // Assert
        Assert.False(restored);
        Assert.False(Directory.Exists(Path.Combine(repository, "src", "A", "obj")));
    }
}
