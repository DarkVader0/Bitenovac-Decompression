namespace Bitenovac.RemoteBuildTool;

/// <summary>
/// Represents the writers that a command reports progress and failures to.
/// </summary>
/// <param name="Out">The writer for progress, decisions and results.</param>
/// <param name="Error">The writer for anything that makes the run fail.</param>
internal sealed record PipelineOutput(TextWriter Out, TextWriter Error)
{
    /// <summary>
    /// Gets an output that writes to the process console.
    /// </summary>
    public static PipelineOutput Console => new(System.Console.Out, System.Console.Error);

    /// <summary>
    /// Writes one line of progress.
    /// </summary>
    /// <param name="message">The line to write.</param>
    public void WriteLine(string message) => Out.WriteLine(message);

    /// <summary>
    /// Writes one line of failure.
    /// </summary>
    /// <param name="message">The line to write.</param>
    public void WriteError(string message) => Error.WriteLine(message);
}
