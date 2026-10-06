using Bitenovac.CloudBuild.Processes;

namespace Bitenovac.CloudBuild.Unit.Tests;

public sealed class ProcessRunnerTests
{
    [Fact]
    public void Run_ShouldReportZero_WhenTheProcessSucceeds()
    {
        // Arrange
        using var directory = TestFactory.Directory();

        // Act
        var exitCode = ProcessRunner.Run("dotnet", ["--version"], directory.Path);

        // Assert
        Assert.Equal(0, exitCode);
    }

    [Fact]
    public void Run_ShouldReportTheProcessExitCode_WhenItFails()
    {
        // Arrange
        using var directory = TestFactory.Directory();

        // Act
        var exitCode = ProcessRunner.Run("dotnet", ["exec", directory.Combine("not-an-assembly.dll")], directory.Path);

        // Assert
        Assert.NotEqual(0, exitCode);
    }

    [Fact]
    public void Run_ShouldStartTheProcess_WhenAnEnvironmentIsSupplied()
    {
        // Arrange
        using var directory = TestFactory.Directory();
        var environment = new Dictionary<string, string> { ["CLOUDBUILD_TEST_VARIABLE"] = "set" };

        // Act
        var exitCode = ProcessRunner.Run("dotnet", ["--version"], directory.Path, environment);

        // Assert
        Assert.Equal(0, exitCode);
        Assert.Null(Environment.GetEnvironmentVariable("CLOUDBUILD_TEST_VARIABLE"));
    }

    [Fact]
    public void RunCaptured_ShouldReturnTheOutputInsteadOfPrintingIt_WhenTheProcessSucceeds()
    {
        // Arrange
        using var directory = TestFactory.Directory();

        // Act
        var (exitCode, output) = ProcessRunner.RunCaptured("dotnet", ["--version"], directory.Path);

        // Assert
        Assert.Equal(0, exitCode);
        Assert.Matches(@"^\d+\.\d+\.\d+", output);
    }

    [Fact]
    public void RunCaptured_ShouldCaptureTheErrorStream_WhenTheProcessFails()
    {
        // Arrange
        using var directory = TestFactory.Directory();

        // Act
        var (exitCode, output) = ProcessRunner.RunCaptured("dotnet", ["exec", directory.Combine("not-an-assembly.dll")], directory.Path);

        // Assert
        Assert.NotEqual(0, exitCode);
        Assert.Contains("not-an-assembly.dll", output, StringComparison.Ordinal);
    }

    [Fact]
    public void RunCaptured_ShouldThrow_WhenTheExecutableDoesNotExist()
    {
        // Arrange
        using var directory = TestFactory.Directory();

        // Act
        Action act = () => ProcessRunner.RunCaptured("bitenovac-no-such-executable", [], directory.Path);

        // Assert
        Assert.ThrowsAny<Exception>(act);
    }

    [Fact]
    public void Run_ShouldThrow_WhenTheExecutableDoesNotExist()
    {
        // Arrange
        using var directory = TestFactory.Directory();

        // Act
        Action act = () => ProcessRunner.Run("bitenovac-no-such-executable", [], directory.Path);

        // Assert
        Assert.ThrowsAny<Exception>(act);
    }
}
