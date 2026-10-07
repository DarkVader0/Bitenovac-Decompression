using Bitenovac.RemoteBuildTool.Planning;
using Bitenovac.RemoteBuildTool.Releasing;
using Bitenovac.RemoteBuildTool.Toolchains;

namespace Bitenovac.RemoteBuildTool.Commands;

/// <summary>
/// Provides the <c>release</c> command, which builds every project in Release under the next
/// release version and drops the deployable output into <see cref="PipelineOptions.DropRoot"/>.
/// </summary>
/// <remarks>
/// The drop is assembled in a staging directory and renamed to its version only when every
/// toolchain succeeds, so a failed release leaves nothing behind and does not use up a number.
/// </remarks>
internal static class ReleaseCommand
{
    public static int Run(PipelineOptions options, PipelineOutput output) =>
        Run(options, output, DateOnly.FromDateTime(DateTime.UtcNow));

    public static int Run(PipelineOptions options, PipelineOutput output, DateOnly date)
    {
        var entries = PlanState.Load(options.PlanFile).For("Release");
        if (entries.Count == 0)
        {
            output.WriteError("error: the plan selected nothing for Release, so there is nothing to release.");
            return 1;
        }

        Directory.CreateDirectory(options.DropRoot);
        var version = ReleaseVersion.Next(options.DropRoot, date);
        var staging = Path.Combine(options.DropRoot, $".staging-{version}");
        var drop = Path.Combine(options.DropRoot, version);

        output.WriteLine($"==> Release {version}");

        using var toolchains = ToolchainRegistry.Create(options.RepositoryRoot);
        try
        {
            if (Directory.Exists(staging))
                Directory.Delete(staging, recursive: true);
            Directory.CreateDirectory(staging);

            foreach (var name in ToolchainOrder.Resolve(entries))
            {
                var batch = entries.Where(entry => entry.Toolchain == name).ToList();
                var exitCode = toolchains.For(name).Publish(options, batch, version, staging, output);
                if (exitCode != 0)
                {
                    output.WriteError($"error: release {version} failed; nothing was dropped.");
                    return exitCode;
                }
            }

            Directory.Move(staging, drop);
        }
        finally
        {
            if (Directory.Exists(staging))
                Directory.Delete(staging, recursive: true);
        }

        output.WriteLine($"Release {version} dropped to {drop}");
        return 0;
    }
}
