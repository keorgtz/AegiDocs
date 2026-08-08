using System.Text.Json.Serialization;
using AegiDocs.Domain.Serialization;

namespace AegiDocs.Infrastructure.Storage.Persistence;

/// <summary>
/// Persisted manifest DTO. Storage maps it explicitly to Domain; it is not a domain record.
/// </summary>
/// <remarks>
/// V1 rejects unknown properties after validating <see cref="SchemaVersion"/>. It does not
/// preserve extension data: preserving unknown future fields could publish data this build
/// cannot validate. A future compatible schema requires an explicit migrator and DTO update.
/// </remarks>
public sealed class ProjectManifestDto
{
    [JsonPropertyName(ProjectSchema.VersionPropertyName)] public int SchemaVersion { get; init; }
    [JsonPropertyName("generation")] public long Generation { get; init; }
    [JsonPropertyName("project")] public ProjectDto Project { get; init; } = new();
    [JsonPropertyName("assets")] public IReadOnlyList<AssetReferenceDto> Assets { get; init; } = [];
}

public sealed class ProjectDto
{
    [JsonPropertyName("id")] public string Id { get; init; } = string.Empty;
    [JsonPropertyName("name")] public string Name { get; init; } = string.Empty;
    [JsonPropertyName("languageTag")] public string LanguageTag { get; init; } = string.Empty;
    [JsonPropertyName("createdAt")] public DateTimeOffset CreatedAt { get; init; }
    [JsonPropertyName("modifiedAt")] public DateTimeOffset ModifiedAt { get; init; }
    [JsonPropertyName("tutorials")] public IReadOnlyList<TutorialDto> Tutorials { get; init; } = [];
}

public sealed class TutorialDto
{
    [JsonPropertyName("id")] public string Id { get; init; } = string.Empty;
    [JsonPropertyName("order")] public int Order { get; init; }
    [JsonPropertyName("title")] public string Title { get; init; } = string.Empty;
    [JsonPropertyName("description")] public string? Description { get; init; }
    [JsonPropertyName("audience")] public string? Audience { get; init; }
    [JsonPropertyName("steps")] public IReadOnlyList<StepDto> Steps { get; init; } = [];
}

public sealed class StepDto
{
    [JsonPropertyName("id")] public string Id { get; init; } = string.Empty;
    [JsonPropertyName("order")] public int Order { get; init; }
    [JsonPropertyName("title")] public string Title { get; init; } = string.Empty;
    [JsonPropertyName("description")] public string? Description { get; init; }
    [JsonPropertyName("asset")] public AssetReferenceDto Asset { get; init; } = new();
    [JsonPropertyName("capture")] public CaptureDto Capture { get; init; } = new();
    [JsonPropertyName("interaction")] public InteractionDto Interaction { get; init; } = new();
    [JsonPropertyName("annotations")] public IReadOnlyList<AnnotationDto> Annotations { get; init; } = [];
}

public sealed class CaptureDto
{
    [JsonPropertyName("pixelWidth")] public int PixelWidth { get; init; }
    [JsonPropertyName("pixelHeight")] public int PixelHeight { get; init; }
    [JsonPropertyName("dpiX")] public double DpiX { get; init; }
    [JsonPropertyName("dpiY")] public double DpiY { get; init; }
    [JsonPropertyName("monitorId")] public string MonitorId { get; init; } = string.Empty;
    [JsonPropertyName("capturedAt")] public DateTimeOffset CapturedAt { get; init; }
}

public sealed class InteractionDto
{
    [JsonPropertyName("type")] public string Type { get; init; } = string.Empty;
    [JsonPropertyName("button")] public string Button { get; init; } = string.Empty;
    [JsonPropertyName("x")] public double X { get; init; }
    [JsonPropertyName("y")] public double Y { get; init; }
    [JsonPropertyName("occurredAt")] public DateTimeOffset OccurredAt { get; init; }
    [JsonPropertyName("sequence")] public long Sequence { get; init; }
}

/// <summary>
/// A flat discriminated-union DTO. Serializer and mapper implementation are deferred.
/// </summary>
public sealed class AnnotationDto
{
    [JsonPropertyName("id")] public string Id { get; init; } = string.Empty;
    [JsonPropertyName("zIndex")] public int ZIndex { get; init; }
    [JsonPropertyName(ProjectSchema.AnnotationDiscriminatorPropertyName)] public string Kind { get; init; } = string.Empty;
    [JsonPropertyName("x")] public double? X { get; init; }
    [JsonPropertyName("y")] public double? Y { get; init; }
    [JsonPropertyName("width")] public double? Width { get; init; }
    [JsonPropertyName("height")] public double? Height { get; init; }
    [JsonPropertyName("startX")] public double? StartX { get; init; }
    [JsonPropertyName("startY")] public double? StartY { get; init; }
    [JsonPropertyName("endX")] public double? EndX { get; init; }
    [JsonPropertyName("endY")] public double? EndY { get; init; }
    [JsonPropertyName("number")] public int? Number { get; init; }
    [JsonPropertyName("content")] public string? Content { get; init; }
    [JsonPropertyName("style")] public string? Style { get; init; }
}

public sealed class AssetReferenceDto
{
    [JsonPropertyName("assetId")] public string AssetId { get; init; } = string.Empty;
    [JsonPropertyName("revision")] public long Revision { get; init; }
    [JsonPropertyName("relativePath")] public string RelativePath { get; init; } = string.Empty;
}
