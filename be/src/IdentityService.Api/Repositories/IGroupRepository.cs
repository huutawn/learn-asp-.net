using IdentityService.Api.Entities;

namespace IdentityService.Api.Repositories;

public interface IGroupRepository
{
    Task<bool> ExistsByNameAndTypeAsync(string name, string type, CancellationToken cancellationToken);
    Task AddAsync(Group group, CancellationToken cancellationToken);
}
