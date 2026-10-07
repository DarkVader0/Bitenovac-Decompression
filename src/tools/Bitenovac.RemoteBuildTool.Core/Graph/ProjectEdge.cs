namespace Bitenovac.RemoteBuildTool.Core.Graph;

/// <summary>
/// Represents a reference from one project to another in a <see cref="ProjectGraph"/>.
/// </summary>
/// <param name="Project">The project that declares the reference.</param>
/// <param name="References">The project that <paramref name="Project"/> references.</param>
public readonly record struct ProjectEdge(ProjectId Project, ProjectId References);
