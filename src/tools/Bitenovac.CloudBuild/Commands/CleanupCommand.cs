namespace Bitenovac.CloudBuild.Commands;

/// <summary>Drops this run's own volume. Runs with <c>if: always()</c> in the workflow, win or lose.</summary>
internal static class CleanupCommand
{
    public static int Run(PipelineOptions options, bool keepArtifacts, PipelineOutput output)
    {
        if (keepArtifacts)
        {
            output.WriteLine($"--keep-artifacts: leaving {options.PrStoreRoot} in place.");
            return 0;
        }

        if (!Directory.Exists(options.PrStoreRoot))
        {
            output.WriteLine("Nothing to clean up.");
            return 0;
        }

        Directory.Delete(options.PrStoreRoot, recursive: true);
        output.WriteLine($"Removed {options.PrStoreRoot}.");
        return 0;
    }
}
