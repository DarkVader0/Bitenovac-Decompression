namespace Bitenovac.CloudBuild.Core.Graph;

/// <summary>One <c>ProjectReference</c>: <see cref="Project"/> must rebuild when <see cref="References"/> changes.</summary>
/// <param name="Project">The project declaring the reference.</param>
/// <param name="References">The project it references.</param>
public readonly record struct ProjectEdge(ProjectId Project, ProjectId References);
