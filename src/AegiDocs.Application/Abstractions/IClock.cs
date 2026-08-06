namespace AegiDocs.Application.Abstractions;

/// <summary>
/// Provides the current time in Coordinated Universal Time (UTC).
/// </summary>
public interface IClock
{
    /// <summary>
    /// Gets the current UTC time.
    /// </summary>
    DateTimeOffset UtcNow { get; }
}
