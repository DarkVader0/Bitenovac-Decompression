using System.Diagnostics;

namespace Bitenovac.CloudBuild.Processes;

/// <summary>Runs an external process, streaming its output live and reporting its exit code.</summary>
internal static class ProcessRunner
{
    public static int Run(string fileName, IReadOnlyList<string> arguments, string workingDirectory, IReadOnlyDictionary<string, string>? environment = null)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = fileName,
            WorkingDirectory = workingDirectory,
            UseShellExecute = false,
        };

        foreach (var argument in arguments)
            startInfo.ArgumentList.Add(argument);

        if (environment is not null)
        {
            foreach (var (key, value) in environment)
                startInfo.Environment[key] = value;
        }

        using var process = Process.Start(startInfo) ?? throw new InvalidOperationException($"Could not start '{fileName}'.");
        process.WaitForExit();
        return process.ExitCode;
    }
}
