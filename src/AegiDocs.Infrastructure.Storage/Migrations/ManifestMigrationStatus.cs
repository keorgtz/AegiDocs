namespace AegiDocs.Infrastructure.Storage.Migrations;

/// <summary>
/// Describes the safe outcome of a manifest schema preflight or migration.
/// </summary>
public enum ManifestMigrationStatus
{
    /// <summary>The manifest already uses the target schema.</summary>
    Current = 0,

    /// <summary>The manifest uses an older schema and can be migrated.</summary>
    MigrationRequired = 1,

    /// <summary>The manifest was successfully migrated to the target schema.</summary>
    Migrated = 2,

    /// <summary>The schema version is absent or invalid.</summary>
    InvalidSchema = 3,

    /// <summary>The manifest was written by a newer application.</summary>
    UnsupportedFutureSchema = 4,

    /// <summary>The manifest generation is absent, non-integral, or not positive.</summary>
    InvalidGeneration = 5,

    /// <summary>A configured migration did not produce a valid adjacent result.</summary>
    MigrationFailed = 6,
}
