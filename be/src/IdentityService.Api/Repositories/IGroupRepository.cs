using IdentityService.Api.Entities;

namespace IdentityService.Api.Repositories;

public interface IGroupRepository
{
    Task<bool> ExistsByNameAndTypeAsync(string name, string type, CancellationToken cancellationToken);
    Task<bool> ScopeExistsAsync(Guid scopeId, CancellationToken cancellationToken);
    Task AddAsync(Group group, CancellationToken cancellationToken);
}
