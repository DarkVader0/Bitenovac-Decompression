namespace Bitenovac.RemoteBuildTool.Toolchains;

/// <summary>The outcome of <see cref="IToolchain.Prepare"/>: the prepared projects, or the exit code it failed with.</summary>
internal sealed record ToolchainPreparation(int ExitCode, IReadOnlyDictionary<string, IReadOnlyList<EvaluatedProject>> ByConfiguration)
{
    public bool Succeeded => ExitCode == 0;

    public static ToolchainPreparation Failed(int exitCode) => new(exitCode, new Dictionary<string, IReadOnlyList<EvaluatedProject>>());
}
