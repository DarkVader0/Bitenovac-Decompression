using System.Xml.Linq;
using Bitenovac.Ci.Core.Coverage;

namespace Bitenovac.Ci.Testing;

/// <summary>Reads a merged cobertura report into a measurement per assembly.</summary>
internal static class CoverageReportReader
{
    public static IReadOnlyDictionary<string, CoverageMeasurement> Read(string mergedReportPath)
    {
        var document = XDocument.Load(mergedReportPath);
        var measurements = new Dictionary<string, CoverageMeasurement>();

        foreach (var package in document.Descendants("package"))
        {
            var name = (string?)package.Attribute("name");
            if (string.IsNullOrEmpty(name))
                continue;

            var lineRate = (double?)package.Attribute("line-rate") ?? 0;
            var branchRate = (double?)package.Attribute("branch-rate") ?? 0;

            measurements[name] = new CoverageMeasurement(lineRate * 100, branchRate * 100);
        }

        return measurements;
    }
}
