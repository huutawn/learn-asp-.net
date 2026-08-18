using IdentityService.Api.DTOs.Users;
namespace IdentityService.Api.Services;

public interface IUserService
{
    Task<UserResponse?> GetByIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<PagedUsersResponse> GetPageAsync(
        UserListQuery query,
        CancellationToken cancellationToken = default);
    Task<bool> UpdateRoleAsync(
        Guid actorUserId,
        Guid userId,
        UpdateUserRoleRequest request,
        CancellationToken cancellationToken = default);
}
