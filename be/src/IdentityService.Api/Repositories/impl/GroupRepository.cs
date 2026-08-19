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

    public async Task<bool> GroupAndUserExistAsync(
        Guid groupId,
        Guid userId,
        CancellationToken cancellationToken) =>
        await dbContext.Groups.AnyAsync(x => x.Id == groupId, cancellationToken) &&
        await dbContext.Users.AnyAsync(x => x.Id == userId, cancellationToken);
    public async Task<IEnumerable<Group>> GetGroupsByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken) =>
        await dbContext.UserGroups
            .Where(ug => ug.UserId == userId)
            .Select(ug => ug.Group)
            .ToListAsync(cancellationToken);
    public Task<UserGroup?> GetMembershipAsync(
        Guid groupId,
        Guid userId,
        CancellationToken cancellationToken) =>
        dbContext.UserGroups.FindAsync([userId, groupId], cancellationToken).AsTask();

    public void AddMembership(UserGroup membership) => dbContext.UserGroups.Add(membership);

    public Task SaveChangesAsync(CancellationToken cancellationToken) =>
        dbContext.SaveChangesAsync(cancellationToken);
}
