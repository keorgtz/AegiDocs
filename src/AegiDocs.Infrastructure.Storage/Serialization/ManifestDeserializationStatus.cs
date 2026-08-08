namespace AegiDocs.Infrastructure.Storage.Serialization;

public enum ManifestDeserializationStatus { Success, ManifestTooLarge, UnsupportedSchema, MigrationFailed, InvalidJson, ArchiveUnsupported, ResourceLimitExceeded }
