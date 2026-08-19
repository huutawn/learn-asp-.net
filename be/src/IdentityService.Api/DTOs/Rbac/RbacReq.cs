namespace IdentityService.Api.DTOs.Rbac;

using System.ComponentModel.DataAnnotations;

public sealed record CreatePrincipalReq(
    [param: Required, MaxLength(100)]
    string Type
);

public sealed record CreateScopeReq(
    [param: Required, MaxLength(100)]
    string Type
);

public sealed record CreatePermissionReq(
    [param: Required, MaxLength(100)]
    string Name,
    [param: MaxLength(1_000)]
    string? Description
);

public sealed record CreateRoleReq(
    [param: Required, MaxLength(100)]
    string Name,
    [param: MaxLength(1_000)]
    string? Description,
    List<Guid>? PermissionIds = null
);

public sealed record UpdateRoleReq(
    [param: Required, MaxLength(100)]
    string Name,
    [param: MaxLength(1_000)]
    string? Description
);

public sealed record AssignPermissionsToRoleReq(
    [param: Required]
    List<Guid> PermissionIds
);

public sealed record CreateRoleAssignmentReq(
    [param: Required]
    Guid RoleId,
    [param: Required]
    Guid PrincipalId,
    Guid? ScopeId = null
);

public sealed record AddMemberPrincipalReq(
    [param: Required]
    Guid PrincipalId,
    [param: Required]
    string Type
);
