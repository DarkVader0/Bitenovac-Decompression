namespace Bitenovac.Ci.Commands;

/// <summary>Drops this run's own volume. Runs with <c>if: always()</c> in the workflow, win or lose.</summary>
internal static class CleanupCommand
{
    public static int Run(PipelineOptions options, bool keepArtifacts)
    {
        if (keepArtifacts)
        {
            Console.WriteLine($"--keep-artifacts: leaving {options.PrStoreRoot} in place.");
            return 0;
        }

        if (!Directory.Exists(options.PrStoreRoot))
        {
            Console.WriteLine("Nothing to clean up.");
            return 0;
        }

        Directory.Delete(options.PrStoreRoot, recursive: true);
        Console.WriteLine($"Removed {options.PrStoreRoot}.");
        return 0;
    }
}
