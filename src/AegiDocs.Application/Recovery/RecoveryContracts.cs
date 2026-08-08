namespace AegiDocs.Application.Recovery;

public enum RecoveryCandidateKind { Main, Backup, Recovery }
public sealed record RecoveryCandidate(Guid ProjectId, long Generation, RecoveryCandidateKind Kind);
public enum RecoveryActionStatus { Restored, Discarded, NotFound, DestinationExists, Canceled, Failed }
public interface IProjectRecoveryService
{
    ValueTask<Result<IReadOnlyList<RecoveryCandidate>>> FindCandidatesAsync(Guid projectId, CancellationToken cancellationToken);
    ValueTask<Result<RecoveryActionStatus>> RestoreAsync(RecoveryCandidate candidate, CancellationToken cancellationToken);
    ValueTask<Result<RecoveryActionStatus>> DiscardAsync(RecoveryCandidate candidate, CancellationToken cancellationToken);
}
