using System.Xml.Linq;

namespace Bitenovac.RemoteBuildTool.MsBuild;

/// <summary>
/// Provides methods for writing a <c>.slnx</c> solution that lists a set of projects.
/// </summary>
/// <remarks>
/// One MSBuild invocation on the solution restores the projects in a single NuGet pass and builds
/// them in one dependency-ordered graph.
/// </remarks>
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
