namespace Bitenovac.CloudBuild.Unit.Tests;

[Collection(EnvironmentVariables.Collection)]
public sealed class PipelineOptionsTests : IDisposable
{
    private readonly EnvironmentVariables _environment = new();

    public void Dispose() => _environment.Dispose();

    [Fact]
    public void FromEnvironment_ShouldUseTheCurrentDirectory_WhenNoRepositoryRootIsSet()
    {
        // Arrange
        _environment.Clear();

        // Act
        var options = PipelineOptions.FromEnvironment();

        // Assert
        Assert.Equal(Directory.GetCurrentDirectory(), options.RepositoryRoot);
    }

    [Fact]
    public void FromEnvironment_ShouldUseTheDeclaredRoot_WhenRepositoryRootIsSet()
    {
        // Arrange
        _environment.Clear();
        _environment.Set("CLOUDBUILD_REPO_ROOT", @"C:\somewhere\repo");

        // Act
        var options = PipelineOptions.FromEnvironment();

        // Assert
        Assert.Equal(@"C:\somewhere\repo", options.RepositoryRoot);
    }

    [Fact]
    public void FromEnvironment_ShouldDeriveBothStoresFromTheRepositoryRoot_WhenNeitherIsSet()
    {
        // Arrange
        _environment.Clear();
        _environment.Set("CLOUDBUILD_REPO_ROOT", "repo");

        // Act
        var options = PipelineOptions.FromEnvironment();

        // Assert
        Assert.Equal(Path.Combine("repo", "artifacts", "ci-main"), options.MainStoreRoot);
        Assert.Equal(Path.Combine("repo", "artifacts", "ci-pr"), options.PrStoreRoot);
    }

    [Fact]
    public void FromEnvironment_ShouldUseTheDeclaredStores_WhenBothAreSet()
    {
        // Arrange
        _environment.Clear();
        _environment.Set("CLOUDBUILD_MAIN_STORE", "/volumes/main");
        _environment.Set("CLOUDBUILD_PR_STORE", "/volumes/pr");

        // Act
        var options = PipelineOptions.FromEnvironment();

        // Assert
        Assert.Equal("/volumes/main", options.MainStoreRoot);
        Assert.Equal("/volumes/pr", options.PrStoreRoot);
    }

    [Theory]
    [InlineData("true")]
    [InlineData("TRUE")]
    [InlineData("1")]
    public void FromEnvironment_ShouldReportCacheless_WhenTheVariableSaysSo(string value)
    {
        // Arrange
        _environment.Clear();
        _environment.Set("CLOUDBUILD_CACHELESS", value);

        // Act
        var options = PipelineOptions.FromEnvironment();

        // Assert
        Assert.True(options.Cacheless);
    }

    [Theory]
    [InlineData("false")]
    [InlineData("0")]
    [InlineData("yes")]
    public void FromEnvironment_ShouldNotReportCacheless_WhenTheVariableSaysAnythingElse(string value)
    {
        // Arrange
        _environment.Clear();
        _environment.Set("CLOUDBUILD_CACHELESS", value);

        // Act
        var options = PipelineOptions.FromEnvironment();

        // Assert
        Assert.False(options.Cacheless);
    }

    [Fact]
    public void FromEnvironment_ShouldNotReportCacheless_WhenTheVariableIsUnset()
    {
        // Arrange
        _environment.Clear();

        // Act
        var options = PipelineOptions.FromEnvironment();

        // Assert
        Assert.False(options.Cacheless);
    }

    [Fact]
    public void FromEnvironment_ShouldDisableTheHtmlReport_WhenCoverageHtmlIsZero()
    {
        // Arrange
        _environment.Clear();
        _environment.Set("CLOUDBUILD_COVERAGE_HTML", "0");

        // Act
        var options = PipelineOptions.FromEnvironment();

        // Assert
        Assert.False(options.CoverageHtml);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("1")]
    [InlineData("anything")]
    public void FromEnvironment_ShouldEnableTheHtmlReport_WhenCoverageHtmlIsAnythingButZero(string? value)
    {
        // Arrange
        _environment.Clear();
        _environment.Set("CLOUDBUILD_COVERAGE_HTML", value);

        // Act
        var options = PipelineOptions.FromEnvironment();

        // Assert
        Assert.True(options.CoverageHtml);
    }

    [Fact]
    public void PlanFile_ShouldSitInThePrStore()
    {
        // Arrange
        var options = TestFactory.Options("repo", "main", "pr");

        // Act
        var planFile = options.PlanFile;

        // Assert
        Assert.Equal(Path.Combine("pr", "plan.json"), planFile);
    }

    [Fact]
    public void SyntheticProjectPath_ShouldSitInThePrStore_NamedAfterTheStage()
    {
        // Arrange
        var options = TestFactory.Options("repo", "main", "pr");

        // Act
        var path = options.SyntheticProjectPath("restore");

        // Assert
        Assert.Equal(Path.Combine("pr", "build-restore.proj"), path);
    }

    [Fact]
    public void CoverageDirectory_ShouldSitUnderTheCheckout_NotThePrStore()
    {
        // Arrange
        var options = TestFactory.Options("repo", "main", "pr");

        // Act
        var directory = options.CoverageDirectory("Debug");

        // Assert
        Assert.Equal(Path.Combine("repo", "artifacts", "coverage", "Debug"), directory);
    }

    [Fact]
    public void CoverageReportDirectory_ShouldSitUnderTheCheckout_NotThePrStore()
    {
        // Arrange
        var options = TestFactory.Options("repo", "main", "pr");

        // Act
        var directory = options.CoverageReportDirectory("Release");

        // Assert
        Assert.Equal(Path.Combine("repo", "artifacts", "coverage-report", "Release"), directory);
    }

    [Fact]
    public void Configurations_ShouldBeDebugAndRelease()
    {
        // Arrange

        // Act
        var configurations = PipelineOptions.Configurations;

        // Assert
        Assert.Equal(["Debug", "Release"], configurations);
    }
}
