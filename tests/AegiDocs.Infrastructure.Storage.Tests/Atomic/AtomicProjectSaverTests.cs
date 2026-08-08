using AegiDocs.Infrastructure.Storage.Atomic;
using Xunit;

namespace AegiDocs.Infrastructure.Storage.Tests.Atomic;

public sealed class AtomicProjectSaverTests
{
    [Fact]
    public async Task AcquireFailuresReturnTypedResults()
    {
        using var source = new CancellationTokenSource();
        source.Cancel();
        var canceled = await new AtomicProjectSaver(new Fake { AcquireFailure = new OperationCanceledException(source.Token) }).SaveAsync(Request(), source.Token);
        var storage = await new AtomicProjectSaver(new Fake { AcquireFailure = new IOException() }).SaveAsync(Request(), default);
        Assert.Equal(AtomicSaveStatus.Canceled, canceled.Status);
        Assert.Equal(AtomicSaveStatus.StorageFailure, storage.Status);
    }

    [Fact]
    public async Task DisposeFailureIsTypedBeforeCommitAndDeferredAfterCommit()
    {
        var pre = await new AtomicProjectSaver(new Fake { StageManifestFailure = new IOException(), DisposeFailure = new IOException() }).SaveAsync(Request(), default);
        var post = await new AtomicProjectSaver(new Fake { DisposeFailure = new IOException() }).SaveAsync(Request(), default);
        Assert.Equal(AtomicSaveStatus.StorageFailure, pre.Status);
        Assert.False(pre.Committed);
        Assert.Equal(AtomicSaveStatus.CleanupDeferred, post.Status);
        Assert.True(post.Committed);
        Assert.True(post.Published);
    }

    [Fact]
    public async Task SaveAsHandlesDestinationRaceAndPostPublishDispose()
    {
        var race = await new AtomicProjectSaver(new Fake { SiblingPublished = false }).CreateOrSaveAsAsync("sibling", "canonical", default);
        var post = await new AtomicProjectSaver(new Fake { DisposeFailure = new IOException() }).CreateOrSaveAsAsync("sibling", "canonical", default);
        Assert.Equal(AtomicSaveStatus.DestinationExists, race.Status);
        Assert.Equal(AtomicSaveStatus.CleanupDeferred, post.Status);
        Assert.True(post.Published);
    }

    [Fact]
    public async Task CommitUsesNonCancelableTokenAndCanonicalRoot()
    {
        using var source = new CancellationTokenSource();
        var fake = new Fake();
        await new AtomicProjectSaver(fake).SaveAsync(new("canonical-root", [], "m", false), source.Token);
        Assert.False(fake.CommitToken!.Value.CanBeCanceled);
        Assert.Equal("canonical-root", fake.AcquiredRoot);
    }

    private static AtomicSaveRequest Request() => new("canonical", [], "m", false);

    private sealed class Fake : IAtomicSavePrimitives
    {
        public Exception? AcquireFailure { get; init; }
        public Exception? StageManifestFailure { get; init; }
        public Exception? DisposeFailure { get; init; }
        public bool SiblingPublished { get; init; } = true;
        public string? AcquiredRoot { get; private set; }
        public CancellationToken? CommitToken { get; private set; }
        public ValueTask<IAsyncDisposable> AcquireWriterAsync(string root, CancellationToken cancellationToken)
        {
            if (AcquireFailure is not null) throw AcquireFailure;
            AcquiredRoot = root;
            return ValueTask.FromResult<IAsyncDisposable>(new Lease(DisposeFailure));
        }
        public ValueTask<StagedWrite> StageAssetAsync(StagedAsset asset, CancellationToken cancellationToken) => ValueTask.FromResult(new StagedWrite("a"));
        public ValueTask<StagedWrite> StageManifestAsync(string temporaryName, CancellationToken cancellationToken)
        {
            if (StageManifestFailure is not null) throw StageManifestFailure;
            return ValueTask.FromResult(new StagedWrite("m"));
        }
        public ValueTask FlushAndCloseAsync(StagedWrite staged, CancellationToken cancellationToken) => ValueTask.CompletedTask;
        public ValueTask PublishImmutableAssetAsync(StagedWrite staged, string publishedName, CancellationToken cancellationToken) => ValueTask.CompletedTask;
        public ValueTask CommitManifestAsync(StagedWrite staged, bool hasPublishedManifest, CancellationToken cancellationToken) { CommitToken = cancellationToken; return ValueTask.CompletedTask; }
        public ValueTask CleanupDeferredAsync() => ValueTask.CompletedTask;
        public ValueTask<bool> PublishSiblingRootAsync(string siblingTemporaryRoot, string destinationRoot, CancellationToken cancellationToken) => ValueTask.FromResult(SiblingPublished);
    }

    private sealed class Lease(Exception? failure) : IAsyncDisposable
    {
        public ValueTask DisposeAsync()
        {
            if (failure is not null) throw failure;
            return ValueTask.CompletedTask;
        }
    }
}
