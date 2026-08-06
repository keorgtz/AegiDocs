namespace AegiDocs.Domain.Identifiers;

/// <summary>
/// Identifies a document project within the domain.
/// </summary>
public readonly record struct ProjectId
{
    private ProjectId(Guid value)
    {
        Value = value;
    }

    /// <summary>
    /// Gets the GUID value used for durable storage boundaries.
    /// </summary>
    public Guid Value { get; }

    /// <summary>
    /// Creates a new project identifier.
    /// </summary>
    public static ProjectId New() => new(Guid.NewGuid());

    /// <summary>
    /// Reconstitutes a project identifier from a non-empty GUID.
    /// </summary>
    public static ProjectId Create(Guid value) => value == Guid.Empty
        ? throw new ArgumentException("A project identifier cannot be empty.", nameof(value))
        : new ProjectId(value);

    /// <summary>
    /// Parses a project identifier persisted as a GUID.
    /// </summary>
    public static ProjectId Parse(string value) => Create(Guid.Parse(value));

    /// <summary>
    /// Tries to parse a non-empty project identifier persisted as a GUID.
    /// </summary>
    public static bool TryParse(string? value, out ProjectId projectId)
    {
        if (Guid.TryParse(value, out var guid) && guid != Guid.Empty)
        {
            projectId = new ProjectId(guid);
            return true;
        }

        projectId = default;
        return false;
    }
}
