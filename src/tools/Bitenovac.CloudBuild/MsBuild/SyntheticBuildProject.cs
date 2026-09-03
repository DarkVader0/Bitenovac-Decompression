using System.Xml.Linq;

namespace Bitenovac.CloudBuild.MsBuild;

/// <summary>
/// Writes a project that restores or builds a set of projects in one MSBuild session — ported
/// from <c>write_build_project</c> in the old <c>build/ci.sh</c>. One session evaluates each
/// shared dependency once and schedules the whole graph across cores; a loop of "dotnet build
/// &lt;project&gt;" would re-evaluate the whole reference closure of every project separately.
/// </summary>
/// <remarks>
/// A plain <c>&lt;Project&gt;</c> with no Sdk attribute, so it does not import
/// <c>Directory.Build.props</c> and cannot pick up repository defaults meant for real projects.
/// </remarks>
internal static class SyntheticBuildProject
{
    public static string Write(string outputPath, IEnumerable<string> projectFullPaths)
    {
        var items = new XElement("ItemGroup",
            projectFullPaths.Select(path => new XElement("CloudBuildProject", new XAttribute("Include", path))));

        var document = new XDocument(
            new XElement("Project",
                items,
                new XElement("Target", new XAttribute("Name", "Restore"),
                    new XElement("MSBuild",
                        new XAttribute("Projects", "@(CloudBuildProject)"),
                        new XAttribute("Targets", "Restore"),
                        new XAttribute("BuildInParallel", "false"))),
                new XElement("Target", new XAttribute("Name", "Build"),
                    new XElement("MSBuild",
                        new XAttribute("Projects", "@(CloudBuildProject)"),
                        new XAttribute("Targets", "Build"),
                        new XAttribute("BuildInParallel", "true")))));

        Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
        document.Save(outputPath);
        return outputPath;
    }
}
