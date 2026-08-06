namespace AegiDocs.Domain.Captures;

/// <summary>
/// An optional opaque technical reference to a native window.
/// </summary>
/// <remarks>
/// The value is a handle-like technical token only; it is not a user identity,
/// window title, process identifier, process name, or path.
/// </remarks>
public readonly record struct WindowReference
{
    /// <summary>
    /// Creates a non-zero opaque window reference.
    /// </summary>
    public WindowReference(nuint value)
    {
        if (value == 0)
        {
            throw new ArgumentOutOfRangeException(nameof(value), value, "A window reference must be non-zero.");
        }

        Value = value;
    }

    /// <summary>
    /// Gets the opaque native-handle value.
    /// </summary>
    public nuint Value { get; }
}
