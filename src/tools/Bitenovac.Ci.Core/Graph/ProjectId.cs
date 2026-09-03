namespace Bitenovac.Ci.Core.Graph;

/// <summary>
/// Identifies a project by its path relative to the repository root, with backslashes
/// normalised to forward slashes so a path read on Windows compares equal to one read on Linux.
/// </summary>
public sealed record ProjectId : IComparable<ProjectId>
{
    /// <summary>The repository-relative path, using forward slashes.</summary>
    public string Value { get; }

    /// <summary>Creates a <see cref="ProjectId"/> from a repository-relative path.</summary>
    /// <param name="value">The path, with either slash style.</param>
    /// <exception cref="ArgumentException"><paramref name="value"/> is null, empty, or white space.</exception>
    public ProjectId(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        Value = value.Replace('\\', '/');
    }

    /// <inheritdoc/>
    public int CompareTo(ProjectId? other) =>
        string.CompareOrdinal(Value, other?.Value);

    /// <inheritdoc/>
    public override string ToString() => Value;
}
