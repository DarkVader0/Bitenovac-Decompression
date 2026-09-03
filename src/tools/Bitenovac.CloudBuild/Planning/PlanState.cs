using System.Text.Json;

namespace Bitenovac.CloudBuild.Planning;

/// <summary>The whole run's plan: one entry list per configuration, read and written as JSON.</summary>
internal sealed record PlanState(Dictionary<string, List<PlanEntry>> ByConfiguration)
{
    private static readonly JsonSerializerOptions Options = new() { WriteIndented = true };

    public IReadOnlyList<PlanEntry> For(string configuration) =>
        ByConfiguration.TryGetValue(configuration, out var entries) ? entries : [];

    public void Save(string path)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, JsonSerializer.Serialize(this, Options));
    }

    public static PlanState Load(string path)
    {
        if (!File.Exists(path))
            throw new InvalidOperationException($"No plan found at {path}. Run 'plan' first.");

        var json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<PlanState>(json)
            ?? throw new InvalidOperationException($"The plan at {path} could not be read.");
    }
}
