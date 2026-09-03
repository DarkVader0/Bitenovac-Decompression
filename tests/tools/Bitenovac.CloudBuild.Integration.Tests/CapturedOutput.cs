namespace Bitenovac.CloudBuild.Integration.Tests;

/// <summary>
/// Collects what a command reported. Owned by the test that creates it, so tests running side by
/// side never read each other's output.
/// </summary>
internal sealed class CapturedOutput
{
    private readonly StringWriter _out = new();
    private readonly StringWriter _error = new();

    public PipelineOutput Pipeline => new(_out, _error);

    public override string ToString() => _out.ToString();
}
