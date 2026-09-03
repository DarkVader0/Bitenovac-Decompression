using Bitenovac.DecompressionAlgorithms.BuhlmannZhl16c;
using Bitenovac.DecompressionAlgorithms.Core;
using Bitenovac.DecompressionAlgorithms.Core.Planning;

namespace Bitenovac.DecompressionAlgorithms.Zhl16c.Integration.Tests;

public sealed class DivePlannerZhl16cTests
{
    private const int Precision = 6;

    private static DecoPlan Plan(DivePlanRequest request,
        double gradientFactorLow = 0.3,
        double gradientFactorHigh = 0.7) =>
        new DivePlanner(new BuhlmannZhl16cAlgorithm(gradientFactorLow, gradientFactorHigh)).CreatePlan(request);

    [Fact]
    public void CreatePlan_ShouldBeValidAndEndAtSurface_WhenDiveNeedsNoDecompression()
    {
        // Arrange
        var request = new DivePlanRequest(TestFactory.CreateProfile((18, 15)), [TestFactory.CreateCylinder()],
            TestFactory.CreateSettings());

        // Act
        var plan = Plan(request, 0.85, 0.85);

        // Assert
        Assert.True(plan.IsValid);
        Assert.Equal(0.0, plan.ExpandedSegments[^1].Depth.InMeter, Precision);
        Assert.True(plan.TotalRuntime > TimeSpan.FromMinutes(15));
    }

    [Fact]
    public void CreatePlan_ShouldGenerateGridAlignedStops_WhenDiveNeedsDecompression()
    {
        // Arrange
        var nitrox50 = GasMixture.FromPercent(50, 0);
        var request = new DivePlanRequest(TestFactory.CreateProfile((45, 25)),
            [TestFactory.CreateCylinder(), TestFactory.CreateCylinder(nitrox50)],
            TestFactory.CreateSettings());

        // Act
        var plan = Plan(request);

        // Assert
        Assert.True(plan.IsValid);
        var stops = 0;
        for (var i = 0; i < plan.ExpandedSegments.Count; i++)
        {
            if (plan.ExpandedSegments[i].Kind != SegmentKind.Stop)
            {
                continue;
            }

            stops++;
            Assert.Equal(0.0, plan.ExpandedSegments[i].Depth.InMeter % 3.0, Precision);
        }

        Assert.True(stops > 0);
        Assert.Equal(0.0, plan.ExpandedSegments[^1].Depth.InMeter, Precision);
    }

    [Fact]
    public void CreatePlan_ShouldRecordViolation_WhenProfileAscendsAboveCeiling()
    {
        // Arrange
        var request = new DivePlanRequest(TestFactory.CreateProfile((40, 25), (3, 5)),
            [TestFactory.CreateCylinder()],
            TestFactory.CreateSettings());

        // Act
        var plan = Plan(request);

        // Assert
        Assert.False(plan.IsValid);
        var violation = Assert.Single(plan.Violations);
        Assert.Equal(3.0, violation.ToDepth.InMeter, Precision);
        Assert.True(violation.Ceiling.InMeter > 3.0);
    }

    [Fact]
    public void CreatePlan_ShouldLengthenDecompression_WhenDiveIsRepetitive()
    {
        // Arrange
        var nitrox50 = GasMixture.FromPercent(50, 0);
        var cylinders = new[] { TestFactory.CreateCylinder(), TestFactory.CreateCylinder(nitrox50) };
        var profile = TestFactory.CreateProfile((40, 20));
        var settings = TestFactory.CreateSettings();
        var priorDive = new PriorDive(profile, cylinders, settings, TimeSpan.FromMinutes(45), GasMixture.Air);

        var firstRequest = new DivePlanRequest(profile, cylinders, settings);
        var repetitiveRequest = new DivePlanRequest(profile, cylinders, settings, [priorDive]);

        // Act
        var firstPlan = Plan(firstRequest);
        var repetitivePlan = Plan(repetitiveRequest);

        // Assert
        Assert.True(TotalStopTime(repetitivePlan) > TotalStopTime(firstPlan));
    }

    [Fact]
    public void CreatePlan_ShouldBeDeterministic_WhenRunTwiceFromIdenticalInputs()
    {
        // Arrange
        var nitrox50 = GasMixture.FromPercent(50, 0);

        DecoPlan Run() => Plan(new DivePlanRequest(TestFactory.CreateProfile((45, 25)),
            [TestFactory.CreateCylinder(), TestFactory.CreateCylinder(nitrox50)],
            TestFactory.CreateSettings()));

        // Act
        var first = Run();
        var second = Run();

        // Assert
        Assert.Equal(first.TotalRuntime, second.TotalRuntime);
        Assert.Equal(first.ExpandedSegments.Count, second.ExpandedSegments.Count);
        for (var i = 0; i < first.ExpandedSegments.Count; i++)
        {
            Assert.Equal(first.ExpandedSegments[i].Depth.InMillimeter,
                second.ExpandedSegments[i].Depth.InMillimeter);
            Assert.Equal(first.ExpandedSegments[i].Duration, second.ExpandedSegments[i].Duration);
            Assert.Equal(first.ExpandedSegments[i].Gas, second.ExpandedSegments[i].Gas);
            Assert.Equal(first.ExpandedSegments[i].Kind, second.ExpandedSegments[i].Kind);
        }
    }

    private static TimeSpan TotalStopTime(DecoPlan plan)
    {
        var total = TimeSpan.Zero;
        for (var i = 0; i < plan.ExpandedSegments.Count; i++)
        {
            if (plan.ExpandedSegments[i].Kind == SegmentKind.Stop)
            {
                total += plan.ExpandedSegments[i].Duration;
            }
        }

        return total;
    }
}