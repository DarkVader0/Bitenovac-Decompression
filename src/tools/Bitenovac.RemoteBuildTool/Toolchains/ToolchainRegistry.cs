using Bitenovac.RemoteBuildTool.Toolchains.DotNet;

namespace Bitenovac.RemoteBuildTool.Toolchains;

/// <summary>Every toolchain the pipeline drives. Supporting another language means adding it to <see cref="Create"/>.</summary>
internal sealed class ToolchainRegistry : IDisposable
{
    private readonly IReadOnlyList<IToolchain> _toolchains;

    public ToolchainRegistry(IReadOnlyList<IToolchain> toolchains)
    {
        var duplicate = toolchains.GroupBy(toolchain => toolchain.Name, StringComparer.Ordinal).FirstOrDefault(group => group.Count() > 1);
        if (duplicate is not null)
            throw new ArgumentException($"Two toolchains are named '{duplicate.Key}'.", nameof(toolchains));

        _toolchains = [.. toolchains];
    }

    public static ToolchainRegistry Create(string repositoryRoot) =>
        new([new DotNetToolchain(repositoryRoot)]);

    public IReadOnlyList<IToolchain> All => _toolchains;

    /// <exception cref="InvalidOperationException">No toolchain has this name.</exception>
    public IToolchain For(string name) =>
        _toolchains.FirstOrDefault(toolchain => toolchain.Name == name)
        ?? throw new InvalidOperationException($"The plan names toolchain '{name}', which this RemoteBuildTool does not have. Re-run 'plan'.");

    public void Dispose()
    {
        foreach (var toolchain in _toolchains)
            toolchain.Dispose();
    }
}
