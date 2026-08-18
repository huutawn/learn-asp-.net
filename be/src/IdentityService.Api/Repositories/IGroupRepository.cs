using IdentityService.Api.Entities;

namespace IdentityService.Api.Repositories;

public interface IGroupRepository
{
    Task<bool> ExistsByNameAndTypeAsync(string name, string type, CancellationToken cancellationToken);
    Task AddAsync(Group group, CancellationToken cancellationToken);
    Task<bool> GroupAndUserExistAsync(Guid groupId, Guid userId, CancellationToken cancellationToken);
    Task<UserGroup?> GetMembershipAsync(Guid groupId, Guid userId, CancellationToken cancellationToken);
    void AddMembership(UserGroup membership);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
