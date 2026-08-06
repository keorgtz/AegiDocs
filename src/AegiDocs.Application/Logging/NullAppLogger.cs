using AegiDocs.Application.Abstractions;

namespace AegiDocs.Application.Logging;

/// <summary>
/// Discards safe diagnostic events when logging is intentionally unavailable, such as in tests.
/// </summary>
public sealed class NullAppLogger : IAppLogger
{
    /// <summary>
    /// Gets the shared no-op logger instance.
    /// </summary>
    public static NullAppLogger Instance { get; } = new();

    private NullAppLogger()
    {
    }

    /// <inheritdoc />
    public void Log(AppLogEvent logEvent)
    {
        ArgumentNullException.ThrowIfNull(logEvent);
    }
}
