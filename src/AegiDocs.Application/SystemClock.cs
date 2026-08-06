using AegiDocs.Application.Abstractions;

namespace AegiDocs.Application;

/// <summary>
/// Provides UTC time from the system clock.
/// </summary>
public sealed class SystemClock : IClock
{
    /// <inheritdoc />
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
