using Bitenovac.RemoteBuildTool.Planning;
using Bitenovac.RemoteBuildTool.Testing;
using Bitenovac.RemoteBuildTool.Toolchains;
using Bitenovac.RemoteBuildTool.Toolchains.DotNet;

namespace Bitenovac.RemoteBuildTool.Unit.Tests;

public sealed class ToolchainRegistryTests
{
    [Fact]
    public void Create_ShouldRegisterTheDotNetToolchain_WhenCalledForARepository()
    {
        // Arrange
        using var repository = TestFactory.Directory();

        // Act
        using var registry = ToolchainRegistry.Create(repository.Path);

        // Assert
        Assert.IsType<DotNetToolchain>(registry.For(DotNetToolchain.ToolchainName));
    }

    [Fact]
    public void For_ShouldThrow_WhenNoToolchainHasTheName()
    {
        // Arrange
        using var registry = new ToolchainRegistry([new FakeToolchain("dotnet")]);

        // Act
        var act = () => registry.For("rust");

        // Assert
        var exception = Assert.Throws<InvalidOperationException>(act);
        Assert.Contains("'rust'", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Constructor_ShouldThrow_WhenTwoToolchainsShareAName()
    {
        // Arrange
        IToolchain[] toolchains = [new FakeToolchain("node"), new FakeToolchain("node")];

        // Act
        var act = () => new ToolchainRegistry(toolchains);

        // Assert
        Assert.Throws<ArgumentException>(act);
    }

    [Fact]
    public void Dispose_ShouldDisposeEveryToolchain_WhenTheRegistryIsDisposed()
    {
        // Arrange
        var first = new FakeToolchain("dotnet");
        var second = new FakeToolchain("node");
        var registry = new ToolchainRegistry([first, second]);

        // Act
        registry.Dispose();

        // Assert
        Assert.True(first.Disposed);
        Assert.True(second.Disposed);
    }

    private sealed class FakeToolchain(string name) : IToolchain
    {
        public bool Disposed { get; private set; }

        public string Name => name;

        public IReadOnlyList<string> OutputDirectories => ["out"];

        public IReadOnlyList<string> Discover() => [];

        public IReadOnlyList<EvaluatedProject> Evaluate(IReadOnlyList<string> relativePaths, string configuration) => [];

        public ToolchainPreparation Prepare(
            PipelineOptions options,
            IReadOnlyDictionary<string, IReadOnlyList<EvaluatedProject>> byConfiguration,
            PipelineOutput output) =>
            throw new NotSupportedException();

        public int Build(PipelineOptions options, IReadOnlyList<PlanEntry> entries, string configuration, PipelineOutput output) =>
            throw new NotSupportedException();

        public TestRunResult RunTests(PlanEntry entry, string configuration, string resultsFilePrefix, string resultsDirectory) =>
            throw new NotSupportedException();

        public void Dispose() => Disposed = true;
    }
}
