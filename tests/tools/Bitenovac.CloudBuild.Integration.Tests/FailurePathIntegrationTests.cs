using Bitenovac.CloudBuild.Commands;
using Bitenovac.CloudBuild.MsBuild;
using Bitenovac.CloudBuild.Testing;

namespace Bitenovac.CloudBuild.Integration.Tests;

/// <summary>
/// The ways a real run goes wrong: code that does not compile, tests that fail, a suite with
/// nothing in it, and a report that cannot be merged.
/// </summary>
public sealed class FailurePathIntegrationTests
{
    private readonly CapturedOutput _output = new();

    [Fact]
    public void Build_ShouldFail_WhenTheSourceDoesNotCompile()
    {
        // Arrange
        using var fixture = FixtureRepository.Create();
        Assert.Equal(0, PlanCommand.Run(fixture.Options, _output.Pipeline));
        fixture.Write("src/Lib/Calculator.cs", "namespace Lib; public static class Calculator { this is not C# }");

        // Act
        var exitCode = BuildCommand.Run(fixture.Options, "Debug", _output.Pipeline);

        // Assert
        Assert.NotEqual(0, exitCode);
    }

    [Fact]
    public void Build_ShouldSkipWhatIsAlreadyThere_WhenTheWorkspaceStillHoldsTheHash()
    {
        // Arrange
        using var fixture = FixtureRepository.Create();
        Assert.Equal(0, PlanCommand.Run(fixture.Options, _output.Pipeline));
        Assert.Equal(0, BuildCommand.Run(fixture.Options, "Debug", _output.Pipeline));
        Assert.Equal(0, PromoteCommand.Run(fixture.Options, _output.Pipeline));

        fixture.With(prStoreRoot: fixture.Combine("artifacts/ci-pr-2"));
        Assert.Equal(0, PlanCommand.Run(fixture.Options, _output.Pipeline));

        // Act
        var exitCode = BuildCommand.Run(fixture.Options, "Debug", _output.Pipeline);
        var reported = _output.ToString();

        // Assert
        Assert.Equal(0, exitCode);
        Assert.Contains("2 already present", reported, StringComparison.Ordinal);
    }

    [Fact]
    public void Test_ShouldFail_WhenATestProjectHasAFailingTest()
    {
        // Arrange
        using var fixture = FixtureRepository.Create(TestSuite.Failing);
        Assert.Equal(0, PlanCommand.Run(fixture.Options, _output.Pipeline));
        Assert.Equal(0, BuildCommand.Run(fixture.Options, "Debug", _output.Pipeline));

        // Act
        var exitCode = TestCommand.Run(fixture.Options, "Debug", _output.Pipeline);

        // Assert
        Assert.Equal(1, exitCode);
    }

    [Fact]
    public void Test_ShouldWarnRatherThanFail_WhenATestProjectRunsNoTests()
    {
        // Arrange
        using var fixture = FixtureRepository.Create(TestSuite.NoTests);
        Assert.Equal(0, PlanCommand.Run(fixture.Options, _output.Pipeline));
        Assert.Equal(0, BuildCommand.Run(fixture.Options, "Debug", _output.Pipeline));

        // Act
        var exitCode = TestCommand.Run(fixture.Options, "Debug", _output.Pipeline);
        var reported = _output.ToString();

        // Assert
        Assert.Contains("warning: test projects that ran no tests: Lib.Tests", reported, StringComparison.Ordinal);
        Assert.Contains("All selected tests passed", reported, StringComparison.Ordinal);
        Assert.Contains($"FAIL {FixtureRepository.LibraryProject} is below its coverage gate", reported, StringComparison.Ordinal);
        Assert.Equal(1, exitCode);
    }

    [Fact]
    public void Merge_ShouldThrow_WhenThereIsNoReportToMerge()
    {
        // Arrange
        using var fixture = FixtureRepository.Create();
        var empty = fixture.Combine("artifacts/coverage/Debug");
        Directory.CreateDirectory(empty);

        // Act
        var act = () => CoverageReportGenerator.Merge(
            fixture.Root, empty, fixture.Combine("artifacts/coverage-report/Debug"), includeHtml: false);

        // Assert
        Assert.Throws<InvalidOperationException>(act);
    }

    [Fact]
    public void Test_ShouldWriteTheHtmlReport_WhenCoverageHtmlIsOn()
    {
        // Arrange
        using var fixture = FixtureRepository.Create();
        fixture.With(coverageHtml: true);
        Assert.Equal(0, PlanCommand.Run(fixture.Options, _output.Pipeline));
        Assert.Equal(0, BuildCommand.Run(fixture.Options, "Debug", _output.Pipeline));

        // Act
        var exitCode = TestCommand.Run(fixture.Options, "Debug", _output.Pipeline);

        // Assert
        Assert.Equal(0, exitCode);
        Assert.True(File.Exists(fixture.Combine("artifacts/coverage-report/Debug/index.html")));
    }

    [Fact]
    public void Restore_ShouldSucceed_WhenTheParallelismIsPinnedByTheEnvironment()
    {
        // Arrange
        using var fixture = FixtureRepository.Create();
        var previous = Environment.GetEnvironmentVariable("CLOUDBUILD_MAX_CPU");
        Environment.SetEnvironmentVariable("CLOUDBUILD_MAX_CPU", "1");

        // Act
        int exitCode;
        try
        {
            exitCode = MsBuildRunner.Restore(
                fixture.Root,
                [fixture.Combine(FixtureRepository.LibraryProject)],
                "Debug",
                fixture.Options.SyntheticProjectPath("restore"));
        }
        finally
        {
            Environment.SetEnvironmentVariable("CLOUDBUILD_MAX_CPU", previous);
        }

        // Assert
        Assert.Equal(0, exitCode);
        Assert.True(File.Exists(fixture.Combine("src/Lib/obj/project.assets.json")));
    }
}
