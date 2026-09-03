using System.Xml.Linq;
using Bitenovac.CloudBuild.MsBuild;

namespace Bitenovac.CloudBuild.Unit.Tests;

public sealed class SyntheticBuildProjectTests
{
    [Fact]
    public void Write_ShouldCreateTheDirectory_WhenItDoesNotExist()
    {
        // Arrange
        using var directory = TestFactory.Directory();
        var path = directory.Combine("store/build-Debug.proj");

        // Act
        var written = SyntheticBuildProject.Write(path, []);

        // Assert
        Assert.Equal(path, written);
        Assert.True(File.Exists(path));
    }

    [Fact]
    public void Write_ShouldNotDeclareAnSdk_SoRepositoryDefaultsAreNotImported()
    {
        // Arrange
        using var directory = TestFactory.Directory();
        var path = directory.Combine("build.proj");

        // Act
        SyntheticBuildProject.Write(path, []);

        // Assert
        Assert.Null(XDocument.Load(path).Root!.Attribute("Sdk"));
    }

    [Fact]
    public void Write_ShouldListEveryProject_WhenSeveralAreGiven()
    {
        // Arrange
        using var directory = TestFactory.Directory();
        var path = directory.Combine("build.proj");
        string[] projects = [@"C:\repo\A\A.csproj", @"C:\repo\B\B.csproj"];

        // Act
        SyntheticBuildProject.Write(path, projects);

        // Assert
        var includes = XDocument.Load(path)
            .Descendants("CloudBuildProject")
            .Select(item => (string)item.Attribute("Include")!);
        Assert.Equal(projects, includes);
    }

    [Fact]
    public void Write_ShouldRestoreSeriallyAndBuildInParallel_WhenTheProjectIsRead()
    {
        // Arrange
        using var directory = TestFactory.Directory();
        var path = directory.Combine("build.proj");

        // Act
        SyntheticBuildProject.Write(path, [@"C:\repo\A\A.csproj"]);

        // Assert
        var targets = XDocument.Load(path)
            .Descendants("Target")
            .ToDictionary(target => (string)target.Attribute("Name")!, target => target.Element("MSBuild")!);
        Assert.Equal("false", (string)targets["Restore"].Attribute("BuildInParallel")!);
        Assert.Equal("Restore", (string)targets["Restore"].Attribute("Targets")!);
        Assert.Equal("true", (string)targets["Build"].Attribute("BuildInParallel")!);
        Assert.Equal("Build", (string)targets["Build"].Attribute("Targets")!);
        Assert.All(targets.Values, msbuild => Assert.Equal("@(CloudBuildProject)", (string)msbuild.Attribute("Projects")!));
    }
}
