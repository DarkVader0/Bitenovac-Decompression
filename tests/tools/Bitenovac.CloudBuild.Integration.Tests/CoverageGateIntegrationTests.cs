using Bitenovac.CloudBuild.Commands;

namespace Bitenovac.CloudBuild.Integration.Tests;

public sealed class CoverageGateIntegrationTests
{
    private readonly CapturedOutput _output = new();

    [Fact]
    public void Test_ShouldPassTheGate_WhenEveryGatedProjectMeetsItsPolicy()
    {
        // Arrange
        using var fixture = FixtureRepository.Create(TestSuite.FullyCovering);
        Assert.Equal(0, PlanCommand.Run(fixture.Options, _output.Pipeline));
        Assert.Equal(0, BuildCommand.Run(fixture.Options, "Debug", _output.Pipeline));

        // Act
        var exitCode = TestCommand.Run(fixture.Options, "Debug", _output.Pipeline);
        var reported = _output.ToString();

        // Assert
        Assert.Equal(0, exitCode);
        Assert.Contains("Every gated project meets its coverage policy", reported, StringComparison.Ordinal);
    }

    [Fact]
    public void Test_ShouldFailTheGate_WhenAGatedProjectLeavesABranchUncovered()
    {
        // Arrange
        using var fixture = FixtureRepository.Create(TestSuite.PartiallyCovering);
        Assert.Equal(0, PlanCommand.Run(fixture.Options, _output.Pipeline));
        Assert.Equal(0, BuildCommand.Run(fixture.Options, "Debug", _output.Pipeline));

        // Act
        var exitCode = TestCommand.Run(fixture.Options, "Debug", _output.Pipeline);
        var reported = _output.ToString();

        // Assert
        Assert.Equal(1, exitCode);
        Assert.Contains("All selected tests passed", reported, StringComparison.Ordinal);
        Assert.Contains($"FAIL {FixtureRepository.LibraryProject} is below its coverage gate", reported, StringComparison.Ordinal);
    }

    [Fact]
    public void Test_ShouldNotGateOnRelease_WhenTheConfigurationIsNotDebug()
    {
        // Arrange
        using var fixture = FixtureRepository.Create(TestSuite.PartiallyCovering);
        Assert.Equal(0, PlanCommand.Run(fixture.Options, _output.Pipeline));
        Assert.Equal(0, BuildCommand.Run(fixture.Options, "Release", _output.Pipeline));

        // Act
        var exitCode = TestCommand.Run(fixture.Options, "Release", _output.Pipeline);

        // Assert
        Assert.Equal(0, exitCode);
        Assert.False(Directory.Exists(fixture.Combine("artifacts/coverage-report/Release")));
    }
}
