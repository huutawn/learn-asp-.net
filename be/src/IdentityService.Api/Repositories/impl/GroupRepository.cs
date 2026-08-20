using IdentityService.Api.Data;
using IdentityService.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace IdentityService.Api.Repositories;

public sealed class GroupRepository(ApplicationDbContext dbContext) : IGroupRepository
{
    public Task<bool> ExistsByNameAndTypeAsync(
        string name,
        string type,
        CancellationToken cancellationToken) =>
        dbContext.Groups.AnyAsync(x => x.Name == name && x.Type == type, cancellationToken);

    public Task<bool> ScopeExistsAsync(Guid scopeId, CancellationToken cancellationToken) =>
        dbContext.Scopes.AnyAsync(x => x.Id == scopeId, cancellationToken);

    public async Task AddAsync(Group group, CancellationToken cancellationToken)
    {
        dbContext.Groups.Add(group);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

}
