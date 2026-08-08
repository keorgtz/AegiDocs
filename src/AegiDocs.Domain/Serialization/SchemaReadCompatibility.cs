namespace AegiDocs.Domain.Serialization;

/// <summary>
/// Describes how a persisted schema version relates to this application build.
/// </summary>
public enum SchemaReadCompatibility
{
    /// <summary>
    /// The supplied version is absent or invalid and must be rejected.
    /// </summary>
    Invalid = 0,

    /// <summary>
    /// The supplied version matches the current schema exactly.
    /// </summary>
    Current = 1,

    /// <summary>
    /// The supplied version predates the current schema and needs a Storage migration.
    /// </summary>
    RequiresMigration = 2,

    /// <summary>
    /// The supplied version was written by a newer application and must be rejected safely.
    /// </summary>
    UnsupportedFuture = 3,
}
