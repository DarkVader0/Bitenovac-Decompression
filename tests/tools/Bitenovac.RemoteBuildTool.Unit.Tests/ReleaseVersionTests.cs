using Bitenovac.RemoteBuildTool.Releasing;

namespace Bitenovac.RemoteBuildTool.Unit.Tests;

public sealed class ReleaseVersionTests
{
    private static readonly DateOnly Today = new(2026, 10, 7);

    [Fact]
    public void Next_ShouldStartAtOne_WhenTheDropRootDoesNotExist()
    {
        // Arrange
        using var directory = TestFactory.Directory();

        // Act
        var version = ReleaseVersion.Next(directory.Combine("missing"), Today);

        // Assert
        Assert.Equal("2026.10.07.001", version);
    }

    [Fact]
    public void Next_ShouldStartAtOne_WhenOnlyOtherDatesWereDropped()
    {
        // Arrange
        using var directory = TestFactory.Directory();
        Directory.CreateDirectory(directory.Combine("2026.10.06.004"));

        // Act
        var version = ReleaseVersion.Next(directory.Path, Today);

        // Assert
        Assert.Equal("2026.10.07.001", version);
    }

    [Fact]
    public void Next_ShouldFollowTheHighestCounter_WhenTheDateWasAlreadyDropped()
    {
        // Arrange
        using var directory = TestFactory.Directory();
        Directory.CreateDirectory(directory.Combine("2026.10.07.001"));
        Directory.CreateDirectory(directory.Combine("2026.10.07.009"));
        Directory.CreateDirectory(directory.Combine("2026.10.07.002"));

        // Act
        var version = ReleaseVersion.Next(directory.Path, Today);

        // Assert
        Assert.Equal("2026.10.07.010", version);
    }

    [Theory]
    [InlineData(".staging-2026.10.07.005")]
    [InlineData("2026.10.07.latest")]
    [InlineData("2026.10.07.-3")]
    public void Next_ShouldIgnoreTheDirectory_WhenItIsNotANumberedDrop(string name)
    {
        // Arrange
        using var directory = TestFactory.Directory();
        Directory.CreateDirectory(directory.Combine(name));

        // Act
        var version = ReleaseVersion.Next(directory.Path, Today);

        // Assert
        Assert.Equal("2026.10.07.001", version);
    }

    [Fact]
    public void Next_ShouldIgnoreFiles_WhenTheyAreNamedLikeADrop()
    {
        // Arrange
        using var directory = TestFactory.Directory();
        TestFactory.WriteFile(directory.Path, "2026.10.07.003", "not a drop");

        // Act
        var version = ReleaseVersion.Next(directory.Path, Today);

        // Assert
        Assert.Equal("2026.10.07.001", version);
    }
}
