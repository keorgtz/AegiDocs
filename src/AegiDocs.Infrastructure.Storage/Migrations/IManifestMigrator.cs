using System.Text.Json.Nodes;

namespace AegiDocs.Infrastructure.Storage.Migrations;

/// <summary>
/// Applies one forward-only, adjacent schema migration to a bounded manifest JSON object.
/// </summary>
/// <remarks>
/// Implementations must be deterministic, side-effect free, and idempotent when
/// invoked through <see cref="ManifestMigrationPipeline"/>. They receive an object
/// representation so they can preserve unknown compatible JSON while renaming or
/// removing explicit properties. They must not access assets, files, network
/// resources, or logging sinks containing manifest content.
/// </remarks>
public interface IManifestMigrator
{
    /// <summary>
    /// Gets the positive source schema version.
    /// </summary>
    int FromVersion { get; }

    /// <summary>
    /// Gets the adjacent positive target schema version.
    /// </summary>
    int ToVersion { get; }

    /// <summary>
    /// Produces JSON at <see cref="ToVersion"/> from JSON at <see cref="FromVersion"/>.
    /// The pipeline has already applied header and resource budgets before this call.
    /// </summary>
    JsonObject Migrate(JsonObject manifest);
}
