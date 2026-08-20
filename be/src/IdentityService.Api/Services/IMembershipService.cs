using IdentityService.Api.Entities;
using IdentityService.Api.DTOs.Users;

namespace IdentityService.Api.Services;

public interface IMembershipService
{
    Task<bool> SetMemberAsync(
        PrincipalType type,
        Guid resourceId,
        Guid userId,
        bool isMember,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<UserResponse>?> GetMembersAsync(
        PrincipalType type,
        Guid resourceId,
        CancellationToken cancellationToken);
}
