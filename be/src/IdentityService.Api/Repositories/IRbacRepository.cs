namespace IdentityService.Api.Repositories;

using IdentityService.Api.Entities;

public interface IRbacRepository
{
    // Principal
    Task<Principal?> GetPrincipalByIdAsync(Guid principalId, CancellationToken cancellationToken = default);
    Task<Principal?> GetPrincipalByIdAsync(Guid principalId, bool includeUsers, bool includeGroups, CancellationToken cancellationToken = default);
    Task<Principal> CreatePrincipalAsync(Principal principal, CancellationToken cancellationToken = default);
    Task<IEnumerable<Principal>> GetAllPrincipalsAsync(CancellationToken cancellationToken = default);

    // Scope
    Task<Scope> CreateScopeAsync(Scope scope, CancellationToken cancellationToken = default);
    Task<Scope?> GetScopeByIdAsync(Guid scopeId, CancellationToken cancellationToken = default);
    Task<Scope?> GetScopeByTypeAsync(ScopeType type, CancellationToken cancellationToken = default);
    Task<IEnumerable<Scope>> GetAllScopesAsync(CancellationToken cancellationToken = default);
    Task<Scope> GetOrCreateDefaultScopeAsync(CancellationToken cancellationToken = default);

    // Role
    Task<Role> CreateRoleAsync(Role role, CancellationToken cancellationToken = default);
    Task<Role?> GetRoleByIdAsync(Guid roleId, CancellationToken cancellationToken = default);
    Task<Role?> GetRoleByIdWithPermissionsAsync(Guid roleId, CancellationToken cancellationToken = default);
    Task<Role?> GetRoleByNameAsync(string name, CancellationToken cancellationToken = default);
    Task<bool> RoleExistsByNameAsync(string name, CancellationToken cancellationToken = default);
    Task<IEnumerable<Role>> GetAllRolesAsync(CancellationToken cancellationToken = default);
    Task DeleteRoleAsync(Role role, CancellationToken cancellationToken = default);

    // Permission
    Task<Permission> CreatePermissionAsync(Permission permission, CancellationToken cancellationToken = default);
    Task<Permission?> GetPermissionByIdAsync(Guid permissionId, CancellationToken cancellationToken = default);
    Task<Permission?> GetPermissionByNameAsync(string name, CancellationToken cancellationToken = default);
    Task<bool> PermissionExistsByNameAsync(string name, CancellationToken cancellationToken = default);
    Task<IEnumerable<Permission>> GetAllPermissionsAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<Permission>> GetPermissionsByIdsAsync(IEnumerable<Guid> permissionIds, CancellationToken cancellationToken = default);
    Task DeletePermissionAsync(Permission permission, CancellationToken cancellationToken = default);

    // RolePermission
    Task<RolePermission> CreateRolePermissionAsync(RolePermission rolePermission, CancellationToken cancellationToken = default);
    Task<RolePermission?> GetRolePermissionAsync(Guid roleId, Guid permissionId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Permission>> GetPermissionsByRoleIdAsync(Guid roleId, CancellationToken cancellationToken = default);
    Task<IEnumerable<RolePermission>> GetRolePermissionsByRoleIdAsync(Guid roleId, CancellationToken cancellationToken = default);
    Task SetRolePermissionsAsync(Guid roleId, IEnumerable<Guid> permissionIds, CancellationToken cancellationToken = default);
    Task DeleteRolePermissionAsync(RolePermission rolePermission, CancellationToken cancellationToken = default);

    // RoleAssignment
    Task<RoleAssignment> CreateRoleAssignmentAsync(RoleAssignment roleAssignment, CancellationToken cancellationToken = default);
    Task<RoleAssignment?> GetRoleAssignmentByIdAsync(Guid assignmentId, CancellationToken cancellationToken = default);
    Task<RoleAssignment?> GetRoleAssignmentAsync(Guid principalId, Guid roleId, CancellationToken cancellationToken = default);
    Task<RoleAssignment?> GetRoleAssignmentAsync(Guid principalId, Guid roleId, Guid scopeId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Role>> GetRolesByPrincipalIdAsync(Guid principalId, CancellationToken cancellationToken = default);
    Task<IEnumerable<RoleAssignment>> GetRoleAssignmentsByPrincipalIdAsync(Guid principalId, CancellationToken cancellationToken = default);
    Task<IEnumerable<RoleAssignment>> GetRoleAssignmentsByRoleIdAsync(Guid roleId, CancellationToken cancellationToken = default);
    Task DeleteRoleAssignmentAsync(RoleAssignment roleAssignment, CancellationToken cancellationToken = default);

    // Evaluation & Candidate Selection
    Task<IEnumerable<string>> GetPermissionsForPrincipalAsync(Guid principalId, Guid? scopeId = null, CancellationToken cancellationToken = default);
    Task<bool> HasPermissionAsync(Guid principalId, string permissionName, Guid? scopeId = null, CancellationToken cancellationToken = default);
    Task<IEnumerable<User>> GetUsersForPrincipalSelectionAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<Group>> GetGroupsForPrincipalSelectionAsync(CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}