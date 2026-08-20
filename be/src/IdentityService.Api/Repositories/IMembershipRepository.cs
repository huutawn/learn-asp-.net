using IdentityService.Api.Entities;

namespace IdentityService.Api.Repositories;

public interface IMembershipRepository
{
    Task<Guid?> GetPrincipalIdAsync(PrincipalType type, Guid resourceId, CancellationToken cancellationToken);
    Task<bool> UserExistsAsync(Guid userId, CancellationToken cancellationToken);
    Task<PrincipalMembership?> GetAsync(Guid userId, Guid principalId, CancellationToken cancellationToken);
    Task<IReadOnlyList<User>> GetActiveUsersAsync(Guid principalId, CancellationToken cancellationToken);
    void Add(PrincipalMembership membership);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
