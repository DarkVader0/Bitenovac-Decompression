using Bitenovac.RemoteBuildTool.Core.Graph;
using Bitenovac.RemoteBuildTool.Toolchains;

namespace Bitenovac.RemoteBuildTool.Unit.Tests;

public sealed class ToolchainOrderTests
{
    [Fact]
    public void Resolve_ShouldReturnEachToolchainOnce_WhenProjectsOnlyReferenceTheirOwnToolchain()
    {
        // Arrange
        var projects = Projects(
            ("src/A/A.csproj", "dotnet", ["src/B/B.csproj"]),
            ("src/B/B.csproj", "dotnet", []),
            ("web/package.json", "node", []));

        // Act
        var order = ToolchainOrder.Resolve(projects);

        // Assert
        Assert.Equal(["dotnet", "node"], order);
    }

    [Fact]
    public void Resolve_ShouldPlaceTheReferencedToolchainFirst_WhenAProjectReferencesAnotherToolchain()
    {
        // Arrange
        var projects = Projects(
            ("src/A/A.csproj", "dotnet", ["native/B/Cargo.toml"]),
            ("native/B/Cargo.toml", "rust", []));

        // Act
        var order = ToolchainOrder.Resolve(projects);

        // Assert
        Assert.Equal(["rust", "dotnet"], order);
    }

    [Fact]
    public void Resolve_ShouldThrow_WhenToolchainsReferenceEachOtherInACycle()
    {
        // Arrange
        var projects = Projects(
            ("src/A/A.csproj", "dotnet", ["native/B/Cargo.toml"]),
            ("native/B/Cargo.toml", "rust", ["src/C/C.csproj"]),
            ("src/C/C.csproj", "dotnet", []));

        // Act
        var act = () => ToolchainOrder.Resolve(projects);

        // Assert
        var exception = Assert.Throws<InvalidOperationException>(act);
        Assert.Contains("dotnet -> rust -> dotnet", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Resolve_ShouldIgnoreTheReference_WhenItNamesNoKnownProject()
    {
        // Arrange
        var projects = Projects(("src/A/A.csproj", "dotnet", ["src/Gone/Gone.csproj"]));

        // Act
        var order = ToolchainOrder.Resolve(projects);

        // Assert
        Assert.Equal(["dotnet"], order);
    }

    private static List<(ProjectId Project, string Toolchain, IReadOnlyList<ProjectId> References)> Projects(
        params (string Path, string Toolchain, string[] References)[] projects) =>
        [.. projects.Select(project => (
            TestFactory.Id(project.Path),
            project.Toolchain,
            (IReadOnlyList<ProjectId>)[.. project.References.Select(TestFactory.Id)]))];
}
