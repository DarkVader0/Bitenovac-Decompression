namespace Bitenovac.RemoteBuildTool.Integration.Tests;

/// <summary>
/// Represents the output a command reported to one test.
/// </summary>
internal sealed class CapturedOutput
{
    private readonly StringWriter _out = new();
    private readonly StringWriter _error = new();

    public PipelineOutput Pipeline => new(_out, _error);

    public override string ToString() => _out.ToString();
}
