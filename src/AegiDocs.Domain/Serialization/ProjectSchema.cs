namespace AegiDocs.Domain.Serialization;

/// <summary>
/// Defines the stable, serializer-independent contract for persisted AegiDocs projects.
/// </summary>
/// <remarks>
/// Storage owns DTO definitions and JSON configuration. It must serialize DTOs rather
/// than domain records, especially the polymorphic <c>Annotation</c> hierarchy. DTOs
/// must use the property and discriminator values defined here verbatim; they are part
/// of the durable project contract and must not be renamed during refactoring.
/// </remarks>
public static class ProjectSchema
{
    /// <summary>
    /// Gets the first published internal project schema version.
    /// </summary>
    public const int InitialVersion = 1;

    /// <summary>
    /// Gets the latest schema version understood by this application build.
    /// </summary>
    public static int CurrentVersion => InitialVersion;

    /// <summary>
    /// Gets the stable DTO property name that carries the schema version.
    /// </summary>
    public const string VersionPropertyName = "schemaVersion";

    /// <summary>
    /// Gets the stable DTO property name that identifies a polymorphic annotation.
    /// </summary>
    public const string AnnotationDiscriminatorPropertyName = "kind";

    /// <summary>
    /// Gets the stable discriminator for a numbered marker DTO.
    /// </summary>
    public const string NumberedMarkerAnnotationKind = "numbered-marker";

    /// <summary>
    /// Gets the stable discriminator for a rectangle DTO.
    /// </summary>
    public const string RectangleAnnotationKind = "rectangle";

    /// <summary>
    /// Gets the stable discriminator for an arrow DTO.
    /// </summary>
    public const string ArrowAnnotationKind = "arrow";

    /// <summary>
    /// Gets the stable discriminator for a text DTO.
    /// </summary>
    public const string TextAnnotationKind = "text";

    /// <summary>
    /// Gets the stable discriminator for a privacy redaction DTO.
    /// </summary>
    public const string RedactionAnnotationKind = "redaction";

    /// <summary>
    /// Classifies whether a persisted project version can be read by this build.
    /// </summary>
    /// <remarks>
    /// A future version is rejected before DTO mapping or asset access. When the
    /// current version increases, older positive versions require an explicit,
    /// idempotent migration owned by Storage before domain mapping occurs.
    /// </remarks>
    public static SchemaReadCompatibility GetReadCompatibility(int schemaVersion) => schemaVersion switch
    {
        <= 0 => SchemaReadCompatibility.Invalid,
        var version when version == CurrentVersion => SchemaReadCompatibility.Current,
        var version when version < CurrentVersion => SchemaReadCompatibility.RequiresMigration,
        _ => SchemaReadCompatibility.UnsupportedFuture,
    };

    /// <summary>
    /// Rejects a version that cannot be read without an explicit Storage migration.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">The version is missing or invalid.</exception>
    /// <exception cref="NotSupportedException">The project was written by a newer application version.</exception>
    public static void EnsureReadable(int schemaVersion)
    {
        switch (GetReadCompatibility(schemaVersion))
        {
            case SchemaReadCompatibility.Current:
            case SchemaReadCompatibility.RequiresMigration:
                return;
            case SchemaReadCompatibility.Invalid:
                throw new ArgumentOutOfRangeException(nameof(schemaVersion), schemaVersion, "A persisted schema version must be positive.");
            case SchemaReadCompatibility.UnsupportedFuture:
                throw new NotSupportedException("The project uses a newer schema version and cannot be opened safely by this application version.");
            default:
                throw new InvalidOperationException("The schema compatibility result is not recognized.");
        }
    }
}
