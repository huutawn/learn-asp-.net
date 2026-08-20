using IdentityService.Api.Entities;
using IdentityService.Api.DTOs.Users;

namespace IdentityService.Api.Services;

public interface IMembershipService
{
    Task<bool> SetMemberAsync(PrincipalType type, Guid resourceId, Guid actorUserId, Guid userId, DTOs.Members.SetMemberRequest request, CancellationToken cancellationToken);

    Task<IReadOnlyList<DTOs.Members.MemberResponse>?> GetMembersAsync(
        PrincipalType type,
        Guid resourceId,
        CancellationToken cancellationToken);
}
