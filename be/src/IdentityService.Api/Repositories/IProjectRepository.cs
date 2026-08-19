using IdentityService.Api.Entities;

namespace IdentityService.Api.Repositories;

public interface IProjectRepository
{
    Task AddAsync(Project project, CancellationToken cancellationToken);
    Task<Project?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<Project?> GetForUpdateAsync(Guid id, CancellationToken cancellationToken);
    Task<bool> OwnerExistsAsync(Guid ownerId, CancellationToken cancellationToken);
    Task<bool> ScopeExistsAsync(Guid scopeId, CancellationToken cancellationToken);
    Task DeleteAsync(Project project, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
