using Bitenovac.CloudBuild.Hashing;

namespace Bitenovac.CloudBuild.Unit.Tests;

public sealed class PackageClosureReaderTests
{
    [Fact]
    public void Read_ShouldReturnNothing_WhenTheProjectHasNotBeenRestored()
    {
        // Arrange
        using var directory = TestFactory.Directory();
        var project = TestFactory.WriteFile(directory.Combine("A.csproj"), "");

        // Act
        var entries = PackageClosureReader.Read(project);

        // Assert
        Assert.Empty(entries);
    }

    [Fact]
    public void Read_ShouldReturnNothing_WhenTheAssetsFileDeclaresNoLibraries()
    {
        // Arrange
        using var directory = TestFactory.Directory();
        var project = TestFactory.WriteFile(directory.Combine("A.csproj"), "");
        TestFactory.WriteFile(directory.Combine("obj/project.assets.json"), """{ "version": 3 }""");

        // Act
        var entries = PackageClosureReader.Read(project);

        // Assert
        Assert.Empty(entries);
    }

    [Fact]
    public void Read_ShouldReturnOneSortedEntryPerLibrary_WhenTheClosureIsResolved()
    {
        // Arrange
        using var directory = TestFactory.Directory();
        var project = TestFactory.WriteFile(directory.Combine("A.csproj"), "");
        TestFactory.WriteFile(directory.Combine("obj/project.assets.json"), """
            {
              "libraries": {
                "xunit.v3/3.2.2": { "type": "package" },
                "Microsoft.Build/18.9.6": { "type": "package" },
                "coverlet.MTP/10.0.1": { "type": "package" }
              }
            }
            """);

        // Act
        var entries = PackageClosureReader.Read(project);

        // Assert
        Assert.Equal(
            [
                "package:Microsoft.Build/18.9.6",
                "package:coverlet.MTP/10.0.1",
                "package:xunit.v3/3.2.2",
            ],
            entries);
    }
}
