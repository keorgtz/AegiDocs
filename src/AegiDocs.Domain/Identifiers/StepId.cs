namespace AegiDocs.Domain.Identifiers;

/// <summary>
/// Identifies a step within a tutorial.
/// </summary>
public readonly record struct StepId
{
    private StepId(Guid value)
    {
        Value = value;
    }

    /// <summary>
    /// Gets the GUID value used for durable storage boundaries.
    /// </summary>
    public Guid Value { get; }

    /// <summary>
    /// Creates a new step identifier.
    /// </summary>
    public static StepId New() => new(Guid.NewGuid());

    /// <summary>
    /// Reconstitutes a step identifier from a non-empty GUID.
    /// </summary>
    public static StepId Create(Guid value) => value == Guid.Empty
        ? throw new ArgumentException("A step identifier cannot be empty.", nameof(value))
        : new StepId(value);

    /// <summary>
    /// Parses a step identifier persisted as a GUID.
    /// </summary>
    public static StepId Parse(string value) => Create(Guid.Parse(value));

    /// <summary>
    /// Tries to parse a non-empty step identifier persisted as a GUID.
    /// </summary>
    public static bool TryParse(string? value, out StepId stepId)
    {
        if (Guid.TryParse(value, out var guid) && guid != Guid.Empty)
        {
            stepId = new StepId(guid);
            return true;
        }

        stepId = default;
        return false;
    }
}
