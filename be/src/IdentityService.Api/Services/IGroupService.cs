using IdentityService.Api.DTOs.Groups;

namespace IdentityService.Api.Services;

public interface IGroupService
{
    Task<GroupResponse> CreateAsync(CreateGroupReq request, CancellationToken cancellationToken);
    Task<bool> SetMemberAsync(
        Guid groupId,
        Guid userId,
        bool isMember,
        CancellationToken cancellationToken);
}
