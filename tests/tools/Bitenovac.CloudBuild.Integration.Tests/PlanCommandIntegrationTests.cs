using Bitenovac.CloudBuild.Commands;

namespace Bitenovac.CloudBuild.Integration.Tests;

public sealed class PlanCommandIntegrationTests
{
    private readonly CapturedOutput _output = new();

    [Fact]
    public void Run_ShouldFail_WhenTheRepositoryHoldsNoProjects()
    {
        // Arrange
        using var fixture = FixtureRepository.CreateEmpty();

        // Act
        var exitCode = PlanCommand.Run(fixture.Options, _output.Pipeline);

        // Assert
        Assert.Equal(1, exitCode);
        Assert.False(File.Exists(fixture.Options.PlanFile));
    }

    [Fact]
    public void Run_ShouldFailBeforeRestoring_WhenAProjectDeclaresTargetFrameworks()
    {
        // Arrange
        using var fixture = FixtureRepository.Create();
        fixture.Write(FixtureRepository.LibraryProject, """
            <Project Sdk="Microsoft.NET.Sdk">
                <PropertyGroup>
                    <TargetFrameworks>net10.0;net9.0</TargetFrameworks>
                </PropertyGroup>
            </Project>
            """);

        // Act
        var exitCode = PlanCommand.Run(fixture.Options, _output.Pipeline);

        // Assert
        Assert.Equal(1, exitCode);
        Assert.False(File.Exists(fixture.Combine("src/Lib/obj/project.assets.json")));
        Assert.False(File.Exists(fixture.Options.PlanFile));
    }

    [Fact]
    public void Run_ShouldPlanBothConfigurations_WhenTheRepositoryIsValid()
    {
        // Arrange
        using var fixture = FixtureRepository.Create();

        // Act
        var exitCode = PlanCommand.Run(fixture.Options, _output.Pipeline);

        // Assert
        Assert.Equal(0, exitCode);
        var plan = Planning.PlanState.Load(fixture.Options.PlanFile);
        foreach (var configuration in PipelineOptions.Configurations)
        {
            var entries = plan.For(configuration);
            Assert.Equal(2, entries.Count);
            Assert.Contains(entries, entry => entry.ProjectPath == FixtureRepository.LibraryProject && !entry.IsTestProject);
            Assert.Contains(entries, entry => entry.ProjectPath == FixtureRepository.TestProject && entry.IsTestProject);
        }

        var debug = plan.For("Debug").Single(entry => entry.ProjectPath == FixtureRepository.LibraryProject);
        var release = plan.For("Release").Single(entry => entry.ProjectPath == FixtureRepository.LibraryProject);
        Assert.NotEqual(debug.FullHash, release.FullHash);
    }

    [Fact]
    public void Graph_ShouldPrintEveryProjectAndItsDependencies_ForBothConfigurations()
    {
        // Arrange
        using var fixture = FixtureRepository.Create();

        // Act
        var exitCode = GraphCommand.Run(fixture.Options, _output.Pipeline);
        var reported = _output.ToString();

        // Assert
        Assert.Equal(0, exitCode);
        Assert.Contains("=== Debug ===", reported, StringComparison.Ordinal);
        Assert.Contains("=== Release ===", reported, StringComparison.Ordinal);
        Assert.Contains($"    -> {FixtureRepository.LibraryProject}", reported, StringComparison.Ordinal);
    }
}
