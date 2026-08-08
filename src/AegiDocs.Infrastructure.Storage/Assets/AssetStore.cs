using System.Security.Cryptography;
using AegiDocs.Infrastructure.Storage.Persistence;

namespace AegiDocs.Infrastructure.Storage.Assets;

/// <summary>
/// Writes immutable PNG assets to canonical, derived v1 relative paths.
/// </summary>
public sealed class AssetStore
{
    private const string PngExtension = ".png";
    private readonly IAssetStoreFileSystem fileSystem;

    public AssetStore(IAssetStoreFileSystem fileSystem) => this.fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));

    public async ValueTask<AssetStoreResult> StoreAsync(AssetWriteRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (cancellationToken.IsCancellationRequested)
        {
            return AssetStoreResult.Failure(AssetStoreStatus.Canceled);
        }

        var inputStatus = ValidateRequest(request);
        if (inputStatus is not null)
        {
            return AssetStoreResult.Failure(inputStatus.Value);
        }

        var pngStatus = ValidatePngHeader(request.Content, cancellationToken);
        if (pngStatus is not null)
        {
            return AssetStoreResult.Failure(pngStatus.Value);
        }

        try
        {
            var inventory = await fileSystem.GetInventoryAsync(cancellationToken).ConfigureAwait(false);
            if (inventory.AssetCount < 0 || inventory.TotalBytes < 0)
            {
                return AssetStoreResult.Failure(AssetStoreStatus.StorageFailure);
            }

            if (inventory.AssetCount >= AssetStoreLimits.MaximumAssetCount)
            {
                return AssetStoreResult.Failure(AssetStoreStatus.AssetLimitExceeded);
            }

            if (request.ContentLength > AssetStoreLimits.MaximumTotalAssetBytes - inventory.TotalBytes)
            {
                return AssetStoreResult.Failure(AssetStoreStatus.TotalBytesLimitExceeded);
            }

            string? hash = null;
            if (request.CalculateContentHash)
            {
                hash = await CalculateHashAsync(request.Content, cancellationToken).ConfigureAwait(false);
                var existing = await fileSystem.FindByContentHashAsync(hash, cancellationToken).ConfigureAwait(false);
                if (existing is not null)
                {
                    return AssetStoreResult.Success(AssetStoreStatus.Deduplicated, existing);
                }
            }

            var relativePath = CreateRelativePath(request.AssetId, request.Revision);
            if (await fileSystem.ExistsAsync(relativePath, cancellationToken).ConfigureAwait(false))
            {
                return AssetStoreResult.Failure(AssetStoreStatus.AssetAlreadyExists);
            }

            await fileSystem.WriteAsync(relativePath, request.Content, cancellationToken).ConfigureAwait(false);
            return AssetStoreResult.Success(AssetStoreStatus.Success, CreateReference(request.AssetId, request.Revision, relativePath));
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return AssetStoreResult.Failure(AssetStoreStatus.Canceled);
        }
        catch (IOException)
        {
            return AssetStoreResult.Failure(AssetStoreStatus.StorageFailure);
        }
        catch (UnauthorizedAccessException)
        {
            return AssetStoreResult.Failure(AssetStoreStatus.StorageFailure);
        }
    }

    public static string CreateRelativePath(AegiDocs.Domain.Identifiers.AssetId assetId, long revision)
    {
        if (assetId == default)
        {
            throw new ArgumentException("An asset identifier is required.", nameof(assetId));
        }

        if (revision <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(revision), revision, "An asset revision must be positive.");
        }

        var identifier = assetId.Value.ToString("D").ToLowerInvariant();
        return $"assets/{identifier}/v{revision:D16}{PngExtension}";
    }

    private static AssetStoreStatus? ValidateRequest(AssetWriteRequest request)
    {
        if (request.AssetId == default)
        {
            return AssetStoreStatus.InvalidAssetId;
        }

        if (request.Revision <= 0)
        {
            return AssetStoreStatus.InvalidRevision;
        }

        if (!string.Equals(request.SourceExtension, PngExtension, StringComparison.Ordinal))
        {
            return AssetStoreStatus.UnsupportedExtension;
        }

        if (request.ContentLength > AssetStoreLimits.MaximumAssetBytes)
        {
            return AssetStoreStatus.AssetTooLarge;
        }

        if (request.Content is null || !request.Content.CanRead || !request.Content.CanSeek || request.ContentLength < 0 ||
            request.ContentLength != request.Content.Length - request.Content.Position)
        {
            return AssetStoreStatus.InvalidContent;
        }

        return null;
    }

    private static AssetStoreStatus? ValidatePngHeader(Stream content, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var originalPosition = content.Position;
        try
        {
            Span<byte> header = stackalloc byte[24];
            if (content.Read(header) != header.Length || !header[..8].SequenceEqual(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }) ||
                !header.Slice(12, 4).SequenceEqual("IHDR"u8)) return AssetStoreStatus.InvalidPng;
            var width = System.Buffers.Binary.BinaryPrimitives.ReadInt32BigEndian(header.Slice(16, 4));
            var height = System.Buffers.Binary.BinaryPrimitives.ReadInt32BigEndian(header.Slice(20, 4));
            return width <= 0 || height <= 0 || width > 7680 || height > 4320 || (long)width * height > 33_177_600 ? AssetStoreStatus.InvalidImageDimensions : null;
        }
        finally { content.Position = originalPosition; }
    }

    private static async ValueTask<string> CalculateHashAsync(Stream content, CancellationToken cancellationToken)
    {
        var originalPosition = content.Position;
        try
        {
            var hash = await SHA256.HashDataAsync(content, cancellationToken).ConfigureAwait(false);
            return Convert.ToHexString(hash).ToLowerInvariant();
        }
        finally
        {
            content.Position = originalPosition;
        }
    }

    private static AssetReferenceDto CreateReference(AegiDocs.Domain.Identifiers.AssetId assetId, long revision, string relativePath) => new()
    {
        AssetId = assetId.Value.ToString("D").ToLowerInvariant(),
        Revision = revision,
        RelativePath = relativePath,
    };
}
