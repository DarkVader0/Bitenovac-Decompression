using System.Xml.Linq;
using Bitenovac.CloudBuild.MsBuild;

namespace Bitenovac.CloudBuild.Unit.Tests;

public sealed class SyntheticSolutionTests
{
    [Fact]
    public void Write_ShouldCreateTheDirectory_WhenItDoesNotExist()
    {
        // Arrange
        using var directory = TestFactory.Directory();
        var path = directory.Combine("store/build-Debug.slnx");

        // Act
        var written = SyntheticSolution.Write(path, []);

        // Assert
        Assert.Equal(path, written);
        Assert.True(File.Exists(path));
    }

    [Fact]
    public void Write_ShouldListEveryProjectRelativeToTheSolution_WhenSeveralAreGiven()
    {
        // Arrange
        using var directory = TestFactory.Directory();
        var path = directory.Combine("store/build.slnx");
        string[] projects = [directory.Combine("src/A/A.csproj"), directory.Combine("src/B/B.csproj")];

        // Act
        SyntheticSolution.Write(path, projects);

        // Assert
        var paths = XDocument.Load(path).Root!
            .Elements("Project")
            .Select(project => (string)project.Attribute("Path")!);
        Assert.Equal(["../src/A/A.csproj", "../src/B/B.csproj"], paths);
    }

    [Fact]
    public void Write_ShouldProduceASolutionRoot_WhenNoProjectIsGiven()
    {
        // Arrange
        using var directory = TestFactory.Directory();
        var path = directory.Combine("build.slnx");

        // Act
        SyntheticSolution.Write(path, []);

        // Assert
        var root = XDocument.Load(path).Root!;
        Assert.Equal("Solution", root.Name.LocalName);
        Assert.Empty(root.Elements());
    }
}
