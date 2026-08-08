using AegiDocs.Application.Recovery;
using AegiDocs.Infrastructure.Storage.Recovery;
using Xunit;

namespace AegiDocs.Infrastructure.Storage.Tests.Recovery;

public sealed class StorageProjectRecoveryServiceTests
{
    [Fact]
    public async Task FindCandidatesAsyncOrdersValidatedGenerationsAndPrefersMainOnTie()
    {
        var projectId = Guid.NewGuid();
        var store = new FakeStore
        {
            Candidates =
            [
                new(projectId, 3, RecoveryCandidateKind.Recovery),
                new(projectId, 3, RecoveryCandidateKind.Main),
                new(projectId, 3, RecoveryCandidateKind.Backup),
                new(projectId, 0, RecoveryCandidateKind.Main),
                new(Guid.NewGuid(), 10, RecoveryCandidateKind.Main),
            ],
        };
        var service = new StorageProjectRecoveryService(store);

        var result = await service.FindCandidatesAsync(projectId, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal([RecoveryCandidateKind.Main, RecoveryCandidateKind.Backup, RecoveryCandidateKind.Recovery], result.Value.Select(candidate => candidate.Kind));
    }

    [Fact]
    public async Task RestoreAsyncUsesLeaseAndDoesNotOverwriteDestination()
    {
        var candidate = new RecoveryCandidate(Guid.NewGuid(), 4, RecoveryCandidateKind.Recovery);
        var store = new FakeStore { RestoreResult = false };
        var service = new StorageProjectRecoveryService(store);

        var result = await service.RestoreAsync(candidate, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(RecoveryActionStatus.DestinationExists, result.Value);
        Assert.Equal(1, store.LeaseCount);
        Assert.Equal(1, store.RestoreCount);
    }

    [Fact]
    public async Task DiscardAsyncReturnsNotFoundWithoutTreatingItAsFailure()
    {
        var candidate = new RecoveryCandidate(Guid.NewGuid(), 2, RecoveryCandidateKind.Backup);
        var store = new FakeStore { DiscardResult = false };
        var service = new StorageProjectRecoveryService(store);

        var result = await service.DiscardAsync(candidate, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(RecoveryActionStatus.NotFound, result.Value);
    }

    private sealed class FakeStore : IRecoveryStore
    {
        public IReadOnlyList<RecoveryCandidate> Candidates { get; init; } = [];
        public bool RestoreResult { get; init; } = true;
        public bool DiscardResult { get; init; } = true;
        public int LeaseCount { get; private set; }
        public int RestoreCount { get; private set; }

        public ValueTask<IReadOnlyList<RecoveryCandidate>> GetCandidatesAsync(Guid projectId, CancellationToken cancellationToken) => ValueTask.FromResult(Candidates);
        public ValueTask<IAsyncDisposable> AcquireLeaseAsync(Guid projectId, CancellationToken cancellationToken)
        {
            LeaseCount++;
            return ValueTask.FromResult<IAsyncDisposable>(new NoopLease());
        }
        public ValueTask<bool> RestoreWithoutOverwriteAsync(RecoveryCandidate candidate, CancellationToken cancellationToken)
        {
            RestoreCount++;
            return ValueTask.FromResult(RestoreResult);
        }
        public ValueTask<bool> DiscardIfPresentAsync(RecoveryCandidate candidate, CancellationToken cancellationToken) => ValueTask.FromResult(DiscardResult);
    }

    private sealed class NoopLease : IAsyncDisposable
    {
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}
