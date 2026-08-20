namespace IdentityService.Api.Services;

using IdentityService.Api.DTOs.Rbac;

public interface IRbacService
{
    // Principal
    Task<PrincipalResponse?> GetPrincipalByIdAsync(Guid principalId, CancellationToken cancellationToken = default);
    Task<PrincipalSearchResponse> SearchPrincipalsAsync(PrincipalSearchQuery query, CancellationToken cancellationToken = default);
    Task SetPrincipalAvailabilityAsync(Guid principalId, bool available, CancellationToken cancellationToken = default);

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
    Task<CheckPermissionResponse> CheckPermissionAsync(Guid principalId, string permissionName, Guid? resourcePrincipalId = null, CancellationToken cancellationToken = default);
    Task<PrincipalPermissionsResponse> GetPermissionsForPrincipalAsync(Guid principalId, Guid? resourcePrincipalId = null, CancellationToken cancellationToken = default);
}
