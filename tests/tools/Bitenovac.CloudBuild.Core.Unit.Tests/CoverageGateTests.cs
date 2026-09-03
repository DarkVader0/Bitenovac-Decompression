using Bitenovac.CloudBuild.Core.Coverage;
using Bitenovac.CloudBuild.Core.Graph;

namespace Bitenovac.CloudBuild.Core.Unit.Tests;

public sealed class CoverageGateTests
{
    private const int Precision = 2;

    [Fact]
    public void Evaluate_ShouldThrow_WhenGatedIsNull()
    {
        // Arrange
        var measured = new Dictionary<ProjectId, CoverageMeasurement>();

        // Act
        var act = () => CoverageGate.Evaluate(null!, measured);

        // Assert
        Assert.Throws<ArgumentNullException>(act);
    }

    [Fact]
    public void Evaluate_ShouldThrow_WhenMeasuredIsNull()
    {
        // Arrange
        var gated = new Dictionary<ProjectId, CoveragePolicy>();

        // Act
        var act = () => CoverageGate.Evaluate(gated, null!);

        // Assert
        Assert.Throws<ArgumentNullException>(act);
    }

    [Fact]
    public void Evaluate_ShouldReturnNoResults_WhenNothingIsGated()
    {
        // Arrange
        var gated = new Dictionary<ProjectId, CoveragePolicy>();
        var measured = new Dictionary<ProjectId, CoverageMeasurement>();

        // Act
        var results = CoverageGate.Evaluate(gated, measured);

        // Assert
        Assert.Empty(results);
    }

    [Fact]
    public void Evaluate_ShouldFail_WhenGatedProjectHasNoMeasurement()
    {
        // Arrange
        var gated = new Dictionary<ProjectId, CoveragePolicy> { [TestFactory.Id("Core")] = TestFactory.Policy() };
        var measured = new Dictionary<ProjectId, CoverageMeasurement>();

        // Act
        var results = CoverageGate.Evaluate(gated, measured);

        // Assert
        var result = Assert.Single(results);
        Assert.False(result.Passed);
        Assert.Contains("no test exercised it", result.Reason);
    }

    [Fact]
    public void Evaluate_ShouldPass_WhenMeasuredCoverageExactlyMeetsThePolicy()
    {
        // Arrange
        var gated = new Dictionary<ProjectId, CoveragePolicy> { [TestFactory.Id("Core")] = TestFactory.Policy(90, 80) };
        var measured = new Dictionary<ProjectId, CoverageMeasurement> { [TestFactory.Id("Core")] = TestFactory.Measurement(90, 80) };

        // Act
        var results = CoverageGate.Evaluate(gated, measured);

        // Assert
        Assert.True(Assert.Single(results).Passed);
    }

    [Fact]
    public void Evaluate_ShouldPass_WhenMeasuredCoverageExceedsThePolicy()
    {
        // Arrange
        var gated = new Dictionary<ProjectId, CoveragePolicy> { [TestFactory.Id("Core")] = TestFactory.Policy(90, 80) };
        var measured = new Dictionary<ProjectId, CoverageMeasurement> { [TestFactory.Id("Core")] = TestFactory.Measurement(100, 100) };

        // Act
        var results = CoverageGate.Evaluate(gated, measured);

        // Assert
        Assert.True(Assert.Single(results).Passed);
    }

    [Fact]
    public void Evaluate_ShouldFail_WhenLineCoverageIsBelowThePolicy()
    {
        // Arrange
        var gated = new Dictionary<ProjectId, CoveragePolicy> { [TestFactory.Id("Core")] = TestFactory.Policy(90, 80) };
        var measured = new Dictionary<ProjectId, CoverageMeasurement> { [TestFactory.Id("Core")] = TestFactory.Measurement(89.99, 100) };

        // Act
        var results = CoverageGate.Evaluate(gated, measured);

        // Assert
        var result = Assert.Single(results);
        Assert.False(result.Passed);
        Assert.Contains("below its coverage gate", result.Reason);
    }

    [Fact]
    public void Evaluate_ShouldFail_WhenBranchCoverageIsBelowThePolicy()
    {
        // Arrange
        var gated = new Dictionary<ProjectId, CoveragePolicy> { [TestFactory.Id("Core")] = TestFactory.Policy(90, 80) };
        var measured = new Dictionary<ProjectId, CoverageMeasurement> { [TestFactory.Id("Core")] = TestFactory.Measurement(100, 79.99) };

        // Act
        var results = CoverageGate.Evaluate(gated, measured);

        // Assert
        Assert.False(Assert.Single(results).Passed);
    }

    [Fact]
    public void Evaluate_ShouldReportEveryGatedProject_OrderedByPath()
    {
        // Arrange
        var gated = new Dictionary<ProjectId, CoveragePolicy>
        {
            [TestFactory.Id("Zeta")] = TestFactory.Policy(),
            [TestFactory.Id("Alpha")] = TestFactory.Policy(),
        };
        var measured = new Dictionary<ProjectId, CoverageMeasurement>
        {
            [TestFactory.Id("Zeta")] = TestFactory.Measurement(100, 100),
            [TestFactory.Id("Alpha")] = TestFactory.Measurement(100, 100),
        };

        // Act
        var results = CoverageGate.Evaluate(gated, measured);

        // Assert
        Assert.Equal([TestFactory.Id("Alpha"), TestFactory.Id("Zeta")], results.Select(r => r.Project));
    }

    [Theory]
    [InlineData(90.0, 80.0)]
    [InlineData(100.0, 100.0)]
    public void Evaluate_ShouldRoundTripTheMeasuredFigures_WhenProjectPasses(double line, double branch)
    {
        // Arrange
        var gated = new Dictionary<ProjectId, CoveragePolicy> { [TestFactory.Id("Core")] = TestFactory.Policy(line, branch) };
        var measured = new Dictionary<ProjectId, CoverageMeasurement> { [TestFactory.Id("Core")] = TestFactory.Measurement(line, branch) };

        // Act
        var result = Assert.Single(CoverageGate.Evaluate(gated, measured));

        // Assert
        Assert.Equal(line, measured[TestFactory.Id("Core")].LinePercent, Precision);
        Assert.Equal(branch, measured[TestFactory.Id("Core")].BranchPercent, Precision);
        Assert.True(result.Passed);
    }
}
