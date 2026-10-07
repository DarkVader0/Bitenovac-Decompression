using Bitenovac.RemoteBuildTool.Commands;

namespace Bitenovac.RemoteBuildTool.Integration.Tests;

public sealed class ReleaseCommandIntegrationTests
{
    private static readonly DateOnly Today = new(2026, 10, 7);

    private readonly CapturedOutput _output = new();

    [Fact]
    public void Run_ShouldPackLibrariesAndPublishApps_WhenEveryProjectBuilds()
    {
        // Arrange
        using var fixture = CreateWithApp();
        Assert.Equal(0, PlanCommand.Run(fixture.Options, _output.Pipeline));

        // Act
        var exitCode = ReleaseCommand.Run(fixture.Options, _output.Pipeline, Today);

        // Assert
        Assert.Equal(0, exitCode);
        var drop = Path.Combine(fixture.Options.DropRoot, "2026.10.07.001");
        Assert.True(File.Exists(Path.Combine(drop, "packages", "Lib.2026.10.7.1.nupkg")));
        Assert.True(File.Exists(Path.Combine(drop, "apps", "App", "App.dll")));
        Assert.DoesNotContain(Directory.EnumerateFileSystemEntries(drop, "*", SearchOption.AllDirectories), path => path.Contains("Lib.Tests", StringComparison.Ordinal));
        Assert.Equal([drop], Directory.EnumerateFileSystemEntries(fixture.Options.DropRoot));
    }

    [Fact]
    public void Run_ShouldTakeTheNextNumber_WhenTheDayAlreadyHasADrop()
    {
        // Arrange
        using var fixture = CreateWithApp();
        Directory.CreateDirectory(Path.Combine(fixture.Options.DropRoot, "2026.10.07.001"));
        Assert.Equal(0, PlanCommand.Run(fixture.Options, _output.Pipeline));

        // Act
        var exitCode = ReleaseCommand.Run(fixture.Options, _output.Pipeline, Today);

        // Assert
        Assert.Equal(0, exitCode);
        Assert.True(File.Exists(Path.Combine(fixture.Options.DropRoot, "2026.10.07.002", "packages", "Lib.2026.10.7.2.nupkg")));
    }

    [Fact]
    public void Run_ShouldDropNothing_WhenTheBuildFails()
    {
        // Arrange
        using var fixture = CreateWithApp();
        fixture.Write("src/Lib/Calculator.cs", "namespace Lib; public static class Calculator { this is not C# }");
        Assert.Equal(0, PlanCommand.Run(fixture.Options, _output.Pipeline));

        // Act
        var exitCode = ReleaseCommand.Run(fixture.Options, _output.Pipeline, Today);

        // Assert
        Assert.NotEqual(0, exitCode);
        Assert.Empty(Directory.EnumerateFileSystemEntries(fixture.Options.DropRoot));
    }

    private static FixtureRepository CreateWithApp()
    {
        var fixture = FixtureRepository.Create();
        fixture.Write("src/App/App.csproj", """
            <Project Sdk="Microsoft.NET.Sdk">
                <PropertyGroup>
                    <OutputType>Exe</OutputType>
                </PropertyGroup>
                <ItemGroup>
                    <ProjectReference Include="..\Lib\Lib.csproj"/>
                </ItemGroup>
            </Project>
            """);
        fixture.Write("src/App/Program.cs", "System.Console.WriteLine(Lib.Calculator.Add(1, 2));");
        return fixture;
    }
}
