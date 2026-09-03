namespace Bitenovac.CloudBuild.Unit.Tests;

[Collection(EnvironmentVariables.Collection)]
public sealed class CommandLineTests : IDisposable
{
    private readonly EnvironmentVariables _environment = new();

    public void Dispose() => _environment.Dispose();

    [Fact]
    public void Run_ShouldFail_WhenNoArgumentsAreGiven()
    {
        // Arrange
        var arguments = Array.Empty<string>();

        // Act
        var exitCode = CommandLine.Run(arguments, TestFactory.Silence());

        // Assert
        Assert.Equal(1, exitCode);
    }

    [Theory]
    [InlineData("--help")]
    [InlineData("-h")]
    public void Run_ShouldSucceed_WhenHelpIsRequested(string argument)
    {
        // Arrange
        string[] arguments = [argument];

        // Act
        var exitCode = CommandLine.Run(arguments, TestFactory.Silence());

        // Assert
        Assert.Equal(0, exitCode);
    }

    [Fact]
    public void Run_ShouldFail_WhenTheCommandIsUnknown()
    {
        // Arrange
        string[] arguments = ["compile"];

        // Act
        var exitCode = CommandLine.Run(arguments, TestFactory.Silence());

        // Assert
        Assert.Equal(1, exitCode);
    }

    [Theory]
    [InlineData("build")]
    [InlineData("test")]
    public void Run_ShouldFail_WhenNoConfigurationIsGiven(string command)
    {
        // Arrange
        string[] arguments = [command];

        // Act
        var exitCode = CommandLine.Run(arguments, TestFactory.Silence());

        // Assert
        Assert.Equal(1, exitCode);
    }

    [Theory]
    [InlineData("build", "debug")]
    [InlineData("build", "Beta")]
    [InlineData("test", "release")]
    public void Run_ShouldFail_WhenTheConfigurationIsNeitherDebugNorRelease(string command, string configuration)
    {
        // Arrange
        string[] arguments = [command, configuration];

        // Act
        var exitCode = CommandLine.Run(arguments, TestFactory.Silence());

        // Assert
        Assert.Equal(1, exitCode);
    }

    [Theory]
    [InlineData("cleanup")]
    [InlineData("cleanup", "--keep-artifacts")]
    public void Run_ShouldDispatchToCleanup_WhenTheCommandNamesIt(params string[] arguments)
    {
        // Arrange
        using var repository = TestFactory.Directory();
        var prStore = repository.Combine("pr");
        Directory.CreateDirectory(prStore);
        PointAt(repository, prStore);

        // Act
        var exitCode = CommandLine.Run(arguments, TestFactory.Silence());

        // Assert
        Assert.Equal(0, exitCode);
        Assert.Equal(arguments.Contains("--keep-artifacts"), Directory.Exists(prStore));
    }

    [Theory]
    [InlineData("build", "Debug")]
    [InlineData("test", "Release")]
    [InlineData("promote")]
    public void Run_ShouldDispatchToTheStageCommand_WhenAPlanSelectsNothing(params string[] arguments)
    {
        // Arrange
        using var repository = TestFactory.Directory();
        var prStore = repository.Combine("pr");
        PointAt(repository, prStore);
        TestFactory.Plan("Debug").Save(Path.Combine(prStore, "plan.json"));

        // Act
        var exitCode = CommandLine.Run(arguments, TestFactory.Silence());

        // Assert
        Assert.Equal(0, exitCode);
    }

    [Fact]
    public void Run_ShouldDispatchToPlan_WhenTheRepositoryHoldsNoProjects()
    {
        // Arrange
        using var repository = TestFactory.Directory();
        PointAt(repository, repository.Combine("pr"));

        // Act
        var exitCode = CommandLine.Run(["plan"], TestFactory.Silence());

        // Assert
        Assert.Equal(1, exitCode);
    }

    [Fact]
    public void Run_ShouldReportOneLine_WhenACommandRaisesAnExpectedFailure()
    {
        // Arrange
        using var repository = TestFactory.Directory();
        PointAt(repository, repository.Combine("pr"));
        var output = new CapturedOutput();

        // Act
        var exitCode = CommandLine.Run(["promote"], output.Pipeline);

        // Assert
        Assert.Equal(1, exitCode);
        Assert.StartsWith("error: No plan found at", output.Error);
        Assert.DoesNotContain("InvalidOperationException", output.Error, StringComparison.Ordinal);
    }

    /// <summary>Steers the environment this run reads at every store away from the real repository.</summary>
    private void PointAt(TemporaryDirectory repository, string prStore)
    {
        _environment.Clear();
        _environment.Set("CLOUDBUILD_REPO_ROOT", repository.Path);
        _environment.Set("CLOUDBUILD_MAIN_STORE", repository.Combine("main"));
        _environment.Set("CLOUDBUILD_PR_STORE", prStore);
    }
}
