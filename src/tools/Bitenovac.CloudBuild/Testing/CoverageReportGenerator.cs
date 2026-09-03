using Bitenovac.CloudBuild.Processes;

namespace Bitenovac.CloudBuild.Testing;

/// <summary>
/// Merges every cobertura report produced this run into one, via the <c>reportgenerator</c>
/// local tool. Each test project exercises only part of a library, and coverlet writes the
/// source path differently depending on which project produced the report, so a merge keyed on
/// file path double-counts every line without this step.
/// </summary>
internal static class CoverageReportGenerator
{
    public static string Merge(string repositoryRoot, string coverageDirectory, string reportDirectory, bool includeHtml)
    {
        Directory.CreateDirectory(reportDirectory);

        var reportTypes = includeHtml ? "Cobertura;TextSummary;Html" : "Cobertura;TextSummary";

        var exitCode = ProcessRunner.Run(
            "dotnet",
            [
                "reportgenerator",
                $"-reports:{Path.Combine(coverageDirectory, "*cobertura*.xml")}",
                $"-targetdir:{reportDirectory}",
                $"-reporttypes:{reportTypes}",
            ],
            repositoryRoot);

        if (exitCode != 0)
            throw new InvalidOperationException($"reportgenerator failed (exit {exitCode}) merging reports under {coverageDirectory}.");

        var merged = Path.Combine(reportDirectory, "Cobertura.xml");
        if (!File.Exists(merged))
            throw new InvalidOperationException($"reportgenerator produced no merged report at {merged}.");

        return merged;
    }
}
