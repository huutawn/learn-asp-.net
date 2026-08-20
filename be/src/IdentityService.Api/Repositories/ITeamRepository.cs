using IdentityService.Api.Entities;

namespace IdentityService.Api.Repositories;

public interface ITeamRepository
{
    Task<bool> ExistsByNameAsync(string name, Guid? exceptId, CancellationToken cancellationToken);
    Task AddAsync(Team team, CancellationToken cancellationToken);
    Task<Team?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<Team?> GetForUpdateAsync(Guid id, CancellationToken cancellationToken);
    Task DeleteAsync(Team team, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
