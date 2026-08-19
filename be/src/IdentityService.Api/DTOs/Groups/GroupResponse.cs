namespace IdentityService.Api.DTOs.Groups;

using IdentityService.Api.DTOs.Users;

public sealed record GroupResponse(
    Guid Id,
    Guid PrincipalId,
    string Name,
    string? Description,
    string Type,
    Guid? ScopeId);
public sealed record UserGroupResponse(
    Guid Id,
    Guid PrincipalId,
    string Name,
    string? Description,
    string Type,
    List<UserResponse> Users
);
