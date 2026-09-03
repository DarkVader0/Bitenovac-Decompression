using System.Security.Cryptography;
using System.Text;
using Bitenovac.CloudBuild.Hashing;

namespace Bitenovac.CloudBuild.Unit.Tests;

public sealed class SharedHashInputsTests
{
    [Fact]
    public void Read_ShouldReturnNothing_WhenNeitherFileExists()
    {
        // Arrange
        using var repository = TestFactory.Directory();

        // Act
        var shared = SharedHashInputs.Read(repository.Path);

        // Assert
        Assert.Empty(shared.CommonEntries);
    }

    [Fact]
    public void Read_ShouldEnterGlobalJsonAsItsContentHash_WhenItExists()
    {
        // Arrange
        using var repository = TestFactory.Directory();
        const string content = """{ "sdk": { "version": "10.0.100" } }""";
        TestFactory.WriteFile(repository.Path, "global.json", content);
        var expected = $"repo-file:global.json={Hash(content)}";

        // Act
        var shared = SharedHashInputs.Read(repository.Path);

        // Assert
        Assert.Contains(expected, shared.CommonEntries);
    }

    [Fact]
    public void Read_ShouldEnterNuGetConfigAsItsContentHash_WhenItExists()
    {
        // Arrange
        using var repository = TestFactory.Directory();
        const string content = "<configuration/>";
        TestFactory.WriteFile(repository.Path, "nuget.config", content);
        var digest = Hash(content);

        // Act
        var shared = SharedHashInputs.Read(repository.Path);

        // Assert
        Assert.Contains(
            shared.CommonEntries,
            entry => entry.StartsWith("repo-file:nuget.config=", StringComparison.OrdinalIgnoreCase)
                && entry.EndsWith(digest, StringComparison.Ordinal));
    }

    [Fact]
    public void Read_ShouldChangeItsEntry_WhenGlobalJsonChanges()
    {
        // Arrange
        using var repository = TestFactory.Directory();
        TestFactory.WriteFile(repository.Path, "global.json", "{}");
        var before = SharedHashInputs.Read(repository.Path).CommonEntries.Single();

        // Act
        TestFactory.WriteFile(repository.Path, "global.json", """{ "sdk": {} }""");
        var after = SharedHashInputs.Read(repository.Path).CommonEntries.Single();

        // Assert
        Assert.NotEqual(before, after);
    }

    private static string Hash(string content) =>
        Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(content)));
}
