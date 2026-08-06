namespace AegiDocs.Domain.Identifiers;

/// <summary>
/// Identifies an annotation independently from its position in a step collection.
/// </summary>
/// <remarks>
/// An annotation can be edited or reordered without changing this value. Persisted
/// DTOs use the GUID value, while the typed identifier prevents a collection index
/// from becoming an accidental identity at the domain boundary.
/// </remarks>
public readonly record struct AnnotationId
{
    private AnnotationId(Guid value)
    {
        Value = value;
    }

    /// <summary>
    /// Gets the GUID value used at durable storage boundaries.
    /// </summary>
    public Guid Value { get; }

    /// <summary>
    /// Creates a new annotation identifier.
    /// </summary>
    public static AnnotationId New() => new(Guid.NewGuid());

    /// <summary>
    /// Reconstitutes an annotation identifier from a non-empty GUID.
    /// </summary>
    public static AnnotationId Create(Guid value) => value == Guid.Empty
        ? throw new ArgumentException("An annotation identifier cannot be empty.", nameof(value))
        : new AnnotationId(value);

    /// <summary>
    /// Parses an annotation identifier persisted as a GUID.
    /// </summary>
    public static AnnotationId Parse(string value) => Create(Guid.Parse(value));

    /// <summary>
    /// Tries to parse a non-empty annotation identifier persisted as a GUID.
    /// </summary>
    public static bool TryParse(string? value, out AnnotationId annotationId)
    {
        if (Guid.TryParse(value, out var guid) && guid != Guid.Empty)
        {
            annotationId = new AnnotationId(guid);
            return true;
        }

        annotationId = default;
        return false;
    }
}
