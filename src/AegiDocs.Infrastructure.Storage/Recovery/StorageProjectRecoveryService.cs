using AegiDocs.Application;
using AegiDocs.Application.Recovery;

namespace AegiDocs.Infrastructure.Storage.Recovery;

public interface IRecoveryStore
{
    ValueTask<IReadOnlyList<RecoveryCandidate>> GetCandidatesAsync(Guid projectId, CancellationToken cancellationToken);
    ValueTask<IAsyncDisposable> AcquireLeaseAsync(Guid projectId, CancellationToken cancellationToken);
    ValueTask<bool> RestoreWithoutOverwriteAsync(RecoveryCandidate candidate, CancellationToken cancellationToken);
    ValueTask<bool> DiscardIfPresentAsync(RecoveryCandidate candidate, CancellationToken cancellationToken);
}

public sealed class StorageProjectRecoveryService(IRecoveryStore store) : IProjectRecoveryService
{
    private readonly IRecoveryStore store = store ?? throw new ArgumentNullException(nameof(store));
    public async ValueTask<Result<IReadOnlyList<RecoveryCandidate>>> FindCandidatesAsync(Guid projectId, CancellationToken cancellationToken)
    {
        if (projectId == Guid.Empty) return Result<IReadOnlyList<RecoveryCandidate>>.Failure(Error(ApplicationErrorCode.Validation));
        try
        {
            var candidates = (await store.GetCandidatesAsync(projectId, cancellationToken).ConfigureAwait(false))
                .Where(candidate => candidate.ProjectId == projectId && candidate.Generation > 0)
                .OrderByDescending(candidate => candidate.Generation)
                .ThenBy(candidate => candidate.Kind == RecoveryCandidateKind.Main ? 0 : candidate.Kind == RecoveryCandidateKind.Backup ? 1 : 2)
                .ToArray();
            return Result<IReadOnlyList<RecoveryCandidate>>.Success(candidates);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { return Result<IReadOnlyList<RecoveryCandidate>>.Failure(Error(ApplicationErrorCode.Cancelled)); }
        catch (IOException) { return Result<IReadOnlyList<RecoveryCandidate>>.Failure(Error(ApplicationErrorCode.InputOutput)); }
    }
    public ValueTask<Result<RecoveryActionStatus>> RestoreAsync(RecoveryCandidate candidate, CancellationToken cancellationToken) => MutateAsync(candidate, true, cancellationToken);
    public ValueTask<Result<RecoveryActionStatus>> DiscardAsync(RecoveryCandidate candidate, CancellationToken cancellationToken) => MutateAsync(candidate, false, cancellationToken);
    private async ValueTask<Result<RecoveryActionStatus>> MutateAsync(RecoveryCandidate candidate, bool restore, CancellationToken cancellationToken)
    {
        if (candidate.ProjectId == Guid.Empty || candidate.Generation <= 0) return Result<RecoveryActionStatus>.Failure(Error(ApplicationErrorCode.Validation));
        try
        {
            await using var lease = await store.AcquireLeaseAsync(candidate.ProjectId, cancellationToken).ConfigureAwait(false);
            var changed = restore ? await store.RestoreWithoutOverwriteAsync(candidate, cancellationToken).ConfigureAwait(false) : await store.DiscardIfPresentAsync(candidate, cancellationToken).ConfigureAwait(false);
            return Result<RecoveryActionStatus>.Success(changed ? restore ? RecoveryActionStatus.Restored : RecoveryActionStatus.Discarded : restore ? RecoveryActionStatus.DestinationExists : RecoveryActionStatus.NotFound);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { return Result<RecoveryActionStatus>.Failure(Error(ApplicationErrorCode.Cancelled)); }
        catch (IOException) { return Result<RecoveryActionStatus>.Failure(Error(ApplicationErrorCode.InputOutput)); }
        catch (UnauthorizedAccessException) { return Result<RecoveryActionStatus>.Failure(Error(ApplicationErrorCode.Permission)); }
    }
    private static ApplicationError Error(ApplicationErrorCode code) => new(code, "No se pudo completar la recuperación del proyecto.");
}
