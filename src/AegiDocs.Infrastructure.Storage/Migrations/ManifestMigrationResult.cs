using System.Text.Json.Nodes;

namespace AegiDocs.Infrastructure.Storage.Migrations;

/// <summary>
/// A content-safe result of manifest migration.
/// </summary>
/// <remarks>
/// Failure contains only a category. Callers must not attach manifest text, paths,
/// annotations, or exception messages to diagnostics derived from this result.
/// </remarks>
public sealed record ManifestMigrationResult
{
    private ManifestMigrationResult(ManifestMigrationStatus status, JsonObject? manifest)
    {
        Status = status;
        Manifest = manifest;
    }

    /// <summary>
    /// Gets the outcome category.
    /// </summary>
    public ManifestMigrationStatus Status { get; }

    /// <summary>
    /// Gets the current-schema manifest only on a successful outcome.
    /// </summary>
    public JsonObject? Manifest { get; }

    /// <summary>
    /// Gets whether a current-schema manifest is available.
    /// </summary>
    public bool Succeeded => Status is ManifestMigrationStatus.Current or ManifestMigrationStatus.Migrated;

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    public static ManifestMigrationResult Success(ManifestMigrationStatus status, JsonObject manifest)
    {
        if (status is not ManifestMigrationStatus.Current and not ManifestMigrationStatus.Migrated)
        {
            throw new ArgumentOutOfRangeException(nameof(status), status, "Only successful migration statuses can carry a manifest.");
        }

        ArgumentNullException.ThrowIfNull(manifest);
        return new ManifestMigrationResult(status, manifest);
    }

    /// <summary>
    /// Creates a content-safe failure result.
    /// </summary>
    public static ManifestMigrationResult Failure(ManifestMigrationStatus status)
    {
        if (status is ManifestMigrationStatus.Current or ManifestMigrationStatus.Migrated or ManifestMigrationStatus.MigrationRequired)
        {
            throw new ArgumentOutOfRangeException(nameof(status), status, "Only terminal failure statuses can be returned as failures.");
        }

        return new ManifestMigrationResult(status, null);
    }
}
