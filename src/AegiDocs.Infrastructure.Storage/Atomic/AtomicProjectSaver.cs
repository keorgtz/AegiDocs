namespace AegiDocs.Infrastructure.Storage.Atomic;

using System.Security;

public sealed class AtomicProjectSaver
{
    private readonly IAtomicSavePrimitives primitives;
    public AtomicProjectSaver(IAtomicSavePrimitives primitives) => this.primitives = primitives ?? throw new ArgumentNullException(nameof(primitives));
    public async ValueTask<AtomicSaveResult> SaveAsync(AtomicSaveRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        IAsyncDisposable? lease = null;
        var committed = false;
        var published = false;
        var result = AtomicSaveResult.Failure(AtomicSaveStatus.StorageFailure, committed, published);
        try
        {
            lease = await primitives.AcquireWriterAsync(request.CanonicalRoot, cancellationToken).ConfigureAwait(false);
            foreach (var asset in request.Assets)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var staged = await primitives.StageAssetAsync(asset, cancellationToken).ConfigureAwait(false);
                await primitives.FlushAndCloseAsync(staged, cancellationToken).ConfigureAwait(false);
                await primitives.PublishImmutableAssetAsync(staged, asset.PublishedName, cancellationToken).ConfigureAwait(false);
            }
            cancellationToken.ThrowIfCancellationRequested();
            var manifest = await primitives.StageManifestAsync(request.ManifestTemporaryName, cancellationToken).ConfigureAwait(false);
            await primitives.FlushAndCloseAsync(manifest, cancellationToken).ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
            await primitives.CommitManifestAsync(manifest, request.HasPublishedManifest, CancellationToken.None).ConfigureAwait(false);
            committed = true;
            published = true;
            try { await primitives.CleanupDeferredAsync().ConfigureAwait(false); }
            catch (IOException) { result = AtomicSaveResult.CleanupDeferred(committed, published); }
            catch (UnauthorizedAccessException) { result = AtomicSaveResult.CleanupDeferred(committed, published); }
            catch (SecurityException) { result = AtomicSaveResult.CleanupDeferred(committed, published); }
            if (result.Status != AtomicSaveStatus.CleanupDeferred) result = AtomicSaveResult.Success(committed, published);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { result = AtomicSaveResult.Failure(AtomicSaveStatus.Canceled, committed, published); }
        catch (IOException) { result = AtomicSaveResult.Failure(AtomicSaveStatus.StorageFailure, committed, published); }
        catch (UnauthorizedAccessException) { result = AtomicSaveResult.Failure(AtomicSaveStatus.StorageFailure, committed, published); }
        catch (SecurityException) { result = AtomicSaveResult.Failure(AtomicSaveStatus.StorageFailure, committed, published); }
        finally
        {
            if (lease is not null)
            {
                try { await lease.DisposeAsync().ConfigureAwait(false); }
                catch (IOException) { result = committed || published ? AtomicSaveResult.CleanupDeferred(committed, published) : AtomicSaveResult.Failure(AtomicSaveStatus.StorageFailure, committed, published); }
                catch (UnauthorizedAccessException) { result = committed || published ? AtomicSaveResult.CleanupDeferred(committed, published) : AtomicSaveResult.Failure(AtomicSaveStatus.StorageFailure, committed, published); }
                catch (SecurityException) { result = committed || published ? AtomicSaveResult.CleanupDeferred(committed, published) : AtomicSaveResult.Failure(AtomicSaveStatus.StorageFailure, committed, published); }
            }
        }
        return result;
    }
    public async ValueTask<AtomicSaveResult> CreateOrSaveAsAsync(string siblingTemporaryRoot, string destinationRoot, CancellationToken cancellationToken)
    {
        IAsyncDisposable? lease = null;
        var published = false;
        var result = AtomicSaveResult.Failure(AtomicSaveStatus.StorageFailure, false, published);
        try { lease = await primitives.AcquireWriterAsync(destinationRoot, cancellationToken).ConfigureAwait(false); cancellationToken.ThrowIfCancellationRequested(); published = await primitives.PublishSiblingRootAsync(siblingTemporaryRoot, destinationRoot, cancellationToken).ConfigureAwait(false); result = published ? AtomicSaveResult.Success(false, true) : AtomicSaveResult.Failure(AtomicSaveStatus.DestinationExists, false, false); }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { result = AtomicSaveResult.Failure(AtomicSaveStatus.Canceled, false, published); }
        catch (IOException) { result = AtomicSaveResult.Failure(AtomicSaveStatus.StorageFailure, false, published); }
        catch (UnauthorizedAccessException) { result = AtomicSaveResult.Failure(AtomicSaveStatus.StorageFailure, false, published); }
        catch (SecurityException) { result = AtomicSaveResult.Failure(AtomicSaveStatus.StorageFailure, false, published); }
        finally { if (lease is not null) try { await lease.DisposeAsync().ConfigureAwait(false); } catch (IOException) { result = published ? AtomicSaveResult.CleanupDeferred(false, true) : AtomicSaveResult.Failure(AtomicSaveStatus.StorageFailure, false, false); } catch (UnauthorizedAccessException) { result = published ? AtomicSaveResult.CleanupDeferred(false, true) : AtomicSaveResult.Failure(AtomicSaveStatus.StorageFailure, false, false); } catch (SecurityException) { result = published ? AtomicSaveResult.CleanupDeferred(false, true) : AtomicSaveResult.Failure(AtomicSaveStatus.StorageFailure, false, false); } }
        return result;
    }
}
public sealed record AtomicSaveRequest(string CanonicalRoot, IReadOnlyList<StagedAsset> Assets, string ManifestTemporaryName, bool HasPublishedManifest);
public sealed record StagedAsset(string TemporaryName, string PublishedName);
public sealed record StagedWrite(string Token);
public enum AtomicSaveStatus { Success, CleanupDeferred, Canceled, DestinationExists, StorageFailure }
public sealed record AtomicSaveResult { private AtomicSaveResult(AtomicSaveStatus status, bool committed, bool published) => (Status, Committed, Published) = (status, committed, published); public AtomicSaveStatus Status { get; } public bool Committed { get; } public bool Published { get; } public bool Succeeded => Status is AtomicSaveStatus.Success or AtomicSaveStatus.CleanupDeferred; public static AtomicSaveResult Success(bool committed, bool published) => new(AtomicSaveStatus.Success, committed, published); public static AtomicSaveResult CleanupDeferred(bool committed, bool published) => new(AtomicSaveStatus.CleanupDeferred, committed, published); public static AtomicSaveResult Failure(AtomicSaveStatus status, bool committed, bool published) => new(status, committed, published); }
public interface IAtomicSavePrimitives
{
    ValueTask<IAsyncDisposable> AcquireWriterAsync(string canonicalRoot, CancellationToken cancellationToken);
    ValueTask<StagedWrite> StageAssetAsync(StagedAsset asset, CancellationToken cancellationToken);
    ValueTask<StagedWrite> StageManifestAsync(string temporaryName, CancellationToken cancellationToken);
    ValueTask FlushAndCloseAsync(StagedWrite staged, CancellationToken cancellationToken);
    ValueTask PublishImmutableAssetAsync(StagedWrite staged, string publishedName, CancellationToken cancellationToken);
    ValueTask CommitManifestAsync(StagedWrite staged, bool hasPublishedManifest, CancellationToken cancellationToken);
    ValueTask CleanupDeferredAsync();
    /// <summary>Atomically publishes a same-volume sibling root without overwrite; false means a racing destination exists.</summary>
    ValueTask<bool> PublishSiblingRootAsync(string temporaryRoot, string destinationRoot, CancellationToken cancellationToken);
}
