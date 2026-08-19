namespace IdentityService.Api.Services;

using IdentityService.Api.DTOs.Rbac;

public interface IRbacService
{
    // Principal
    Task<PrincipalResponse> CreatePrincipalAsync(CreatePrincipalReq req, CancellationToken cancellationToken = default);
    Task<PrincipalResponse?> GetPrincipalByIdAsync(Guid principalId, CancellationToken cancellationToken = default);
    Task<IEnumerable<PrincipalResponse>> GetAllPrincipalsAsync(CancellationToken cancellationToken = default);
    Task<PrincipalForAddMemberResponse> GetPrincipalsForAddMemberAsync(CancellationToken cancellationToken = default);
    Task AddMemberPrincipalAsync(AddMemberPrincipalReq req, CancellationToken cancellationToken = default);

    // Scope
    Task<ScopeResponse> CreateScopeAsync(CreateScopeReq req, CancellationToken cancellationToken = default);
    Task<ScopeResponse?> GetScopeByIdAsync(Guid scopeId, CancellationToken cancellationToken = default);
    Task<IEnumerable<ScopeResponse>> GetAllScopesAsync(CancellationToken cancellationToken = default);

    // Permission
    Task<PermissionResponse> CreatePermissionAsync(CreatePermissionReq req, CancellationToken cancellationToken = default);
    Task<PermissionResponse?> GetPermissionByIdAsync(Guid permissionId, CancellationToken cancellationToken = default);
    Task<IEnumerable<PermissionResponse>> GetAllPermissionsAsync(CancellationToken cancellationToken = default);
    Task<bool> DeletePermissionAsync(Guid permissionId, CancellationToken cancellationToken = default);

    // Role
    Task<RoleResponse> CreateRoleAsync(CreateRoleReq req, CancellationToken cancellationToken = default);
    Task<RoleResponse?> GetRoleByIdAsync(Guid roleId, CancellationToken cancellationToken = default);
    Task<IEnumerable<RoleResponse>> GetAllRolesAsync(CancellationToken cancellationToken = default);
    Task<RoleResponse> AssignPermissionsToRoleAsync(Guid roleId, AssignPermissionsToRoleReq req, CancellationToken cancellationToken = default);
    Task<bool> DeleteRoleAsync(Guid roleId, CancellationToken cancellationToken = default);

    // RoleAssignment
    Task<RoleAssignmentResponse> CreateRoleAssignmentAsync(CreateRoleAssignmentReq req, CancellationToken cancellationToken = default);
    Task<IEnumerable<RoleAssignmentResponse>> GetRoleAssignmentsByPrincipalIdAsync(Guid principalId, CancellationToken cancellationToken = default);
    Task<IEnumerable<RoleAssignmentResponse>> GetRoleAssignmentsByRoleIdAsync(Guid roleId, CancellationToken cancellationToken = default);
    Task<bool> DeleteRoleAssignmentAsync(Guid assignmentId, CancellationToken cancellationToken = default);

    // Authorization evaluation
    Task<CheckPermissionResponse> CheckPermissionAsync(Guid principalId, string permissionName, Guid? scopeId = null, CancellationToken cancellationToken = default);
    Task<PrincipalPermissionsResponse> GetPermissionsForPrincipalAsync(Guid principalId, Guid? scopeId = null, CancellationToken cancellationToken = default);
}