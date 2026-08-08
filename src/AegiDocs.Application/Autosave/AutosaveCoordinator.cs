using System.Collections.Concurrent;
using AegiDocs.Application.Projects;
using AegiDocs.Domain.Identifiers;
using AegiDocs.Domain.Projects;

namespace AegiDocs.Application.Autosave;

public interface IAutosaveScheduler
{
    ValueTask DelayAsync(TimeSpan delay, CancellationToken cancellationToken);
}

public enum AutosaveStatus { Saved, Deferred, Coalesced, Canceled, Failed }
public sealed record AutosaveState(ProjectId ProjectId, AutosaveStatus Status);

public sealed class AutosaveCoordinator
{
    private readonly IProjectRepository repository;
    private readonly IAutosaveScheduler scheduler;
    private readonly TimeSpan debounce;
    private readonly ConcurrentDictionary<ProjectId, ProjectState> projects = new();

    public AutosaveCoordinator(IProjectRepository repository, IAutosaveScheduler scheduler, TimeSpan debounce)
    {
        this.repository = repository ?? throw new ArgumentNullException(nameof(repository));
        this.scheduler = scheduler ?? throw new ArgumentNullException(nameof(scheduler));
        if (debounce < TimeSpan.Zero || debounce > TimeSpan.FromMinutes(5)) throw new ArgumentOutOfRangeException(nameof(debounce));
        this.debounce = debounce;
    }

    public event Action<AutosaveState>? StateChanged;

    public async ValueTask<AutosaveStatus> ScheduleAsync(DocumentProject snapshot, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        var state = projects.GetOrAdd(snapshot.Id, static _ => new ProjectState());
        long revision;
        lock (state.Gate) revision = ++state.Revision;
        try { await scheduler.DelayAsync(debounce, cancellationToken).ConfigureAwait(false); }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { return Publish(snapshot.Id, AutosaveStatus.Canceled); }
        lock (state.Gate) if (revision != state.Revision) return Publish(snapshot.Id, AutosaveStatus.Coalesced);
        var writerAcquired = false;
        try
        {
            await state.Writer.WaitAsync(cancellationToken).ConfigureAwait(false);
            writerAcquired = true;
            lock (state.Gate) if (revision != state.Revision) return Publish(snapshot.Id, AutosaveStatus.Coalesced);
            var result = await repository.SaveAsync(new ProjectSaveRequest(new ProjectRepositoryIdentity(snapshot.Id), snapshot), cancellationToken).ConfigureAwait(false);
            return Publish(snapshot.Id, result.IsSuccess ? result.Value!.Deferred ? AutosaveStatus.Deferred : AutosaveStatus.Saved : result.Error!.Code == ApplicationErrorCode.Cancelled ? AutosaveStatus.Canceled : AutosaveStatus.Failed);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { return Publish(snapshot.Id, AutosaveStatus.Canceled); }
        finally
        {
            if (writerAcquired)
            {
                state.Writer.Release();
            }
        }
    }

    private AutosaveStatus Publish(ProjectId projectId, AutosaveStatus status) { StateChanged?.Invoke(new AutosaveState(projectId, status)); return status; }
    private sealed class ProjectState { public object Gate { get; } = new(); public SemaphoreSlim Writer { get; } = new(1, 1); public long Revision { get; set; } }
}
