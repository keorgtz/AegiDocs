namespace AegiDocs.Application.Logging;

/// <summary>
/// Represents immutable, minimal diagnostic metadata that is safe to pass to a local logger.
/// </summary>
/// <remarks>
/// <para>
/// This type deliberately has no exception, technical-detail, path, window-title, capture, or
/// arbitrary-property field. Its message must describe only the application action or outcome;
/// it must not contain personal data, credentials, captured content, user input, or operating
/// system details.
/// </para>
/// <para>
/// Logging is local by default. An infrastructure implementation must not send this event to a
/// network service unless a future, explicit consented design changes that policy.
/// </para>
/// </remarks>
public sealed record AppLogEvent
{
    private const int MaximumCategoryLength = 64;
    private const int MaximumMessageLength = 512;

    /// <summary>
    /// Initializes a new instance of the <see cref="AppLogEvent" /> class.
    /// </summary>
    public AppLogEvent(
        DateTimeOffset timestamp,
        AppLogLevel level,
        string category,
        string message,
        Guid? correlationId = null)
    {
        if (timestamp == default)
        {
            throw new ArgumentOutOfRangeException(nameof(timestamp), "The timestamp must be specified.");
        }

        if (timestamp.Offset != TimeSpan.Zero)
        {
            throw new ArgumentException("The timestamp must use UTC.", nameof(timestamp));
        }

        if (!Enum.IsDefined(level))
        {
            throw new ArgumentOutOfRangeException(nameof(level), level, "The log level must be defined.");
        }

        ValidateCategory(category);
        ValidateMessage(message);

        Timestamp = timestamp;
        Level = level;
        Category = category;
        Message = message;
        CorrelationId = correlationId;
    }

    /// <summary>
    /// Gets the UTC time at which the event occurred.
    /// </summary>
    public DateTimeOffset Timestamp { get; }

    /// <summary>
    /// Gets the event severity.
    /// </summary>
    public AppLogLevel Level { get; }

    /// <summary>
    /// Gets the stable, non-sensitive subsystem category.
    /// </summary>
    public string Category { get; }

    /// <summary>
    /// Gets a short, non-sensitive description of the application action or outcome.
    /// </summary>
    public string Message { get; }

    /// <summary>
    /// Gets the optional session or operation correlation identifier.
    /// </summary>
    public Guid? CorrelationId { get; }

    private static void ValidateCategory(string category)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(category);

        if (category.Length > MaximumCategoryLength || category.Any(static character =>
                !(char.IsAsciiLetterOrDigit(character) || character is '.' or '-' or '_')))
        {
            throw new ArgumentException(
                "The category must contain at most 64 ASCII letters, digits, '.', '-' or '_'.",
                nameof(category));
        }
    }

    private static void ValidateMessage(string message)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(message);

        if (message.Length > MaximumMessageLength || message.Any(char.IsControl))
        {
            throw new ArgumentException(
                "The message must contain at most 512 characters and no control characters.",
                nameof(message));
        }
    }
}
