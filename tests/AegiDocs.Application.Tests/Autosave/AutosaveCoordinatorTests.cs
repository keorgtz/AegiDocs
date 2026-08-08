using AegiDocs.Application.Autosave;
using AegiDocs.Application.Projects;
using AegiDocs.Domain.Identifiers;
using AegiDocs.Domain.Projects;
using Xunit;

namespace AegiDocs.Application.Tests.Autosave;

public sealed class AutosaveCoordinatorTests
{
    [Fact]
    public void ConstructorRejectsInvalidDebounce()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new AutosaveCoordinator(new FakeRepository(), new ImmediateScheduler(), TimeSpan.FromMinutes(6)));
    }

    [Fact]
    public async Task ScheduleAsyncCoalescesObsoleteSnapshot()
    {
        var scheduler = new GateScheduler();
        var repository = new FakeRepository();
        var coordinator = new AutosaveCoordinator(repository, scheduler, TimeSpan.Zero);
        var project = CreateProject();

        var first = coordinator.ScheduleAsync(project, CancellationToken.None).AsTask();
        var second = coordinator.ScheduleAsync(project, CancellationToken.None).AsTask();
        scheduler.Release();

        var statuses = await Task.WhenAll(first, second);

        Assert.Contains(AutosaveStatus.Coalesced, statuses);
        Assert.Contains(AutosaveStatus.Saved, statuses);
        Assert.Equal(1, repository.SaveCount);
    }

    [Fact]
    public async Task ScheduleAsyncPublishesCanceledWhenDelayIsCanceled()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var coordinator = new AutosaveCoordinator(new FakeRepository(), new ImmediateScheduler(), TimeSpan.Zero);

        var status = await coordinator.ScheduleAsync(CreateProject(), cancellation.Token);

        Assert.Equal(AutosaveStatus.Canceled, status);
    }

    [Fact]
    public async Task ScheduleAsyncAllowsDifferentProjectsToWriteConcurrently()
    {
        var repository = new BlockingRepository();
        var coordinator = new AutosaveCoordinator(repository, new ImmediateScheduler(), TimeSpan.Zero);

        var first = coordinator.ScheduleAsync(CreateProject(), CancellationToken.None).AsTask();
        var second = coordinator.ScheduleAsync(CreateProject(), CancellationToken.None).AsTask();
        await repository.WaitForTwoWritesAsync();
        repository.Release();

        await Task.WhenAll(first, second);
        Assert.Equal(2, repository.MaximumConcurrentWrites);
    }

    private static DocumentProject CreateProject()
    {
        var timestamp = new DateTimeOffset(2026, 8, 7, 0, 0, 0, TimeSpan.Zero);
        return new DocumentProject(ProjectId.New(), new ProjectMetadata("Proyecto", "es-MX", timestamp, timestamp, 1));
    }

    private sealed class ImmediateScheduler : IAutosaveScheduler
    {
        public ValueTask DelayAsync(TimeSpan delay, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return ValueTask.CompletedTask;
        }
    }

    private sealed class GateScheduler : IAutosaveScheduler
    {
        private readonly TaskCompletionSource gate = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public ValueTask DelayAsync(TimeSpan delay, CancellationToken cancellationToken) => new(gate.Task.WaitAsync(cancellationToken));
        public void Release() => gate.TrySetResult();
    }

    private class FakeRepository : IProjectRepository
    {
        public int SaveCount { get; private set; }
        public ValueTask<Result<ProjectRepositoryOperation>> CreateAsync(ProjectCreateRequest request, CancellationToken cancellationToken) => throw new NotSupportedException();
        public ValueTask<Result<DocumentProject>> OpenAsync(ProjectRepositoryIdentity identity, CancellationToken cancellationToken) => throw new NotSupportedException();
        public virtual ValueTask<Result<ProjectRepositoryOperation>> SaveAsync(ProjectSaveRequest request, CancellationToken cancellationToken)
        {
            SaveCount++;
            return ValueTask.FromResult(Result<ProjectRepositoryOperation>.Success(new ProjectRepositoryOperation(request.Identity, true, false)));
        }
        public ValueTask<Result<ProjectRepositoryOperation>> SaveAsAsync(ProjectSaveRequest request, CancellationToken cancellationToken) => throw new NotSupportedException();
    }

    private sealed class BlockingRepository : FakeRepository
    {
        private readonly TaskCompletionSource twoWrites = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource release = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private int activeWrites;
        public int MaximumConcurrentWrites { get; private set; }

        public override async ValueTask<Result<ProjectRepositoryOperation>> SaveAsync(ProjectSaveRequest request, CancellationToken cancellationToken)
        {
            var active = Interlocked.Increment(ref activeWrites);
            MaximumConcurrentWrites = Math.Max(MaximumConcurrentWrites, active);
            if (active == 2) twoWrites.TrySetResult();
            await release.Task.WaitAsync(cancellationToken);
            Interlocked.Decrement(ref activeWrites);
            return Result<ProjectRepositoryOperation>.Success(new ProjectRepositoryOperation(request.Identity, true, false));
        }

        public Task WaitForTwoWritesAsync() => twoWrites.Task;
        public void Release() => release.TrySetResult();
    }
}
