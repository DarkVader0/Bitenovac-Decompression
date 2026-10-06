using System.Xml.Linq;

namespace Bitenovac.CloudBuild.MsBuild;

/// <summary>
/// Writes a <c>.slnx</c> listing a set of projects, so one MSBuild invocation restores them in a
/// single NuGet pass and builds them in one dependency-ordered graph.
/// </summary>
internal static class SyntheticSolution
{
    public static string Write(string outputPath, IEnumerable<string> projectFullPaths)
    {
        var solutionDirectory = Path.GetDirectoryName(Path.GetFullPath(outputPath))!;

        var document = new XDocument(
            new XElement("Solution",
                projectFullPaths.Select(path => new XElement("Project",
                    new XAttribute("Path", Path.GetRelativePath(solutionDirectory, path).Replace('\\', '/'))))));

        Directory.CreateDirectory(solutionDirectory);
        document.Save(outputPath);
        return outputPath;
    }
}
