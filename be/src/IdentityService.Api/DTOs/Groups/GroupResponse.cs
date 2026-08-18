namespace IdentityService.Api.DTOs.Groups;

using IdentityService.Api.DTOs.Users;

public sealed record GroupResponse(
    Guid Id,
    string Name,
    string? Description,
    string Type);
public sealed record UserGroupResponse(
    Guid Id,
    string Name,
    string? Description,
    string Type,
    List<UserResponse> Users
);
