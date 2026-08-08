using AegiDocs.Infrastructure.Storage.Persistence;

namespace AegiDocs.Infrastructure.Storage.Serialization;

public sealed record ManifestDeserializationResult
{
    private ManifestDeserializationResult(ManifestDeserializationStatus status, ProjectManifestDto? manifest) => (Status, Manifest) = (status, manifest);
    public ManifestDeserializationStatus Status { get; }
    public ProjectManifestDto? Manifest { get; }
    public bool Succeeded => Status == ManifestDeserializationStatus.Success;
    public static ManifestDeserializationResult Success(ProjectManifestDto manifest) => new(ManifestDeserializationStatus.Success, manifest);
    public static ManifestDeserializationResult Failure(ManifestDeserializationStatus status) => new(status, null);
}
