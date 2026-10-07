using Bitenovac.RemoteBuildTool.Commands;

namespace Bitenovac.RemoteBuildTool;

/// <summary>
/// Provides methods for dispatching command-line arguments to a pipeline command.
/// </summary>
internal static class CommandLine
{
    public static int Run(string[] args, PipelineOutput output)
    {
        if (args.Length == 0)
        {
            PrintUsage(output);
            return 1;
        }

        var options = PipelineOptions.FromEnvironment();
        try
        {
            return args[0] switch
            {
                "plan" => PlanCommand.Run(options, output),
                "build" => WithConfiguration(args, output, configuration => BuildCommand.Run(options, configuration, output)),
                "test" => WithConfiguration(args, output, configuration => TestCommand.Run(options, configuration, output)),
                "promote" => PromoteCommand.Run(options, output),
                "graph" => GraphCommand.Run(options, output),
                "--help" or "-h" => Usage(output),
                _ => Fail(output, $"Unknown command '{args[0]}'."),
            };
        }
        catch (InvalidOperationException exception)
        {
            return Fail(output, exception.Message);
        }
    }

    private static int WithConfiguration(string[] args, PipelineOutput output, Func<string, int> run)
    {
        if (args.Length < 2)
            return Fail(output, "A configuration is required: 'Debug' or 'Release'.");

        return args[1] switch
        {
            "Debug" or "Release" => run(args[1]),
            _ => Fail(output, $"Configuration must be 'Debug' or 'Release', got '{args[1]}'."),
        };
    }

    private static int Usage(PipelineOutput output)
    {
        PrintUsage(output);
        return 0;
    }

    private static int Fail(PipelineOutput output, string message)
    {
        output.WriteError($"error: {message}");
        return 1;
    }

    private static void PrintUsage(PipelineOutput output) =>
        output.WriteLine("""
            Bitenovac RemoteBuildTool

            Usage:
              bitenovac-ci plan                 Discover, verify, restore, hash, decide hit/miss
              bitenovac-ci build <Config>        Materialise hits, compile misses
              bitenovac-ci test <Config>         Restore or run tests, gate coverage on Debug
              bitenovac-ci promote                Copy this run's qualifying entries into main
              bitenovac-ci graph                  Print the graph with each target's hashes

            Environment:
              REMOTEBUILDTOOL_REPO_ROOT       Repository root (default: current directory)
              REMOTEBUILDTOOL_MAIN_STORE      main's artifact volume (default: artifacts/ci-main)
              REMOTEBUILDTOOL_PR_STORE        This run's own artifact volume (default: artifacts/ci-pr)
              REMOTEBUILDTOOL_CACHELESS       true/1 to ignore main and rebuild everything (PR runs only;
                                 never promotes)
              REMOTEBUILDTOOL_COVERAGE_HTML   0 to skip the HTML coverage report (default: on locally, off in CI)
            """);
}
