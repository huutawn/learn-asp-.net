namespace IdentityService.Api.DTOs.Rbac;

using System.ComponentModel.DataAnnotations;

public sealed record PrincipalSearchQuery(
    string? Type = null,
    string? Search = null,
    string? Cursor = null,
    [param: Range(1, 100)] int Limit = 20,
    bool? Available = true,
    Guid? ScopeId = null);

public sealed record SetPrincipalAvailabilityRequest(bool Available);

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
