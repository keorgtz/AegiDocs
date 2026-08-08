using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using AegiDocs.Domain.Serialization;
using AegiDocs.Infrastructure.Storage.Migrations;
using AegiDocs.Infrastructure.Storage.Persistence;

namespace AegiDocs.Infrastructure.Storage.Serialization;

/// <summary>Bounded JSON serialization for current-schema project manifest DTOs.</summary>
public sealed class ProjectManifestSerializer
{
    /// <summary>Maximum UTF-8 bytes accepted before JSON parsing or node allocation.</summary>
    public const int MaximumManifestBytes = 8 * 1024 * 1024;
    private const int MaximumJsonDepth = 64;
    private const int MaximumStringLength = 16_384;
    private const int MaximumArrayElements = 25_000;
    private const int MaximumPropertyCount = 128;

    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = null,
        DictionaryKeyPolicy = null,
        DefaultIgnoreCondition = JsonIgnoreCondition.Never,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
        WriteIndented = true,
    };

    private readonly ManifestMigrationPipeline migrationPipeline;

    public ProjectManifestSerializer(ManifestMigrationPipeline? migrationPipeline = null)
    {
        this.migrationPipeline = migrationPipeline ?? new ManifestMigrationPipeline();
    }

    /// <summary>Serializes a current manifest with stable DTO member names.</summary>
    public static string Serialize(ProjectManifestDto manifest)
    {
        ArgumentNullException.ThrowIfNull(manifest);
        var json = JsonSerializer.Serialize(manifest, Options);
        var bytes = Encoding.UTF8.GetBytes(json);
        if (bytes.Length > MaximumManifestBytes || !ReadShallowHeaderAndValidateBudget(bytes).IsValid)
        {
            throw new ArgumentOutOfRangeException(nameof(manifest), "The serialized manifest exceeds the configured size limit.");
        }

        return json;
    }

    /// <summary>Performs shallow schema preflight, bounded migration, then current DTO deserialization.</summary>
    public ManifestDeserializationResult Deserialize(ReadOnlySpan<byte> utf8Json)
    {
        if (utf8Json.Length > MaximumManifestBytes)
        {
            return ManifestDeserializationResult.Failure(ManifestDeserializationStatus.ManifestTooLarge);
        }

        if (HasZipSignature(utf8Json))
        {
            return ManifestDeserializationResult.Failure(ManifestDeserializationStatus.ArchiveUnsupported);
        }

        try
        {
            var shallow = ReadShallowHeaderAndValidateBudget(utf8Json);
            if (!shallow.IsValid)
            {
                return ManifestDeserializationResult.Failure(shallow.Status);
            }

            var preflight = migrationPipeline.Preflight(shallow.SchemaVersion);
            if (preflight is ManifestMigrationStatus.InvalidSchema or ManifestMigrationStatus.UnsupportedFutureSchema)
            {
                return ManifestDeserializationResult.Failure(ManifestDeserializationStatus.UnsupportedSchema);
            }

            var node = JsonNode.Parse(utf8Json.ToArray()) as JsonObject;
            if (node is null)
            {
                return ManifestDeserializationResult.Failure(ManifestDeserializationStatus.InvalidJson);
            }

            var migrated = migrationPipeline.Migrate(node);
            if (!migrated.Succeeded || migrated.Manifest is null)
            {
                return ManifestDeserializationResult.Failure(ManifestDeserializationStatus.MigrationFailed);
            }

            var manifest = JsonSerializer.Deserialize<ProjectManifestDto>(migrated.Manifest.ToJsonString(), Options);
            return manifest is null
                ? ManifestDeserializationResult.Failure(ManifestDeserializationStatus.InvalidJson)
                : ManifestDeserializationResult.Success(manifest);
        }
        catch (JsonException)
        {
            return ManifestDeserializationResult.Failure(ManifestDeserializationStatus.InvalidJson);
        }
    }

    /// <summary>Encodes a serializer result as UTF-8 for bounded in-memory callers.</summary>
    public static byte[] SerializeUtf8(ProjectManifestDto manifest) => Encoding.UTF8.GetBytes(Serialize(manifest));

    private static bool HasZipSignature(ReadOnlySpan<byte> bytes) => bytes.Length >= 4 && bytes[0] == (byte)'P' && bytes[1] == (byte)'K' && bytes[2] is 3 or 5 or 7 && bytes[3] is 4 or 6 or 8;

    private static ShallowHeader ReadShallowHeaderAndValidateBudget(ReadOnlySpan<byte> bytes)
    {
        var reader = new Utf8JsonReader(bytes, new JsonReaderOptions { MaxDepth = MaximumJsonDepth, CommentHandling = JsonCommentHandling.Disallow });
        var containers = new Stack<JsonContainer>();
        var rootStarted = false;
        string? rootProperty = null;
        int? schemaVersion = null;
        long? generation = null;
        var schemaSeen = false;
        var generationSeen = false;

        while (reader.Read())
        {
            if (IsArrayElement(reader.TokenType) && containers.TryPeek(out var parent) && parent.IsArray)
            {
                parent.ElementCount++;
                if (parent.ElementCount > MaximumArrayElements)
                {
                    return ShallowHeader.ResourceLimit();
                }
            }

            if (!rootStarted)
            {
                if (reader.TokenType != JsonTokenType.StartObject)
                {
                    return ShallowHeader.Invalid();
                }

                rootStarted = true;
                containers.Push(new JsonContainer(false));
                continue;
            }

            if (reader.TokenType == JsonTokenType.StartObject)
            {
                containers.Push(new JsonContainer(false));
                continue;
            }

            if (reader.TokenType == JsonTokenType.EndObject)
            {
                containers.Pop();
                rootProperty = null;
                continue;
            }

            if (reader.TokenType == JsonTokenType.StartArray)
            {
                containers.Push(new JsonContainer(true));
                continue;
            }

            if (reader.TokenType == JsonTokenType.EndArray)
            {
                containers.Pop();
                rootProperty = null;
                continue;
            }

            if (reader.TokenType == JsonTokenType.PropertyName)
            {
                var current = containers.Peek();
                current.PropertyCount++;
                if (current.PropertyCount > MaximumPropertyCount)
                {
                    return ShallowHeader.ResourceLimit();
                }

                var name = reader.GetString();
                if (name is null || name.Length > MaximumStringLength)
                {
                    return ShallowHeader.ResourceLimit();
                }

                rootProperty = containers.Count == 1 ? name : null;
                continue;
            }

            if (reader.TokenType == JsonTokenType.String && (reader.GetString()?.Length ?? 0) > MaximumStringLength)
            {
                return ShallowHeader.ResourceLimit();
            }

            if (containers.Count == 1 && rootProperty == ProjectSchema.VersionPropertyName)
            {
                if (schemaSeen || reader.TokenType != JsonTokenType.Number || !reader.TryGetInt32(out var value))
                {
                    return ShallowHeader.Invalid();
                }

                schemaSeen = true;
                schemaVersion = value;
            }
            else if (containers.Count == 1 && rootProperty == "generation")
            {
                if (generationSeen || reader.TokenType != JsonTokenType.Number || !reader.TryGetInt64(out var value) || value <= 0)
                {
                    return ShallowHeader.Invalid();
                }

                generationSeen = true;
                generation = value;
            }

            if (reader.TokenType is not JsonTokenType.PropertyName)
            {
                rootProperty = null;
            }
        }

        return rootStarted && schemaVersion.HasValue && generation.HasValue
            ? ShallowHeader.Valid(schemaVersion.Value)
            : ShallowHeader.Invalid();
    }

    private static bool IsArrayElement(JsonTokenType tokenType) => tokenType is JsonTokenType.StartObject or JsonTokenType.StartArray or JsonTokenType.String or JsonTokenType.Number or JsonTokenType.True or JsonTokenType.False or JsonTokenType.Null;

    private sealed class JsonContainer(bool isArray)
    {
        public bool IsArray { get; } = isArray;
        public int PropertyCount { get; set; }
        public int ElementCount { get; set; }
    }

    private readonly record struct ShallowHeader(bool IsValid, int SchemaVersion, ManifestDeserializationStatus Status)
    {
        public static ShallowHeader Valid(int schemaVersion) => new(true, schemaVersion, ManifestDeserializationStatus.Success);
        public static ShallowHeader Invalid() => new(false, 0, ManifestDeserializationStatus.InvalidJson);
        public static ShallowHeader ResourceLimit() => new(false, 0, ManifestDeserializationStatus.ResourceLimitExceeded);
    }
}
