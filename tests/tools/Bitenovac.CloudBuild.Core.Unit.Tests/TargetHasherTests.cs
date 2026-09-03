using Bitenovac.CloudBuild.Core.Hashing;

namespace Bitenovac.CloudBuild.Core.Unit.Tests;

public sealed class TargetHasherTests
{
    [Fact]
    public void ComputeOwnHash_ShouldThrow_WhenInputsIsNull()
    {
        // Arrange

        // Act
        var act = () => TargetHasher.ComputeOwnHash(null!);

        // Assert
        Assert.Throws<ArgumentNullException>(act);
    }

    [Fact]
    public void ComputeOwnHash_ShouldBeStable_WhenCalledTwiceWithTheSameInputs()
    {
        // Arrange
        string[] inputs = ["a=1", "b=2"];

        // Act
        var first = TargetHasher.ComputeOwnHash(inputs);
        var second = TargetHasher.ComputeOwnHash(inputs);

        // Assert
        Assert.Equal(first, second);
    }

    [Fact]
    public void ComputeOwnHash_ShouldBeOrderIndependent_WhenInputsAreShuffled()
    {
        // Arrange
        string[] inOrder = ["a=1", "b=2", "c=3"];
        string[] shuffled = ["c=3", "a=1", "b=2"];

        // Act
        var fromInOrder = TargetHasher.ComputeOwnHash(inOrder);
        var fromShuffled = TargetHasher.ComputeOwnHash(shuffled);

        // Assert
        Assert.Equal(fromInOrder, fromShuffled);
    }

    [Fact]
    public void ComputeOwnHash_ShouldDiffer_WhenOneInputChanges()
    {
        // Arrange
        string[] before = ["source:Foo.cs=aaa", "package:Bar/1.0.0"];
        string[] after = ["source:Foo.cs=aaa", "package:Bar/1.1.0"];

        // Act
        var beforeHash = TargetHasher.ComputeOwnHash(before);
        var afterHash = TargetHasher.ComputeOwnHash(after);

        // Assert
        Assert.NotEqual(beforeHash, afterHash);
    }

    [Fact]
    public void ComputeOwnHash_ShouldReturnLowercaseHexSha256_WhenGivenAnyInput()
    {
        // Arrange
        string[] inputs = ["x"];

        // Act
        var hash = TargetHasher.ComputeOwnHash(inputs);

        // Assert
        Assert.Equal(64, hash.Length);
        Assert.Matches("^[0-9a-f]{64}$", hash);
    }

    [Fact]
    public void ComputeFullHash_ShouldThrow_WhenOwnHashIsNull()
    {
        // Arrange

        // Act
        var act = () => TargetHasher.ComputeFullHash(null!, []);

        // Assert
        Assert.Throws<ArgumentNullException>(act);
    }

    [Fact]
    public void ComputeFullHash_ShouldThrow_WhenDependencyFullHashesIsNull()
    {
        // Arrange

        // Act
        var act = () => TargetHasher.ComputeFullHash("own", null!);

        // Assert
        Assert.Throws<ArgumentNullException>(act);
    }

    [Fact]
    public void ComputeFullHash_ShouldDifferFromOwnHash_WhenProjectHasNoDependencies()
    {
        // Arrange
        var ownHash = TargetHasher.ComputeOwnHash(["source:Foo.cs=aaa"]);

        // Act
        var fullHash = TargetHasher.ComputeFullHash(ownHash, []);

        // Assert
        Assert.NotEqual(ownHash, fullHash);
    }

    [Fact]
    public void ComputeFullHash_ShouldBeOrderIndependent_WhenDependencyHashesAreShuffled()
    {
        // Arrange
        var ownHash = TargetHasher.ComputeOwnHash(["own"]);
        string[] inOrder = ["dep-a", "dep-b"];
        string[] shuffled = ["dep-b", "dep-a"];

        // Act
        var fromInOrder = TargetHasher.ComputeFullHash(ownHash, inOrder);
        var fromShuffled = TargetHasher.ComputeFullHash(ownHash, shuffled);

        // Assert
        Assert.Equal(fromInOrder, fromShuffled);
    }

    [Fact]
    public void ComputeFullHash_ShouldChange_WhenADependencysFullHashChanges()
    {
        // Arrange
        var ownHash = TargetHasher.ComputeOwnHash(["own"]);

        // Act
        var beforeDependencyChange = TargetHasher.ComputeFullHash(ownHash, ["dep-hash-1"]);
        var afterDependencyChange = TargetHasher.ComputeFullHash(ownHash, ["dep-hash-2"]);

        // Assert
        Assert.NotEqual(beforeDependencyChange, afterDependencyChange);
    }

    [Fact]
    public void ComputeFullHash_ShouldStayTheSame_WhenOnlyAnUnrelatedProjectsOwnHashChanges()
    {
        // Arrange
        var ownHash = TargetHasher.ComputeOwnHash(["own"]);
        string[] dependencyFullHashes = ["dep-hash-1"];

        // Act
        var before = TargetHasher.ComputeFullHash(ownHash, dependencyFullHashes);
        var after = TargetHasher.ComputeFullHash(ownHash, dependencyFullHashes);

        // Assert
        Assert.Equal(before, after);
    }
}
