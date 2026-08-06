namespace AegiDocs.Domain.Identifiers;

/// <summary>
/// Identifies a tutorial within a document project.
/// </summary>
public readonly record struct TutorialId
{
    private TutorialId(Guid value)
    {
        Value = value;
    }

    /// <summary>
    /// Gets the GUID value used for durable storage boundaries.
    /// </summary>
    public Guid Value { get; }

    /// <summary>
    /// Creates a new tutorial identifier.
    /// </summary>
    public static TutorialId New() => new(Guid.NewGuid());

    /// <summary>
    /// Reconstitutes a tutorial identifier from a non-empty GUID.
    /// </summary>
    public static TutorialId Create(Guid value) => value == Guid.Empty
        ? throw new ArgumentException("A tutorial identifier cannot be empty.", nameof(value))
        : new TutorialId(value);

    /// <summary>
    /// Parses a tutorial identifier persisted as a GUID.
    /// </summary>
    public static TutorialId Parse(string value) => Create(Guid.Parse(value));

    /// <summary>
    /// Tries to parse a non-empty tutorial identifier persisted as a GUID.
    /// </summary>
    public static bool TryParse(string? value, out TutorialId tutorialId)
    {
        if (Guid.TryParse(value, out var guid) && guid != Guid.Empty)
        {
            tutorialId = new TutorialId(guid);
            return true;
        }

        tutorialId = default;
        return false;
    }
}
