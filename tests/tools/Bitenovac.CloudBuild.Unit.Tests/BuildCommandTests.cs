using Bitenovac.CloudBuild.Commands;

namespace Bitenovac.CloudBuild.Unit.Tests;

public sealed class BuildCommandTests
{
    [Fact]
    public void Run_ShouldThrow_WhenNoPlanWasWritten()
    {
        // Arrange
        using var directory = TestFactory.Directory();
        var options = TestFactory.Options(directory.Path, directory.Combine("main"), directory.Combine("pr"));

        // Act
        Action act = () => BuildCommand.Run(options, "Debug", TestFactory.Silence());

        // Assert
        Assert.Throws<InvalidOperationException>(act);
    }

    [Fact]
    public void Run_ShouldSucceedWithoutInvokingMsBuild_WhenTheConfigurationSelectedNothing()
    {
        // Arrange
        using var directory = TestFactory.Directory();
        var options = TestFactory.Options(directory.Path, directory.Combine("main"), directory.Combine("pr"));
        TestFactory.Plan("Debug", TestFactory.Entry("src/A/A.csproj")).Save(options.PlanFile);

        // Act
        var exitCode = BuildCommand.Run(options, "Release", TestFactory.Silence());

        // Assert
        Assert.Equal(0, exitCode);
        Assert.False(File.Exists(options.SyntheticProjectPath("build-Release")));
    }
}
