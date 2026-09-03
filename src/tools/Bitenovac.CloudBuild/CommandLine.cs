using Bitenovac.CloudBuild.Commands;

namespace Bitenovac.CloudBuild;

/// <summary>Dispatches to one pipeline command. Kept separate from <c>Program.cs</c> so nothing here runs before <c>MSBuildLocator.RegisterDefaults()</c> does.</summary>
internal static class CommandLine
{
    public static int Run(string[] args)
    {
        if (args.Length == 0)
        {
            PrintUsage();
            return 1;
        }

        var options = PipelineOptions.FromEnvironment();
        try
        {
            return args[0] switch
            {
                "plan" => PlanCommand.Run(options),
                "build" => WithConfiguration(args, configuration => BuildCommand.Run(options, configuration)),
                "test" => WithConfiguration(args, configuration => TestCommand.Run(options, configuration)),
                "promote" => PromoteCommand.Run(options),
                "cleanup" => CleanupCommand.Run(options, args.Contains("--keep-artifacts")),
                "graph" => GraphCommand.Run(options),
                "--help" or "-h" => Usage(),
                _ => Fail($"Unknown command '{args[0]}'."),
            };
        }
        catch (InvalidOperationException exception)
        {
            return Fail(exception.Message);
        }
    }

    private static int WithConfiguration(string[] args, Func<string, int> run)
    {
        if (args.Length < 2)
            return Fail("A configuration is required: 'Debug' or 'Release'.");

        return args[1] switch
        {
            "Debug" or "Release" => run(args[1]),
            _ => Fail($"Configuration must be 'Debug' or 'Release', got '{args[1]}'."),
        };
    }

    private static int Usage()
    {
        PrintUsage();
        return 0;
    }

    private static int Fail(string message)
    {
        Console.Error.WriteLine($"error: {message}");
        return 1;
    }

    private static void PrintUsage() =>
        Console.WriteLine("""
            Bitenovac CloudBuild

            Usage:
              bitenovac-ci plan                 Discover, verify, restore, hash, decide hit/miss
              bitenovac-ci build <Config>        Materialise hits, compile misses
              bitenovac-ci test <Config>         Restore or run tests, gate coverage on Debug
              bitenovac-ci promote                Copy this run's qualifying entries into main
              bitenovac-ci cleanup [--keep-artifacts]   Drop this run's own store
              bitenovac-ci graph                  Print the graph with each target's hashes

            Environment:
              CLOUDBUILD_REPO_ROOT       Repository root (default: current directory)
              CLOUDBUILD_MAIN_STORE      main's artifact volume (default: artifacts/ci-main)
              CLOUDBUILD_PR_STORE        This run's own artifact volume (default: artifacts/ci-pr)
              CLOUDBUILD_CACHELESS       true/1 to ignore main and rebuild everything (PR runs only;
                                 never promotes)
              CLOUDBUILD_COVERAGE_HTML   0 to skip the HTML coverage report (default: on locally, off in CI)
            """);
}
