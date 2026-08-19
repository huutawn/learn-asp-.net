using IdentityService.Api.Entities;
using IdentityService.Api.Repositories;
using IdentityService.Api.DTOs.Users;
using IdentityService.Api.Exceptions;

namespace IdentityService.Api.Services;

public class UserService : IUserService
{
    private readonly IUserRepository userRepository;

    public UserService(IUserRepository userRepository)
    {
        this.userRepository = userRepository;
    }

    public async Task<UserResponse?> GetByIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await userRepository.GetByIdAsync(userId, cancellationToken);
        return user is null ? null : Map(user);
    }

    public async Task<PagedUsersResponse> GetPageAsync(
        UserListQuery query,
        CancellationToken cancellationToken = default)
    {
        var (users, totalCount) = await userRepository.GetPageAsync(
            (query.Page - 1) * query.PageSize,
            query.PageSize,
            cancellationToken);

        return new PagedUsersResponse(
            users.Select(Map).ToArray(),
            query.Page,
            query.PageSize,
            totalCount);
    }

    public Task<bool> UpdateRoleAsync(
        Guid actorUserId,
        Guid userId,
        UpdateUserRoleRequest request,
        CancellationToken cancellationToken = default)
    {
        if (actorUserId == userId && request.Role != UserRole.Admin)
        {
            throw new ConflictException(
                "Administrators cannot remove their own admin role.");
        }

        return userRepository.UpdateRoleAsync(
            userId,
            request.Role,
            DateTimeOffset.UtcNow,
            cancellationToken);
    }

    private static UserResponse Map(User user) =>
        new(
            user.Id,
            user.PrincipalId,
            user.Email,
            user.DisplayName,
            user.EmailVerified,
            user.Language,
            user.TimeZoneId,
            user.Role,
            user.CreatedAtUtc);
}
