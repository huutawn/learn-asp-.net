using System.ComponentModel.DataAnnotations;
using IdentityService.Api.Entities;

namespace IdentityService.Api.DTOs.Users;

public sealed record UserResponse(
    Guid Id,
    string Email,
    string DisplayName,
    bool EmailVerified,
    string Language,
    string TimeZoneId,
    UserRole Role,
    DateTimeOffset CreatedAtUtc
);

public sealed record PagedUsersResponse(
    IReadOnlyList<UserResponse> Items,
    int Page,
    int PageSize,
    int TotalCount)
{
    public int TotalPages =>
        (int)Math.Ceiling(TotalCount / (double)PageSize);
}

public sealed record UserListQuery
{
    [Range(1, 1_000_000)]
    public int Page { get; init; } = 1;

    [Range(1, 100)]
    public int PageSize { get; init; } = 20;
}

public sealed record UpdateUserRoleRequest
{
    [EnumDataType(typeof(UserRole))]
    public required UserRole Role { get; init; }
}
