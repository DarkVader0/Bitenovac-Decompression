using Bitenovac.Ci.Core.Graph;
using Bitenovac.Ci.Core.Hashing;
using Bitenovac.Ci.Core.Planning;

namespace Bitenovac.Ci.Core.Unit.Tests;

public sealed class CachePlanBuilderTests
{
    [Fact]
    public void Build_ShouldThrow_WhenGraphIsNull()
    {
        // Arrange
        var inputs = TestFactory.OwnInputs();
        var stored = TestFactory.Stored();

        // Act
        var act = () => CachePlanBuilder.Build(null!, inputs, stored);

        // Assert
        Assert.Throws<ArgumentNullException>(act);
    }

    [Fact]
    public void Build_ShouldThrow_WhenOwnHashInputsIsNull()
    {
        // Arrange
        var graph = TestFactory.Graph("A");
        var stored = TestFactory.Stored();

        // Act
        var act = () => CachePlanBuilder.Build(graph, null!, stored);

        // Assert
        Assert.Throws<ArgumentNullException>(act);
    }

    [Fact]
    public void Build_ShouldThrow_WhenStoredIsNull()
    {
        // Arrange
        var graph = TestFactory.Graph("A");
        var inputs = TestFactory.OwnInputs(("A", ["a"]));

        // Act
        var act = () => CachePlanBuilder.Build(graph, inputs, null!);

        // Assert
        Assert.Throws<ArgumentNullException>(act);
    }

    [Fact]
    public void Build_ShouldThrow_WhenAProjectHasNoOwnHashInputs()
    {
        // Arrange
        var graph = TestFactory.Graph("A");
        var inputs = TestFactory.OwnInputs();
        var stored = TestFactory.Stored();

        // Act
        var act = () => CachePlanBuilder.Build(graph, inputs, stored);

        // Assert
        Assert.Throws<ArgumentException>(act);
    }

    [Fact]
    public void Build_ShouldMissAndGate_WhenNothingIsStoredForTheProject()
    {
        // Arrange
        var graph = TestFactory.Graph("A");
        var inputs = TestFactory.OwnInputs(("A", ["a=1"]));
        var stored = TestFactory.Stored();

        // Act
        var plan = CachePlanBuilder.Build(graph, inputs, stored);

        // Assert
        var decision = plan[TestFactory.Id("A")];
        Assert.Equal(CacheOutcome.Miss, decision.BuildOutcome);
        Assert.True(decision.ShouldGateCoverage);
        Assert.False(decision.Forced);
    }

    [Fact]
    public void Build_ShouldHitAndNotGate_WhenComputedHashesMatchWhatIsStored()
    {
        // Arrange — compute the real hashes first, so "stored" reflects a genuine prior run
        // rather than a hand-picked string that happens to match by coincidence.
        var graph = TestFactory.Graph("A");
        var inputs = TestFactory.OwnInputs(("A", ["a=1"]));
        var ownHash = TargetHasher.ComputeOwnHash(["a=1"]);
        var fullHash = TargetHasher.ComputeFullHash(ownHash, []);
        var stored = TestFactory.Stored(("A", ownHash, fullHash));

        // Act
        var plan = CachePlanBuilder.Build(graph, inputs, stored);

        // Assert
        var decision = plan[TestFactory.Id("A")];
        Assert.Equal(CacheOutcome.Hit, decision.BuildOutcome);
        Assert.False(decision.ShouldGateCoverage);
    }

    [Fact]
    public void Build_ShouldMissAndGate_WhenOwnHashChangedSinceItWasStored()
    {
        // Arrange
        var graph = TestFactory.Graph("A");
        var inputs = TestFactory.OwnInputs(("A", ["a=2"]));
        var staleOwnHash = TargetHasher.ComputeOwnHash(["a=1"]);
        var staleFullHash = TargetHasher.ComputeFullHash(staleOwnHash, []);
        var stored = TestFactory.Stored(("A", staleOwnHash, staleFullHash));

        // Act
        var plan = CachePlanBuilder.Build(graph, inputs, stored);

        // Assert
        var decision = plan[TestFactory.Id("A")];
        Assert.Equal(CacheOutcome.Miss, decision.BuildOutcome);
        Assert.True(decision.ShouldGateCoverage);
    }

    [Fact]
    public void Build_ShouldMissDependentButNotGateIt_WhenOnlyItsDependencyChanged()
    {
        // Arrange — Core changes; Buhlmann's own inputs are untouched. Buhlmann must rebuild and
        // retest (fullHash moves, because Core is in its closure) but is not re-measured (its
        // ownHash is unchanged) — "unchanged; rebuilt but not re-measured".
        var graph = TestFactory.Graph("Buhlmann -> Core", "Core");

        var coreOwnHashBefore = TargetHasher.ComputeOwnHash(["core:v1"]);
        var coreFullHashBefore = TargetHasher.ComputeFullHash(coreOwnHashBefore, []);
        var buhlmannOwnHash = TargetHasher.ComputeOwnHash(["buhlmann:v1"]);
        var buhlmannFullHashBefore = TargetHasher.ComputeFullHash(buhlmannOwnHash, [coreFullHashBefore]);

        var stored = TestFactory.Stored(
            ("Core", coreOwnHashBefore, coreFullHashBefore),
            ("Buhlmann", buhlmannOwnHash, buhlmannFullHashBefore));

        var inputs = TestFactory.OwnInputs(
            ("Core", ["core:v2"]),
            ("Buhlmann", ["buhlmann:v1"]));

        // Act
        var plan = CachePlanBuilder.Build(graph, inputs, stored);

        // Assert
        var core = plan[TestFactory.Id("Core")];
        var buhlmann = plan[TestFactory.Id("Buhlmann")];

        Assert.Equal(CacheOutcome.Miss, core.BuildOutcome);
        Assert.True(core.ShouldGateCoverage);

        Assert.Equal(CacheOutcome.Miss, buhlmann.BuildOutcome);
        Assert.False(buhlmann.ShouldGateCoverage);
    }

    [Fact]
    public void Build_ShouldHitDependent_WhenNeitherItNorItsDependencyChanged()
    {
        // Arrange
        var graph = TestFactory.Graph("Buhlmann -> Core", "Core");
        var inputs = TestFactory.UniformOwnInputs(graph);

        var coreOwnHash = TargetHasher.ComputeOwnHash(inputs[TestFactory.Id("Core")]);
        var coreFullHash = TargetHasher.ComputeFullHash(coreOwnHash, []);
        var buhlmannOwnHash = TargetHasher.ComputeOwnHash(inputs[TestFactory.Id("Buhlmann")]);
        var buhlmannFullHash = TargetHasher.ComputeFullHash(buhlmannOwnHash, [coreFullHash]);

        var stored = TestFactory.Stored(
            ("Core", coreOwnHash, coreFullHash),
            ("Buhlmann", buhlmannOwnHash, buhlmannFullHash));

        // Act
        var plan = CachePlanBuilder.Build(graph, inputs, stored);

        // Assert
        Assert.All(plan.Values, decision => Assert.Equal(CacheOutcome.Hit, decision.BuildOutcome));
        Assert.All(plan.Values, decision => Assert.False(decision.ShouldGateCoverage));
    }

    [Fact]
    public void Build_ShouldForceAMiss_WhenProjectIsInTheForcedSet_EvenIfHashesMatch()
    {
        // Arrange
        var graph = TestFactory.Graph("A");
        var inputs = TestFactory.OwnInputs(("A", ["a=1"]));
        var ownHash = TargetHasher.ComputeOwnHash(["a=1"]);
        var fullHash = TargetHasher.ComputeFullHash(ownHash, []);
        var stored = TestFactory.Stored(("A", ownHash, fullHash));
        var forced = new HashSet<ProjectId> { TestFactory.Id("A") };

        // Act
        var plan = CachePlanBuilder.Build(graph, inputs, stored, forced);

        // Assert
        var decision = plan[TestFactory.Id("A")];
        Assert.Equal(CacheOutcome.Miss, decision.BuildOutcome);
        Assert.True(decision.Forced);
        // --cacheless exists to re-verify from nothing, so a forced project is re-gated even
        // though its ownHash still matches what main has stored — the real hash is still what
        // gets recomputed, so the plan can be promoted normally if this run is a merge_group run.
        Assert.True(decision.ShouldGateCoverage);
    }

    [Fact]
    public void Build_ShouldMissButNotGate_WhenOnlyAGlobalFullHashInputChanges()
    {
        // Arrange — a tool-version bump: nothing about the project or its dependencies moved,
        // only the extra global entry folded into every fullHash.
        var graph = TestFactory.Graph("A");
        var inputs = TestFactory.OwnInputs(("A", ["a=1"]));
        var ownHash = TargetHasher.ComputeOwnHash(["a=1"]);
        var staleFullHash = TargetHasher.ComputeFullHash(ownHash, ["ci-tool:v1"]);
        var stored = TestFactory.Stored(("A", ownHash, staleFullHash));

        // Act
        var plan = CachePlanBuilder.Build(graph, inputs, stored, globalFullHashInputs: ["ci-tool:v2"]);

        // Assert
        var decision = plan[TestFactory.Id("A")];
        Assert.Equal(CacheOutcome.Miss, decision.BuildOutcome);
        Assert.False(decision.ShouldGateCoverage);
    }

    [Fact]
    public void Build_ShouldHit_WhenGlobalFullHashInputIsUnchanged()
    {
        // Arrange
        var graph = TestFactory.Graph("A");
        var inputs = TestFactory.OwnInputs(("A", ["a=1"]));
        var ownHash = TargetHasher.ComputeOwnHash(["a=1"]);
        var fullHash = TargetHasher.ComputeFullHash(ownHash, ["ci-tool:v1"]);
        var stored = TestFactory.Stored(("A", ownHash, fullHash));

        // Act
        var plan = CachePlanBuilder.Build(graph, inputs, stored, globalFullHashInputs: ["ci-tool:v1"]);

        // Assert
        Assert.Equal(CacheOutcome.Hit, plan[TestFactory.Id("A")].BuildOutcome);
    }

    [Fact]
    public void Build_ShouldReturnOneDecisionPerProject_WhenGraphHasMultipleProjects()
    {
        // Arrange
        var graph = TestFactory.Graph("A -> C", "B -> C", "C");
        var inputs = TestFactory.UniformOwnInputs(graph);
        var stored = TestFactory.Stored();

        // Act
        var plan = CachePlanBuilder.Build(graph, inputs, stored);

        // Assert
        Assert.Equal(3, plan.Count);
        Assert.Contains(TestFactory.Id("A"), plan.Keys);
        Assert.Contains(TestFactory.Id("B"), plan.Keys);
        Assert.Contains(TestFactory.Id("C"), plan.Keys);
    }
}
