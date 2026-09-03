using Bitenovac.Ci.Core.Graph;

namespace Bitenovac.Ci.Core.Unit.Tests;

public sealed class ProjectIdTests
{
    [Fact]
    public void Constructor_ShouldNormaliseBackslashesToForwardSlashes_WhenPathUsesWindowsSeparators()
    {
        // Arrange
        const string windowsPath = @"src\libraries\Foo\Foo.csproj";

        // Act
        var id = new ProjectId(windowsPath);

        // Assert
        Assert.Equal("src/libraries/Foo/Foo.csproj", id.Value);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_ShouldThrow_WhenValueIsEmptyOrWhiteSpace(string value)
    {
        // Arrange

        // Act
        var act = () => new ProjectId(value);

        // Assert
        Assert.Throws<ArgumentException>(act);
    }

    [Fact]
    public void Equality_ShouldTreatTwoIdsAsEqual_WhenTheyNormaliseToTheSamePath()
    {
        // Arrange
        var fromForwardSlash = new ProjectId("src/Foo/Foo.csproj");
        var fromBackslash = new ProjectId(@"src\Foo\Foo.csproj");

        // Act
        var areEqual = fromForwardSlash == fromBackslash;

        // Assert
        Assert.True(areEqual);
    }

    [Fact]
    public void CompareTo_ShouldOrderOrdinally_WhenComparingTwoIds()
    {
        // Arrange
        var a = new ProjectId("a/A.csproj");
        var b = new ProjectId("b/B.csproj");

        // Act
        var result = a.CompareTo(b);

        // Assert
        Assert.True(result < 0);
    }

    [Fact]
    public void CompareTo_ShouldSortAfterNull_WhenOtherIsNull()
    {
        // Arrange
        var a = new ProjectId("a/A.csproj");

        // Act
        var result = a.CompareTo(null);

        // Assert
        Assert.True(result > 0);
    }
}
