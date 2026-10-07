using Bitenovac.RemoteBuildTool.Toolchains.DotNet;

namespace Bitenovac.RemoteBuildTool.Toolchains;

/// <summary>
/// Represents the set of toolchains that the pipeline drives.
/// </summary>
/// <remarks>
/// <see cref="Create"/> returns every supported toolchain.
/// </remarks>
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

    /// <summary>
    /// Returns the toolchain with the specified name.
    /// </summary>
    /// <param name="name">The name of the toolchain.</param>
    /// <returns>The toolchain named <paramref name="name"/>.</returns>
    /// <exception cref="InvalidOperationException">No toolchain is named <paramref name="name"/>.</exception>
    public IToolchain For(string name) =>
        _toolchains.FirstOrDefault(toolchain => toolchain.Name == name)
        ?? throw new InvalidOperationException($"The plan names toolchain '{name}', which this RemoteBuildTool does not have. Re-run 'plan'.");

    public void Dispose()
    {
        foreach (var toolchain in _toolchains)
            toolchain.Dispose();
    }
}
