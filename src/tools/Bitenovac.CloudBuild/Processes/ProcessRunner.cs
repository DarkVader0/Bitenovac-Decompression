using System.Diagnostics;
using System.Text;

namespace Bitenovac.CloudBuild.Processes;

/// <summary>Runs an external process and reports its exit code, streaming or capturing its output.</summary>
internal static class ProcessRunner
{
    public static int Run(string fileName, IReadOnlyList<string> arguments, string workingDirectory, IReadOnlyDictionary<string, string>? environment = null)
    {
        using var process = Process.Start(CreateStartInfo(fileName, arguments, workingDirectory, environment, redirect: false))
            ?? throw new InvalidOperationException($"Could not start '{fileName}'.");
        process.WaitForExit();
        return process.ExitCode;
    }

    /// <summary>Runs the process with standard output and error collected into one string, in arrival order.</summary>
    public static (int ExitCode, string Output) RunCaptured(string fileName, IReadOnlyList<string> arguments, string workingDirectory)
    {
        var output = new StringBuilder();
        var gate = new Lock();

        using var process = new Process { StartInfo = CreateStartInfo(fileName, arguments, workingDirectory, environment: null, redirect: true) };
        process.OutputDataReceived += (_, line) => Append(line.Data);
        process.ErrorDataReceived += (_, line) => Append(line.Data);

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        process.WaitForExit();

        return (process.ExitCode, output.ToString());

        void Append(string? line)
        {
            if (line is null)
                return;

            lock (gate)
                output.AppendLine(line);
        }
    }

    private static ProcessStartInfo CreateStartInfo(
        string fileName, IReadOnlyList<string> arguments, string workingDirectory, IReadOnlyDictionary<string, string>? environment, bool redirect)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = fileName,
            WorkingDirectory = workingDirectory,
            UseShellExecute = false,
            RedirectStandardOutput = redirect,
            RedirectStandardError = redirect,
        };

        foreach (var argument in arguments)
            startInfo.ArgumentList.Add(argument);

        if (environment is not null)
        {
            foreach (var (key, value) in environment)
                startInfo.Environment[key] = value;
        }

        return startInfo;
    }
}
