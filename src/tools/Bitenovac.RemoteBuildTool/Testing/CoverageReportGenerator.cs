using Bitenovac.RemoteBuildTool.Processes;

namespace Bitenovac.RemoteBuildTool.Testing;

/// <summary>
/// Provides methods for merging every Cobertura report produced in this run into one, using the
/// <c>reportgenerator</c> local tool.
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
