using Bitenovac.CloudBuild.Core.Graph;

namespace Bitenovac.CloudBuild.Core.Unit.Tests;

public sealed class ProjectGraphTests
{
    [Fact]
    public void Constructor_ShouldThrow_WhenAnEdgeDeclaresAnUnknownProject()
    {
        // Arrange
        var projects = new[] { TestFactory.Id("A") };
        var edges = new[] { TestFactory.Edge("Unknown", "A") };

        // Act
        var act = () => new ProjectGraph(projects, edges);

        // Assert
        Assert.Throws<ArgumentException>(act);
    }

    [Fact]
    public void Constructor_ShouldThrow_WhenAnEdgeReferencesAnUnknownProject()
    {
        // Arrange
        var projects = new[] { TestFactory.Id("A") };
        var edges = new[] { TestFactory.Edge("A", "Unknown") };

        // Act
        var act = () => new ProjectGraph(projects, edges);

        // Assert
        Assert.Throws<ArgumentException>(act);
    }

    [Fact]
    public void GetDependencies_ShouldThrow_WhenProjectIsNotInTheGraph()
    {
        // Arrange
        var graph = TestFactory.Graph("A");

        // Act
        var act = () => graph.GetDependencies(TestFactory.Id("Unknown"));

        // Assert
        Assert.Throws<ArgumentException>(act);
    }

    [Fact]
    public void GetDependencies_ShouldReturnDirectReferencesOnly_WhenProjectHasTransitiveDependencies()
    {
        // Arrange
        var graph = TestFactory.Graph("A -> B", "B -> C", "C");

        // Act
        var dependencies = graph.GetDependencies(TestFactory.Id("A"));

        // Assert
        Assert.Equal([TestFactory.Id("B")], dependencies);
    }

    [Fact]
    public void GetBuildOrder_ShouldPlaceEveryDependencyBeforeItsDependent()
    {
        // Arrange
        var graph = TestFactory.Graph(
            "Benchmarks -> Buhlmann, Core, Units",
            "Buhlmann -> Core, Units",
            "Core -> Units",
            "Units");

        // Act
        var order = graph.GetBuildOrder();

        // Assert
        var index = order.Select((project, position) => (project, position))
            .ToDictionary(entry => entry.project, entry => entry.position);

        Assert.True(index[TestFactory.Id("Units")] < index[TestFactory.Id("Core")]);
        Assert.True(index[TestFactory.Id("Core")] < index[TestFactory.Id("Buhlmann")]);
        Assert.True(index[TestFactory.Id("Buhlmann")] < index[TestFactory.Id("Benchmarks")]);
    }

    [Fact]
    public void GetBuildOrder_ShouldIncludeEveryProjectExactlyOnce_WhenGraphIsDiamondShaped()
    {
        // Arrange
        var graph = TestFactory.Graph("A -> B, C", "B -> D", "C -> D", "D");

        // Act
        var order = graph.GetBuildOrder();

        // Assert
        Assert.Equal(4, order.Count);
        Assert.Equal(order.Distinct().Count(), order.Count);
    }

    [Fact]
    public void GetBuildOrder_ShouldThrowProjectGraphCycleException_WhenGraphHasACycle()
    {
        // Arrange
        var graph = TestFactory.Graph("A -> B", "B -> C", "C -> A");

        // Act
        var act = graph.GetBuildOrder;

        // Assert
        var exception = Assert.Throws<ProjectGraphCycleException>(act);
        Assert.Contains(TestFactory.Id("A"), exception.Cycle);
        Assert.Contains(TestFactory.Id("B"), exception.Cycle);
        Assert.Contains(TestFactory.Id("C"), exception.Cycle);
    }

    [Fact]
    public void GetBuildOrder_ShouldThrowProjectGraphCycleException_WhenProjectReferencesItself()
    {
        // Arrange
        var graph = TestFactory.Graph("A -> A");

        // Act
        var act = graph.GetBuildOrder;

        // Assert
        Assert.Throws<ProjectGraphCycleException>(act);
    }

    [Fact]
    public void Projects_ShouldBeOrderedByPath_WhenConstructedFromAnUnorderedSet()
    {
        // Arrange
        var graph = TestFactory.Graph("C", "A", "B");

        // Act
        var projects = graph.Projects;

        // Assert
        Assert.Equal([TestFactory.Id("A"), TestFactory.Id("B"), TestFactory.Id("C")], projects);
    }
}
