using Bitenovac.CloudBuild.Commands;
using Bitenovac.CloudBuild.Core.Graph;
using Bitenovac.CloudBuild.Planning;
using Bitenovac.CloudBuild.Storage;

namespace Bitenovac.CloudBuild.Integration.Tests;

public sealed class CloudBuildPipelineTests
{
    private readonly CapturedOutput _output = new();

    [Fact]
    public void Pipeline_ShouldCompileTestAndPromote_WhenTheRepositoryIsCold()
    {
        // Arrange
        using var fixture = FixtureRepository.Create();

        // Act
        var planExitCode = PlanCommand.Run(fixture.Options, _output.Pipeline);
        var buildExitCode = BuildCommand.Run(fixture.Options, "Debug", _output.Pipeline);
        var testExitCode = TestCommand.Run(fixture.Options, "Debug", _output.Pipeline);
        var promoteExitCode = PromoteCommand.Run(fixture.Options, _output.Pipeline);

        // Assert
        Assert.Equal(0, planExitCode);
        Assert.Equal(0, buildExitCode);
        Assert.Equal(0, testExitCode);
        Assert.Equal(0, promoteExitCode);

        var entries = PlanState.Load(fixture.Options.PlanFile).For("Debug");
        Assert.All(entries, entry => Assert.False(entry.Hit));
        Assert.All(entries, entry => Assert.True(entry.ShouldGateCoverage));
        
        Assert.True(File.Exists(fixture.Combine("src/Lib/bin/Debug/net10.0/Lib.dll")));
        Assert.True(File.Exists(fixture.Combine("artifacts/coverage-report/Debug/Cobertura.xml")));

        var mainStore = new LocalVolumeArtifactStore(fixture.Options.MainStoreRoot);
        foreach (var entry in entries)
        {
            Assert.True(mainStore.TryGetHash(new ProjectId(entry.ProjectPath), "Debug", out var stored));
            Assert.Equal(entry.FullHash, stored.FullHash);
        }
    }

    [Fact]
    public void Pipeline_ShouldMaterialiseAndReuse_WhenASecondCheckoutFindsMainWarm()
    {
        // Arrange
        using var fixture = FixtureRepository.Create();
        Assert.Equal(0, PlanCommand.Run(fixture.Options, _output.Pipeline));
        Assert.Equal(0, BuildCommand.Run(fixture.Options, "Debug", _output.Pipeline));
        Assert.Equal(0, TestCommand.Run(fixture.Options, "Debug", _output.Pipeline));
        Assert.Equal(0, PromoteCommand.Run(fixture.Options, _output.Pipeline));

        var firstRunReports = CoverageReportNames(fixture);
        Assert.NotEmpty(firstRunReports);

        fixture.ResetWorkspace();
        fixture.With(prStoreRoot: fixture.Combine("artifacts/ci-pr-2"));

        // Act
        var planExitCode = PlanCommand.Run(fixture.Options, _output.Pipeline);
        var buildExitCode = BuildCommand.Run(fixture.Options, "Debug", _output.Pipeline);
        var testExitCode = TestCommand.Run(fixture.Options, "Debug", _output.Pipeline);
        var reported = _output.ToString();

        // Assert
        Assert.Equal(0, planExitCode);
        Assert.Equal(0, buildExitCode);
        Assert.Equal(0, testExitCode);

        var entries = PlanState.Load(fixture.Options.PlanFile).For("Debug");
        Assert.All(entries, entry => Assert.True(entry.Hit));

        Assert.True(File.Exists(fixture.Combine("src/Lib/bin/Debug/net10.0/Lib.dll")));
        Assert.Contains("materialised 2 cache hit(s) from main", reported, StringComparison.Ordinal);

        Assert.Contains("(reused)", reported, StringComparison.Ordinal);
        Assert.Equal(firstRunReports, CoverageReportNames(fixture));

        Assert.Contains("No selected project is under the coverage gate", reported, StringComparison.Ordinal);
        Assert.All(entries, entry => Assert.False(entry.ShouldGateCoverage));
        Assert.False(File.Exists(fixture.Combine("artifacts/coverage-report/Debug/Cobertura.xml")));
    }

    private static List<string> CoverageReportNames(FixtureRepository fixture) =>
        Directory.EnumerateFiles(fixture.Combine("artifacts/coverage/Debug"), "*cobertura*.xml")
            .Select(Path.GetFileName)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToList()!;

    [Fact]
    public void Plan_ShouldMissTheEditedProjectAndEverythingAboveIt_WhenASourceFileChanges()
    {
        // Arrange
        using var fixture = FixtureRepository.Create();
        Assert.Equal(0, PlanCommand.Run(fixture.Options, _output.Pipeline));
        Assert.Equal(0, BuildCommand.Run(fixture.Options, "Debug", _output.Pipeline));
        Assert.Equal(0, PromoteCommand.Run(fixture.Options, _output.Pipeline));

        fixture.Write("src/Lib/Calculator.cs", """
            namespace Lib;

            public static class Calculator
            {
                public static int Add(int left, int right) => left + right + 0;

                public static string Describe(int value)
                {
                    if (value > 0)
                    {
                        return "positive";
                    }

                    return "not positive";
                }
            }
            """);
        fixture.With(prStoreRoot: fixture.Combine("artifacts/ci-pr-2"));

        // Act
        var exitCode = PlanCommand.Run(fixture.Options, _output.Pipeline);

        // Assert
        Assert.Equal(0, exitCode);
        var entries = PlanState.Load(fixture.Options.PlanFile).For("Debug");

        var library = entries.Single(entry => entry.ProjectPath == FixtureRepository.LibraryProject);
        var tests = entries.Single(entry => entry.ProjectPath == FixtureRepository.TestProject);
        Assert.False(library.Hit);
        Assert.True(library.ShouldGateCoverage);
        Assert.False(tests.Hit);
        Assert.False(tests.ShouldGateCoverage);
    }

    [Fact]
    public void Plan_ShouldForceEveryProjectToAMiss_WhenTheRunIsCacheless()
    {
        // Arrange
        using var fixture = FixtureRepository.Create();
        Assert.Equal(0, PlanCommand.Run(fixture.Options, _output.Pipeline));
        Assert.Equal(0, BuildCommand.Run(fixture.Options, "Debug", _output.Pipeline));
        Assert.Equal(0, PromoteCommand.Run(fixture.Options, _output.Pipeline));
        fixture.With(cacheless: true, prStoreRoot: fixture.Combine("artifacts/ci-pr-2"));

        // Act
        var exitCode = PlanCommand.Run(fixture.Options, _output.Pipeline);

        // Assert
        Assert.Equal(0, exitCode);
        var entries = PlanState.Load(fixture.Options.PlanFile).For("Debug");
        Assert.NotEmpty(entries);
        Assert.All(entries, entry => Assert.True(entry.Forced));
        Assert.All(entries, entry => Assert.False(entry.Hit));
        Assert.All(entries, entry => Assert.True(entry.ShouldGateCoverage));
    }

    [Fact]
    public void Cleanup_ShouldDropTheRunVolumeAndLeaveMain_WhenThePipelineHasFinished()
    {
        // Arrange
        using var fixture = FixtureRepository.Create();
        Assert.Equal(0, PlanCommand.Run(fixture.Options, _output.Pipeline));
        Assert.Equal(0, BuildCommand.Run(fixture.Options, "Debug", _output.Pipeline));
        Assert.Equal(0, PromoteCommand.Run(fixture.Options, _output.Pipeline));

        // Act
        var exitCode = CleanupCommand.Run(fixture.Options, keepArtifacts: false, _output.Pipeline);

        // Assert
        Assert.Equal(0, exitCode);
        Assert.False(Directory.Exists(fixture.Options.PrStoreRoot));
        Assert.True(new LocalVolumeArtifactStore(fixture.Options.MainStoreRoot)
            .Contains(new ProjectId(FixtureRepository.LibraryProject), "Debug"));
    }
}
