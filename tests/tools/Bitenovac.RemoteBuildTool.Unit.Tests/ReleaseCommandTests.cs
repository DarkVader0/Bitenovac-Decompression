using Bitenovac.RemoteBuildTool.Commands;

namespace Bitenovac.RemoteBuildTool.Unit.Tests;

public sealed class ReleaseCommandTests
{
    [Fact]
    public void Run_ShouldThrow_WhenNoPlanWasWritten()
    {
        // Arrange
        using var directory = TestFactory.Directory();
        var options = TestFactory.Options(directory.Path, directory.Combine("main"), directory.Combine("pr"));

        // Act
        Action act = () => ReleaseCommand.Run(options, TestFactory.Silence());

        // Assert
        Assert.Throws<InvalidOperationException>(act);
    }

    [Fact]
    public void Run_ShouldFailWithoutCreatingADrop_WhenThePlanSelectedNothingForRelease()
    {
        // Arrange
        using var directory = TestFactory.Directory();
        var options = TestFactory.Options(directory.Path, directory.Combine("main"), directory.Combine("pr"));
        TestFactory.Plan("Debug", TestFactory.Entry("src/A/A.csproj")).Save(options.PlanFile);

        // Act
        var exitCode = ReleaseCommand.Run(options, TestFactory.Silence());

        // Assert
        Assert.Equal(1, exitCode);
        Assert.False(Directory.Exists(options.DropRoot));
    }
}
