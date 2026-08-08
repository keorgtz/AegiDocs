using AegiDocs.Domain.Identifiers;
using AegiDocs.Domain.Projects;

namespace AegiDocs.Application.Projects;

public sealed record ProjectRepositoryIdentity(ProjectId ProjectId);
public sealed record ProjectCreateRequest(DocumentProject Project);
public sealed record ProjectSaveRequest(ProjectRepositoryIdentity Identity, DocumentProject Project);
public sealed record ProjectRepositoryOperation(ProjectRepositoryIdentity Identity, bool Committed, bool Deferred);

public interface IProjectRepository
{
    ValueTask<Result<ProjectRepositoryOperation>> CreateAsync(ProjectCreateRequest request, CancellationToken cancellationToken);
    ValueTask<Result<DocumentProject>> OpenAsync(ProjectRepositoryIdentity identity, CancellationToken cancellationToken);
    ValueTask<Result<ProjectRepositoryOperation>> SaveAsync(ProjectSaveRequest request, CancellationToken cancellationToken);
    ValueTask<Result<ProjectRepositoryOperation>> SaveAsAsync(ProjectSaveRequest request, CancellationToken cancellationToken);
}
