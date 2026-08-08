using AegiDocs.Infrastructure.Storage.Persistence;

namespace AegiDocs.Infrastructure.Storage.Assets;

/// <summary>V1 limits for immutable project capture assets.</summary>
public static class AssetStoreLimits
{
    public const int MaximumAssetCount = 25_000;
    public const long MaximumAssetBytes = 32L * 1024 * 1024;
    public const long MaximumTotalAssetBytes = 8L * 1024 * 1024 * 1024;
}

/// <summary>Describes an input capture without accepting a caller-provided destination path.</summary>
public sealed record AssetWriteRequest(
    AegiDocs.Domain.Identifiers.AssetId AssetId,
    long Revision,
    string SourceExtension,
    Stream Content,
    long ContentLength,
    bool CalculateContentHash);

/// <summary>Safe categories returned by <see cref="AssetStore"/>.</summary>
public enum AssetStoreStatus
{
    Success,
    Deduplicated,
    InvalidAssetId,
    InvalidRevision,
    UnsupportedExtension,
    InvalidContent,
    AssetTooLarge,
    AssetLimitExceeded,
    TotalBytesLimitExceeded,
    AssetAlreadyExists,
    InvalidPng,
    InvalidImageDimensions,
    Canceled,
    StorageFailure,
}

/// <summary>Content-safe asset write result. It never contains an absolute filesystem path.</summary>
public sealed record AssetStoreResult
{
    private AssetStoreResult(AssetStoreStatus status, AssetReferenceDto? asset) => (Status, Asset) = (status, asset);
    public AssetStoreStatus Status { get; }
    public AssetReferenceDto? Asset { get; }
    public bool Succeeded => Status is AssetStoreStatus.Success or AssetStoreStatus.Deduplicated;
    public static AssetStoreResult Success(AssetStoreStatus status, AssetReferenceDto asset) => new(status, asset);
    public static AssetStoreResult Failure(AssetStoreStatus status) => new(status, null);
}

/// <summary>Bounded inventory used to enforce project asset budgets before writing.</summary>
public readonly record struct AssetStoreInventory(int AssetCount, long TotalBytes);

/// <summary>
/// Isolates filesystem and stream persistence so AssetStore remains deterministic in tests.
/// </summary>
/// <remarks>
/// Implementations receive only canonical relative asset paths. Atomic publication, root
/// canonicalization, reparse-point checks, and physical locks are owned by later Storage tasks.
/// </remarks>
public interface IAssetStoreFileSystem
{
    ValueTask<AssetStoreInventory> GetInventoryAsync(CancellationToken cancellationToken);
    ValueTask<bool> ExistsAsync(string relativePath, CancellationToken cancellationToken);
    ValueTask<AssetReferenceDto?> FindByContentHashAsync(string sha256Hex, CancellationToken cancellationToken);
    ValueTask WriteAsync(string relativePath, Stream content, CancellationToken cancellationToken);
}
