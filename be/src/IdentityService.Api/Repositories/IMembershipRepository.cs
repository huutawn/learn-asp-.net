using IdentityService.Api.Entities;

namespace IdentityService.Api.Repositories;

public interface IMembershipRepository
{
    Task<Guid?> GetPrincipalIdAsync(PrincipalType type, Guid resourceId, CancellationToken cancellationToken);
    Task<bool> UserExistsAsync(Guid userId, CancellationToken cancellationToken);
    Task<PrincipalMembership?> GetAsync(Guid userId, Guid principalId, CancellationToken cancellationToken);
    Task<IReadOnlyList<(PrincipalMembership Membership, User User)>> GetActiveUsersAsync(Guid principalId, CancellationToken cancellationToken);
    Task<bool> IsAdminAsync(Guid userId, CancellationToken cancellationToken);
    Task<bool> HasPermissionAsync(Guid userId, string permission, Guid resourcePrincipalId, CancellationToken cancellationToken);
    Task<Guid?> GetUserPrincipalIdAsync(Guid userId, CancellationToken cancellationToken);
    Task<HashSet<string>> GetPermissionsAsync(Guid userId, Guid resourcePrincipalId, CancellationToken cancellationToken);
    Task<IReadOnlyList<string>> GetPermissionNamesByIdsAsync(IEnumerable<Guid> permissionIds, CancellationToken cancellationToken);
    Task<IReadOnlyList<string>> GetRolePermissionNamesByIdsAsync(IEnumerable<Guid> roleIds, CancellationToken cancellationToken);
    Task<(IReadOnlyList<Guid> RoleIds, IReadOnlyList<Guid> PermissionIds)> GetAccessAsync(Guid subjectPrincipalId, Guid resourcePrincipalId, CancellationToken cancellationToken);
    Task ReplaceAccessAsync(Guid userId, Guid resourcePrincipalId, IEnumerable<Guid> roleIds, IEnumerable<Guid> permissionIds, CancellationToken cancellationToken);
    Task<bool> HasAnotherOwnerAsync(Guid resourcePrincipalId, Guid excludedUserId, CancellationToken cancellationToken);
    void Add(PrincipalMembership membership);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
