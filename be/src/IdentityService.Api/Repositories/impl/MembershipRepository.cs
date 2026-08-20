using IdentityService.Api.Data;
using IdentityService.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace IdentityService.Api.Repositories;

public sealed class MembershipRepository(ApplicationDbContext dbContext) : IMembershipRepository
{
    public Task<Guid?> GetPrincipalIdAsync(
        PrincipalType type,
        Guid resourceId,
        CancellationToken cancellationToken) => type switch
        {
            PrincipalType.Group => dbContext.Groups
                .Where(x => x.Id == resourceId)
                .Select(x => (Guid?)x.PrincipalId)
                .SingleOrDefaultAsync(cancellationToken),
            PrincipalType.Team => dbContext.Teams
                .Where(x => x.Id == resourceId)
                .Select(x => (Guid?)x.PrincipalId)
                .SingleOrDefaultAsync(cancellationToken),
            PrincipalType.Project => dbContext.Projects
                .Where(x => x.Id == resourceId)
                .Select(x => (Guid?)x.PrincipalId)
                .SingleOrDefaultAsync(cancellationToken),
            _ => Task.FromResult<Guid?>(null)
        };

    public Task<bool> UserExistsAsync(Guid userId, CancellationToken cancellationToken) =>
        dbContext.Users.AnyAsync(x => x.Id == userId, cancellationToken);

    public Task<PrincipalMembership?> GetAsync(
        Guid userId,
        Guid principalId,
        CancellationToken cancellationToken) =>
        dbContext.PrincipalMemberships.FindAsync([userId, principalId], cancellationToken).AsTask();

    public async Task<IReadOnlyList<User>> GetActiveUsersAsync(
        Guid principalId,
        CancellationToken cancellationToken) =>
        await dbContext.PrincipalMemberships.AsNoTracking()
            .Where(x => x.PrincipalId == principalId && x.LeftAtUtc == null)
            .Select(x => x.User)
            .OrderBy(x => x.DisplayName)
            .ToListAsync(cancellationToken);

    public void Add(PrincipalMembership membership) => dbContext.PrincipalMemberships.Add(membership);

    public Task SaveChangesAsync(CancellationToken cancellationToken) =>
        dbContext.SaveChangesAsync(cancellationToken);
}
