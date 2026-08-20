using IdentityService.Api.Data;
using IdentityService.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace IdentityService.Api.Repositories;

public sealed class TeamRepository(ApplicationDbContext dbContext) : ITeamRepository
{
    public Task<bool> ExistsByNameAsync(string name, Guid? exceptId, CancellationToken cancellationToken) =>
        dbContext.Teams.AnyAsync(x => x.Name == name && (!exceptId.HasValue || x.Id != exceptId.Value), cancellationToken);

    public async Task AddAsync(Team team, CancellationToken cancellationToken)
    {
        dbContext.Teams.Add(team);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public Task<Team?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        dbContext.Teams.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, cancellationToken);

    public Task<Team?> GetForUpdateAsync(Guid id, CancellationToken cancellationToken) =>
        dbContext.Teams.Include(x => x.Principal).SingleOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task DeleteAsync(Team team, CancellationToken cancellationToken)
    {
        dbContext.Principals.Remove(team.Principal);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken) => dbContext.SaveChangesAsync(cancellationToken);
}
