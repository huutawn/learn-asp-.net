using IdentityService.Api.Entities;

namespace IdentityService.Api.Repositories;

public interface IUserRepository
{
    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<bool> ExistsByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<User> CreateAsync(User user, CancellationToken cancellationToken = default);
    Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<(IReadOnlyList<User> Items, int TotalCount)> GetPageAsync(
        int skip,
        int take,
        CancellationToken cancellationToken = default);
    Task<bool> UpdateRoleAsync(
        Guid id,
        UserRole role,
        DateTimeOffset updatedAtUtc,
        CancellationToken cancellationToken = default);
}
