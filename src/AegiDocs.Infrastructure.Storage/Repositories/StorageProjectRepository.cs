using AegiDocs.Application;
using AegiDocs.Application.Projects;
using AegiDocs.Domain.Projects;

namespace AegiDocs.Infrastructure.Storage.Repositories;

public interface IStorageProjectRepositoryBackend
{
    ValueTask<DocumentProject?> OpenAsync(ProjectRepositoryIdentity identity, CancellationToken cancellationToken);
    ValueTask<StorageRepositoryWriteOutcome> CreateAsync(ProjectCreateRequest request, CancellationToken cancellationToken);
    ValueTask<StorageRepositoryWriteOutcome> SaveAsync(ProjectSaveRequest request, bool saveAs, CancellationToken cancellationToken);
}

public readonly record struct StorageRepositoryWriteOutcome(bool Committed, bool Deferred, bool DestinationExists);

public sealed class StorageProjectRepository(IStorageProjectRepositoryBackend backend) : IProjectRepository
{
    private readonly IStorageProjectRepositoryBackend backend = backend ?? throw new ArgumentNullException(nameof(backend));

    public async ValueTask<Result<ProjectRepositoryOperation>> CreateAsync(ProjectCreateRequest request, CancellationToken cancellationToken)
    {
        if (request is null) return Result<ProjectRepositoryOperation>.Failure(Error(ApplicationErrorCode.Validation));
        return await WriteAsync(request.Project, new ProjectRepositoryIdentity(request.Project.Id), () => backend.CreateAsync(request, cancellationToken), cancellationToken).ConfigureAwait(false);
    }

    public async ValueTask<Result<DocumentProject>> OpenAsync(ProjectRepositoryIdentity identity, CancellationToken cancellationToken)
    {
        if (identity.ProjectId == default) return Result<DocumentProject>.Failure(Error(ApplicationErrorCode.Validation));
        try
        {
            var project = await backend.OpenAsync(identity, cancellationToken).ConfigureAwait(false);
            return project is null ? Result<DocumentProject>.Failure(Error(ApplicationErrorCode.InputOutput)) : Result<DocumentProject>.Success(project);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { return Result<DocumentProject>.Failure(Error(ApplicationErrorCode.Cancelled)); }
        catch (IOException) { return Result<DocumentProject>.Failure(Error(ApplicationErrorCode.InputOutput)); }
        catch (UnauthorizedAccessException) { return Result<DocumentProject>.Failure(Error(ApplicationErrorCode.Permission)); }
    }

    public ValueTask<Result<ProjectRepositoryOperation>> SaveAsync(ProjectSaveRequest request, CancellationToken cancellationToken)
    {
        if (request is null) return ValueTask.FromResult(Result<ProjectRepositoryOperation>.Failure(Error(ApplicationErrorCode.Validation)));
        return WriteAsync(request.Project, request.Identity, () => backend.SaveAsync(request, false, cancellationToken), cancellationToken);
    }
    public ValueTask<Result<ProjectRepositoryOperation>> SaveAsAsync(ProjectSaveRequest request, CancellationToken cancellationToken)
    {
        if (request is null) return ValueTask.FromResult(Result<ProjectRepositoryOperation>.Failure(Error(ApplicationErrorCode.Validation)));
        return WriteAsync(request.Project, request.Identity, () => backend.SaveAsync(request, true, cancellationToken), cancellationToken);
    }

    private static async ValueTask<Result<ProjectRepositoryOperation>> WriteAsync(DocumentProject? project, ProjectRepositoryIdentity identity, Func<ValueTask<StorageRepositoryWriteOutcome>> write, CancellationToken cancellationToken)
    {
        if (project is null || identity.ProjectId == default || identity.ProjectId != project.Id) return Result<ProjectRepositoryOperation>.Failure(Error(ApplicationErrorCode.Validation));
        try
        {
            var outcome = await write().ConfigureAwait(false);
            if (outcome.DestinationExists) return Result<ProjectRepositoryOperation>.Failure(Error(ApplicationErrorCode.InputOutput));
            if (!outcome.Committed && !outcome.Deferred) return Result<ProjectRepositoryOperation>.Failure(Error(ApplicationErrorCode.InputOutput));
            return Result<ProjectRepositoryOperation>.Success(new ProjectRepositoryOperation(identity, outcome.Committed, outcome.Deferred));
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { return Result<ProjectRepositoryOperation>.Failure(Error(ApplicationErrorCode.Cancelled)); }
        catch (IOException) { return Result<ProjectRepositoryOperation>.Failure(Error(ApplicationErrorCode.InputOutput)); }
        catch (UnauthorizedAccessException) { return Result<ProjectRepositoryOperation>.Failure(Error(ApplicationErrorCode.Permission)); }
    }

    private static ApplicationError Error(ApplicationErrorCode code) => new(code, "No se pudo completar la operación del proyecto.");
}
