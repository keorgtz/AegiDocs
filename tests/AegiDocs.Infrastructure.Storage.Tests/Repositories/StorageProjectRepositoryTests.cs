using AegiDocs.Application;
using AegiDocs.Application.Projects;
using AegiDocs.Domain.Identifiers;
using AegiDocs.Domain.Projects;
using AegiDocs.Infrastructure.Storage.Repositories;
using Xunit;

namespace AegiDocs.Infrastructure.Storage.Tests.Repositories;

public sealed class StorageProjectRepositoryTests
{
    [Fact]
    public async Task SaveAsyncReturnsSuccessfulDeferredOperationAfterCommit()
    {
        var project = CreateProject();
        var backend = new FakeBackend { SaveOutcome = new StorageRepositoryWriteOutcome(true, true, false) };
        var repository = new StorageProjectRepository(backend);

        var result = await repository.SaveAsync(new ProjectSaveRequest(new ProjectRepositoryIdentity(project.Id), project), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(result.Value.Committed);
        Assert.True(result.Value.Deferred);
    }

    [Fact]
    public async Task SaveAsAsyncReturnsSafeInputOutputErrorWhenDestinationExists()
    {
        var project = CreateProject();
        var backend = new FakeBackend { SaveOutcome = new StorageRepositoryWriteOutcome(false, false, true) };
        var repository = new StorageProjectRepository(backend);

        var result = await repository.SaveAsAsync(new ProjectSaveRequest(new ProjectRepositoryIdentity(project.Id), project), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ApplicationErrorCode.InputOutput, result.Error.Code);
        Assert.DoesNotContain("C:\\", result.Error.UserMessage, StringComparison.Ordinal);
    }

    [Fact]
    public async Task OpenAsyncMapsCancelledOperationToSafeCancelledError()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var repository = new StorageProjectRepository(new FakeBackend { OpenException = new OperationCanceledException(cancellation.Token) });

        var result = await repository.OpenAsync(new ProjectRepositoryIdentity(ProjectId.New()), cancellation.Token);

        Assert.False(result.IsSuccess);
        Assert.Equal(ApplicationErrorCode.Cancelled, result.Error.Code);
    }

    private static DocumentProject CreateProject()
    {
        var timestamp = new DateTimeOffset(2026, 8, 7, 0, 0, 0, TimeSpan.Zero);
        return new DocumentProject(ProjectId.New(), new ProjectMetadata("Proyecto", "es-MX", timestamp, timestamp, 1));
    }

    private sealed class FakeBackend : IStorageProjectRepositoryBackend
    {
        public StorageRepositoryWriteOutcome SaveOutcome { get; init; } = new(true, false, false);
        public Exception? OpenException { get; init; }

        public ValueTask<DocumentProject?> OpenAsync(ProjectRepositoryIdentity identity, CancellationToken cancellationToken)
        {
            if (OpenException is not null)
            {
                throw OpenException;
            }

            return ValueTask.FromResult<DocumentProject?>(null);
        }

        public ValueTask<StorageRepositoryWriteOutcome> CreateAsync(ProjectCreateRequest request, CancellationToken cancellationToken) => ValueTask.FromResult(SaveOutcome);

        public ValueTask<StorageRepositoryWriteOutcome> SaveAsync(ProjectSaveRequest request, bool saveAs, CancellationToken cancellationToken) => ValueTask.FromResult(SaveOutcome);
    }
}
