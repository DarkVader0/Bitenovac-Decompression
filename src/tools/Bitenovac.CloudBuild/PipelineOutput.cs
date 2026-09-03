namespace Bitenovac.CloudBuild;

/// <summary>
/// Where a command reports progress and failures. Passed in rather than reached for through
/// <see cref="System.Console"/> so two runs in one process — which is what a parallel test suite
/// is — cannot write into each other's output.
/// </summary>
/// <param name="Out">Progress, decisions, and results.</param>
/// <param name="Error">Anything that makes the run fail.</param>
internal sealed record PipelineOutput(TextWriter Out, TextWriter Error)
{
    /// <summary>The process console, which is what the tool writes to when it runs for real.</summary>
    public static PipelineOutput Console => new(System.Console.Out, System.Console.Error);

    /// <summary>Reports one line of progress.</summary>
    /// <param name="message">The line to write.</param>
    public void WriteLine(string message) => Out.WriteLine(message);

    /// <summary>Reports one line of failure.</summary>
    /// <param name="message">The line to write.</param>
    public void WriteError(string message) => Error.WriteLine(message);
}
