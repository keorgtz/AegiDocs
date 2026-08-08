using AegiDocs.Domain.Serialization;
using System.Text.Json.Nodes;

namespace AegiDocs.Infrastructure.Storage.Migrations;

/// <summary>
/// Validates and runs the complete forward-only migration chain for bounded manifest JSON.
/// </summary>
/// <remarks>
/// A JSON reader must call <see cref="Preflight"/> as soon as it reads the shallow
/// schema header. A future or invalid schema is rejected before parsing deep DTO
/// collections, resolving asset paths, opening files, or decoding images.
/// </remarks>
public sealed class ManifestMigrationPipeline
{
    private readonly Dictionary<int, IManifestMigrator> migrators;

    /// <summary>
    /// Creates a migration pipeline targeting the current Domain schema.
    /// </summary>
    public ManifestMigrationPipeline(IEnumerable<IManifestMigrator>? migrators = null)
    {
        TargetVersion = ProjectSchema.CurrentVersion;
        this.migrators = CreateMigratorMap(migrators);
    }

    /// <summary>
    /// Gets the schema version produced by successful migration.
    /// </summary>
    public int TargetVersion { get; }

    /// <summary>
    /// Classifies a shallow schema header without parsing nested manifest content.
    /// </summary>
    public ManifestMigrationStatus Preflight(int schemaVersion) => schemaVersion switch
    {
        <= 0 => ManifestMigrationStatus.InvalidSchema,
        var version when version > TargetVersion => ManifestMigrationStatus.UnsupportedFutureSchema,
        var version when version == TargetVersion => ManifestMigrationStatus.Current,
        _ => ManifestMigrationStatus.MigrationRequired,
    };

    /// <summary>
    /// Migrates a bounded JSON object only after a successful shallow preflight.
    /// DTO deserialization is deliberately deferred to the serializer.
    /// </summary>
    public ManifestMigrationResult Migrate(JsonObject manifest)
    {
        ArgumentNullException.ThrowIfNull(manifest);

        if (!TryGetInt32(manifest, ProjectSchema.VersionPropertyName, out var schemaVersion))
        {
            return ManifestMigrationResult.Failure(ManifestMigrationStatus.InvalidSchema);
        }

        var status = Preflight(schemaVersion);
        if (status is ManifestMigrationStatus.InvalidSchema or ManifestMigrationStatus.UnsupportedFutureSchema)
        {
            return ManifestMigrationResult.Failure(status);
        }

        if (!TryGetPositiveInt64(manifest, "generation", out var generation))
        {
            return ManifestMigrationResult.Failure(ManifestMigrationStatus.InvalidGeneration);
        }

        if (status == ManifestMigrationStatus.Current)
        {
            return ManifestMigrationResult.Success(ManifestMigrationStatus.Current, manifest);
        }

        var current = manifest.DeepClone().AsObject();
        while (schemaVersion < TargetVersion)
        {
            if (!migrators.TryGetValue(schemaVersion, out var migrator))
            {
                return ManifestMigrationResult.Failure(ManifestMigrationStatus.MigrationFailed);
            }

            try
            {
                current = migrator.Migrate(current);
            }
            catch (Exception)
            {
                return ManifestMigrationResult.Failure(ManifestMigrationStatus.MigrationFailed);
            }

            if (current is null || !IsValidMigratorOutput(current, migrator.ToVersion, generation))
            {
                return ManifestMigrationResult.Failure(ManifestMigrationStatus.MigrationFailed);
            }

            schemaVersion = migrator.ToVersion;
        }

        return ManifestMigrationResult.Success(ManifestMigrationStatus.Migrated, current);
    }

    private static Dictionary<int, IManifestMigrator> CreateMigratorMap(IEnumerable<IManifestMigrator>? configuredMigrators)
    {
        var map = new Dictionary<int, IManifestMigrator>();
        foreach (var migrator in configuredMigrators ?? [])
        {
            ArgumentNullException.ThrowIfNull(migrator);

            if (migrator.FromVersion <= 0 || migrator.ToVersion != migrator.FromVersion + 1 || migrator.ToVersion > ProjectSchema.CurrentVersion)
            {
                throw new ArgumentException("Manifest migrators must move exactly one positive version forward within the target schema.", nameof(configuredMigrators));
            }

            if (!map.TryAdd(migrator.FromVersion, migrator))
            {
                throw new ArgumentException("Only one manifest migrator may own a source schema version.", nameof(configuredMigrators));
            }
        }

        for (var version = ProjectSchema.InitialVersion; version < ProjectSchema.CurrentVersion; version++)
        {
            if (!map.ContainsKey(version))
            {
                throw new ArgumentException("The manifest migration chain must be complete from the initial schema to the target schema.", nameof(configuredMigrators));
            }
        }

        return map;
    }

    /// <summary>
    /// Verifies the two invariants that every individual migrator must preserve.
    /// </summary>
    /// <remarks>
    /// This does not parse or deserialize the manifest. The pipeline invokes it
    /// after every transition so a migrator cannot publish a different generation.
    /// </remarks>
    public static bool IsValidMigratorOutput(JsonObject manifest, int expectedSchemaVersion, long expectedGeneration) =>
        expectedSchemaVersion > 0 &&
        expectedGeneration > 0 &&
        TryGetInt32(manifest, ProjectSchema.VersionPropertyName, out var schemaVersion) &&
        schemaVersion == expectedSchemaVersion &&
        TryGetPositiveInt64(manifest, "generation", out var generation) &&
        generation == expectedGeneration;

    private static bool TryGetInt32(JsonObject manifest, string propertyName, out int value)
    {
        value = default;
        return manifest[propertyName] is JsonValue jsonValue && jsonValue.TryGetValue(out value);
    }

    private static bool TryGetPositiveInt64(JsonObject manifest, string propertyName, out long value)
    {
        value = default;
        return manifest[propertyName] is JsonValue jsonValue && jsonValue.TryGetValue(out value) && value > 0;
    }
}
