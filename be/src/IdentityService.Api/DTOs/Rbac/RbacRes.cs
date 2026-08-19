namespace IdentityService.Api.DTOs.Rbac;

public sealed record RoleResponse(
    Guid Id,
    string Name,
    string? Description,
    IReadOnlyList<PermissionResponse>? Permissions = null);

public sealed record PermissionResponse(
    Guid Id,
    string Name,
    string? Description);

public sealed record ScopeResponse(
    Guid Id,
    string Type);

public sealed record PrincipalResponse(
    Guid Id,
    string Type);

public sealed record PrincipalUserResponse(
    Guid Id,
    string Type,
    string Email,
    string? Name,
    string? Description
);

public sealed record PrincipalGroupResponse(
    Guid Id,
    string Type,
    string Name,
    string? Description
);

public sealed record PrincipalForAddMemberResponse(
    Guid? Id,
    PrincipalUserResponse[] Users,
    PrincipalGroupResponse[] Groups);

public sealed record RoleAssignmentResponse(
    Guid Id,
    Guid RoleId,
    Guid PrincipalId,
    Guid ScopeId,
    string? RoleName = null,
    string? ScopeType = null,
    DateTimeOffset? CreatedAt = null
);

public sealed record CheckPermissionResponse(
    bool HasPermission,
    string PermissionName,
    Guid PrincipalId,
    Guid? ScopeId
);

public sealed record PrincipalPermissionsResponse(
    Guid PrincipalId,
    IReadOnlyList<string> Permissions
);