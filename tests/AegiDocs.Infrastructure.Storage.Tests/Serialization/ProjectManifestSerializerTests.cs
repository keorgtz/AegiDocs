using System.Text;
using AegiDocs.Domain.Serialization;
using AegiDocs.Infrastructure.Storage.Persistence;
using AegiDocs.Infrastructure.Storage.Serialization;
using Xunit;

namespace AegiDocs.Infrastructure.Storage.Tests.Serialization;

public sealed class ProjectManifestSerializerTests
{
    [Fact]
    public void SerializeProducesCanonicalStableJson()
    {
        var json = ProjectManifestSerializer.Serialize(new ProjectManifestDto { SchemaVersion = 1, Generation = 1, Project = new ProjectDto { Id = "11111111-1111-1111-1111-111111111111", Name = "Manual", LanguageTag = "es-MX", CreatedAt = new DateTimeOffset(2026, 8, 7, 12, 0, 0, TimeSpan.Zero), ModifiedAt = new DateTimeOffset(2026, 8, 7, 12, 0, 0, TimeSpan.Zero) } });
        Assert.Contains("\"schemaVersion\": 1", json); Assert.Contains("2026-08-07T12:00:00+00:00", json); Assert.Contains("\"assets\": []", json);
    }

    [Fact]
    public void DeserializeRejectsUnknownAndFutureSchemaBeforeMapping()
    {
        var serializer = new ProjectManifestSerializer();
        var future = Encoding.UTF8.GetBytes("{\"schemaVersion\":2,\"generation\":1}");
        var unknown = Encoding.UTF8.GetBytes("{\"schemaVersion\":1,\"generation\":1,\"project\":{},\"assets\":[],\"unknown\":true}");
        Assert.Equal(ManifestDeserializationStatus.UnsupportedSchema, serializer.Deserialize(future).Status);
        Assert.Equal(ManifestDeserializationStatus.InvalidJson, serializer.Deserialize(unknown).Status);
    }

    [Fact]
    public void DeserializeRejectsManifestOverBudget()
    {
        var serializer = new ProjectManifestSerializer();
        Assert.Equal(ManifestDeserializationStatus.ManifestTooLarge, serializer.Deserialize(new byte[ProjectManifestSerializer.MaximumManifestBytes + 1]).Status);
    }

    [Fact]
    public void DeserializeRejectsZipSignatureBeforeJsonMaterialization()
    {
        var serializer = new ProjectManifestSerializer();

        Assert.Equal(ManifestDeserializationStatus.ArchiveUnsupported, serializer.Deserialize("PK\x03\x04not-json"u8).Status);
    }

    [Fact]
    public void DeserializeRejectsOversizedStringDuringStreamingPreflight()
    {
        var serializer = new ProjectManifestSerializer();
        var payload = new string('x', 16_385);
        var json = Encoding.UTF8.GetBytes($"{{\"schemaVersion\":1,\"generation\":1,\"title\":\"{payload}\"}}");

        Assert.Equal(ManifestDeserializationStatus.ResourceLimitExceeded, serializer.Deserialize(json).Status);
    }

    [Fact]
    public void DeserializeRejectsDuplicateSchemaHeaderBeforeMapping()
    {
        var serializer = new ProjectManifestSerializer();

        Assert.Equal(ManifestDeserializationStatus.InvalidJson, serializer.Deserialize("{\"schemaVersion\":2,\"schemaVersion\":1,\"generation\":1}"u8).Status);
    }

    [Fact]
    public void DeserializeRejectsArrayExceedingStreamingBudget()
    {
        var serializer = new ProjectManifestSerializer();
        var entries = string.Join(',', Enumerable.Repeat("0", 25_001));
        var json = Encoding.UTF8.GetBytes($"{{\"schemaVersion\":1,\"generation\":1,\"entries\":[{entries}]}}");

        Assert.Equal(ManifestDeserializationStatus.ResourceLimitExceeded, serializer.Deserialize(json).Status);
    }

    [Fact]
    public void AnnotationKindAndSolidStyleRoundTripAsStrings()
    {
        var manifest = new ProjectManifestDto { SchemaVersion = ProjectSchema.CurrentVersion, Generation = 1, Project = new ProjectDto(), Assets = [] };
        var json = ProjectManifestSerializer.Serialize(manifest);
        Assert.Contains(ProjectSchema.VersionPropertyName, json);
    }
}
