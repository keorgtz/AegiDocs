namespace AegiDocs.Domain.Captures;

/// <summary>
/// An opaque technical identifier for the monitor from which a capture was acquired.
/// </summary>
/// <remarks>
/// This value identifies a display endpoint only. It must not contain a user name,
/// window title, process name, path, or any captured content.
/// </remarks>
public sealed record MonitorIdentifier
{
    /// <summary>
    /// Creates a required monitor identifier after trimming surrounding whitespace.
    /// </summary>
    public MonitorIdentifier(string value)
    {
        ArgumentNullException.ThrowIfNull(value);

        Value = value.Trim();
        if (Value.Length == 0)
        {
            throw new ArgumentException("A monitor identifier is required.", nameof(value));
        }
    }

    /// <summary>
    /// Gets the opaque technical monitor identifier.
    /// </summary>
    public string Value { get; }
}
