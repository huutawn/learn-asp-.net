using IdentityService.Api.DTOs.Groups;

namespace IdentityService.Api.Services;

public interface IGroupService
{
    Task<GroupResponse> CreateAsync(CreateGroupReq request, Guid actorUserId, CancellationToken cancellationToken);
}
