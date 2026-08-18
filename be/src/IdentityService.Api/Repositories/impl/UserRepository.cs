using IdentityService.Api.Entities;
using IdentityService.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace IdentityService.Api.Repositories;

public sealed class UserRepository(ApplicationDbContext dbContext) : IUserRepository
{
    public async Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        return await dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Email == email, cancellationToken);
    }

    public async Task<bool> ExistsByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        return await dbContext.Users
            .AsNoTracking()
            .AnyAsync(u => u.Email == email, cancellationToken);
    }

    public async Task<User> CreateAsync(User user, CancellationToken cancellationToken = default)
    {
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync(cancellationToken);
        return user;
    }

    public async Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
    }

    public async Task<(IReadOnlyList<User> Items, int TotalCount)> GetPageAsync(
        int skip,
        int take,
        CancellationToken cancellationToken = default)
    {
        var totalCount = await dbContext.Users.CountAsync(cancellationToken);
        var items = await dbContext.Users
            .AsNoTracking()
            .OrderBy(u => u.CreatedAtUtc)
            .ThenBy(u => u.Id)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task<bool> UpdateRoleAsync(
        Guid id,
        UserRole role,
        DateTimeOffset updatedAtUtc,
        CancellationToken cancellationToken = default)
    {
        var affectedRows = await dbContext.Users
            .Where(u => u.Id == id)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(u => u.Role, role)
                    .SetProperty(u => u.UpdatedAtUtc, updatedAtUtc),
                cancellationToken);

        return affectedRows == 1;
    }
}
