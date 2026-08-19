namespace IdentityService.Api.Repositories;

using IdentityService.Api.Data;
using IdentityService.Api.Entities;
using Microsoft.EntityFrameworkCore;

public sealed class RbacRepository(ApplicationDbContext dbContext) : IRbacRepository
{
    // Principal
    public Task<Principal?> GetPrincipalByIdAsync(
        Guid principalId,
        CancellationToken cancellationToken = default) =>
        dbContext.Principals.FindAsync(new object?[] { principalId }, cancellationToken).AsTask();

    public Task<Principal?> GetPrincipalByIdAsync(
        Guid principalId,
        bool includeUsers,
        bool includeGroups,
        CancellationToken cancellationToken = default)
    {
        IQueryable<Principal> query = dbContext.Principals;

        if (includeUsers)
            query = query.Include(p => p.User);

        if (includeGroups)
            query = query.Include(p => p.Group);

        return query.FirstOrDefaultAsync(p => p.Id == principalId, cancellationToken);
    }

    public async Task<Principal> CreatePrincipalAsync(Principal principal, CancellationToken cancellationToken = default)
    {
        dbContext.Principals.Add(principal);
        await dbContext.SaveChangesAsync(cancellationToken);
        return principal;
    }

    public async Task<IEnumerable<Principal>> GetAllPrincipalsAsync(CancellationToken cancellationToken = default) =>
        await dbContext.Principals
            .AsNoTracking()
            .ToListAsync(cancellationToken);

    // Scope
    public async Task<Scope> CreateScopeAsync(Scope scope, CancellationToken cancellationToken = default)
    {
        dbContext.Scopes.Add(scope);
        await dbContext.SaveChangesAsync(cancellationToken);
        return scope;
    }

    public Task<Scope?> GetScopeByIdAsync(Guid scopeId, CancellationToken cancellationToken = default) =>
        dbContext.Scopes.FindAsync(new object?[] { scopeId }, cancellationToken).AsTask();

    public Task<Scope?> GetScopeByTypeAsync(ScopeType type, CancellationToken cancellationToken = default) =>
        dbContext.Scopes.FirstOrDefaultAsync(s => s.Type == type, cancellationToken);

    public async Task<IEnumerable<Scope>> GetAllScopesAsync(CancellationToken cancellationToken = default) =>
        await dbContext.Scopes
            .AsNoTracking()
            .ToListAsync(cancellationToken);

    public async Task<Scope> GetOrCreateDefaultScopeAsync(CancellationToken cancellationToken = default)
    {
        var scope = await dbContext.Scopes.FirstOrDefaultAsync(s => s.Type == ScopeType.System, cancellationToken);
        if (scope is null)
        {
            scope = new Scope
            {
                Id = Guid.NewGuid(),
                Type = ScopeType.System
            };
            dbContext.Scopes.Add(scope);
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        return scope;
    }

    // Role
    public async Task<Role> CreateRoleAsync(Role role, CancellationToken cancellationToken = default)
    {
        dbContext.Roles.Add(role);
        await dbContext.SaveChangesAsync(cancellationToken);
        return role;
    }

    public Task<Role?> GetRoleByIdAsync(Guid roleId, CancellationToken cancellationToken = default) =>
        dbContext.Roles.FindAsync(new object?[] { roleId }, cancellationToken).AsTask();

    public Task<Role?> GetRoleByIdWithPermissionsAsync(Guid roleId, CancellationToken cancellationToken = default) =>
        dbContext.Roles
            .Include(r => r.RolePermissions)
            .ThenInclude(rp => rp.Permission)
            .FirstOrDefaultAsync(r => r.Id == roleId, cancellationToken);

    public Task<Role?> GetRoleByNameAsync(string name, CancellationToken cancellationToken = default) =>
        dbContext.Roles.FirstOrDefaultAsync(r => r.Name.ToLower() == name.ToLower(), cancellationToken);

    public Task<bool> RoleExistsByNameAsync(string name, CancellationToken cancellationToken = default) =>
        dbContext.Roles.AnyAsync(r => r.Name.ToLower() == name.ToLower(), cancellationToken);

    public async Task<IEnumerable<Role>> GetAllRolesAsync(CancellationToken cancellationToken = default) =>
        await dbContext.Roles
            .Include(r => r.RolePermissions)
            .ThenInclude(rp => rp.Permission)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

    public async Task DeleteRoleAsync(Role role, CancellationToken cancellationToken = default)
    {
        dbContext.Roles.Remove(role);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    // Permission
    public async Task<Permission> CreatePermissionAsync(Permission permission, CancellationToken cancellationToken = default)
    {
        dbContext.Permissions.Add(permission);
        await dbContext.SaveChangesAsync(cancellationToken);
        return permission;
    }

    public Task<Permission?> GetPermissionByIdAsync(Guid permissionId, CancellationToken cancellationToken = default) =>
        dbContext.Permissions.FindAsync(new object?[] { permissionId }, cancellationToken).AsTask();

    public Task<Permission?> GetPermissionByNameAsync(string name, CancellationToken cancellationToken = default) =>
        dbContext.Permissions.FirstOrDefaultAsync(p => p.Name.ToLower() == name.ToLower(), cancellationToken);

    public Task<bool> PermissionExistsByNameAsync(string name, CancellationToken cancellationToken = default) =>
        dbContext.Permissions.AnyAsync(p => p.Name.ToLower() == name.ToLower(), cancellationToken);

    public async Task<IEnumerable<Permission>> GetAllPermissionsAsync(CancellationToken cancellationToken = default) =>
        await dbContext.Permissions
            .AsNoTracking()
            .ToListAsync(cancellationToken);

    public async Task<IEnumerable<Permission>> GetPermissionsByIdsAsync(
        IEnumerable<Guid> permissionIds,
        CancellationToken cancellationToken = default) =>
        await dbContext.Permissions
            .Where(p => permissionIds.Contains(p.Id))
            .ToListAsync(cancellationToken);

    public async Task DeletePermissionAsync(Permission permission, CancellationToken cancellationToken = default)
    {
        dbContext.Permissions.Remove(permission);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    // RolePermission
    public async Task<RolePermission> CreateRolePermissionAsync(RolePermission rolePermission, CancellationToken cancellationToken = default)
    {
        dbContext.RolePermissions.Add(rolePermission);
        await dbContext.SaveChangesAsync(cancellationToken);
        return rolePermission;
    }

    public Task<RolePermission?> GetRolePermissionAsync(Guid roleId, Guid permissionId, CancellationToken cancellationToken = default) =>
        dbContext.RolePermissions.FindAsync(new object?[] { roleId, permissionId }, cancellationToken).AsTask();

    public async Task<IEnumerable<Permission>> GetPermissionsByRoleIdAsync(Guid roleId, CancellationToken cancellationToken = default) =>
        await dbContext.RolePermissions
            .Where(rp => rp.RoleId == roleId)
            .Select(rp => rp.Permission)
            .ToListAsync(cancellationToken);

    public async Task<IEnumerable<RolePermission>> GetRolePermissionsByRoleIdAsync(Guid roleId, CancellationToken cancellationToken = default) =>
        await dbContext.RolePermissions
            .Include(rp => rp.Permission)
            .Where(rp => rp.RoleId == roleId)
            .ToListAsync(cancellationToken);

    public async Task SetRolePermissionsAsync(
        Guid roleId,
        IEnumerable<Guid> permissionIds,
        CancellationToken cancellationToken = default)
    {
        var existing = await dbContext.RolePermissions
            .Where(rp => rp.RoleId == roleId)
            .ToListAsync(cancellationToken);

        dbContext.RolePermissions.RemoveRange(existing);

        var toAdd = permissionIds.Distinct().Select(pid => new RolePermission
        {
            RoleId = roleId,
            PermissionId = pid
        });

        await dbContext.RolePermissions.AddRangeAsync(toAdd, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteRolePermissionAsync(RolePermission rolePermission, CancellationToken cancellationToken = default)
    {
        dbContext.RolePermissions.Remove(rolePermission);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    // RoleAssignment
    public async Task<RoleAssignment> CreateRoleAssignmentAsync(RoleAssignment roleAssignment, CancellationToken cancellationToken = default)
    {
        dbContext.RoleAssignments.Add(roleAssignment);
        await dbContext.SaveChangesAsync(cancellationToken);
        return roleAssignment;
    }

    public Task<RoleAssignment?> GetRoleAssignmentByIdAsync(Guid assignmentId, CancellationToken cancellationToken = default) =>
        dbContext.RoleAssignments
            .Include(ra => ra.Role)
            .Include(ra => ra.Scope)
            .FirstOrDefaultAsync(ra => ra.Id == assignmentId, cancellationToken);

    public Task<RoleAssignment?> GetRoleAssignmentAsync(Guid principalId, Guid roleId, CancellationToken cancellationToken = default) =>
        dbContext.RoleAssignments
            .Include(ra => ra.Role)
            .Include(ra => ra.Scope)
            .FirstOrDefaultAsync(ra => ra.PrincipalId == principalId && ra.RoleId == roleId, cancellationToken);

    public Task<RoleAssignment?> GetRoleAssignmentAsync(Guid principalId, Guid roleId, Guid scopeId, CancellationToken cancellationToken = default) =>
        dbContext.RoleAssignments
            .Include(ra => ra.Role)
            .Include(ra => ra.Scope)
            .FirstOrDefaultAsync(ra => ra.PrincipalId == principalId && ra.RoleId == roleId && ra.ScopeId == scopeId, cancellationToken);

    public async Task<IEnumerable<Role>> GetRolesByPrincipalIdAsync(Guid principalId, CancellationToken cancellationToken = default) =>
        await dbContext.RoleAssignments
            .Where(ra => ra.PrincipalId == principalId)
            .Select(ra => ra.Role)
            .Distinct()
            .ToListAsync(cancellationToken);

    public async Task<IEnumerable<RoleAssignment>> GetRoleAssignmentsByPrincipalIdAsync(Guid principalId, CancellationToken cancellationToken = default) =>
        await dbContext.RoleAssignments
            .Include(ra => ra.Role)
            .Include(ra => ra.Scope)
            .Where(ra => ra.PrincipalId == principalId)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

    public async Task<IEnumerable<RoleAssignment>> GetRoleAssignmentsByRoleIdAsync(Guid roleId, CancellationToken cancellationToken = default) =>
        await dbContext.RoleAssignments
            .Include(ra => ra.Role)
            .Include(ra => ra.Scope)
            .Where(ra => ra.RoleId == roleId)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

    public async Task DeleteRoleAssignmentAsync(RoleAssignment roleAssignment, CancellationToken cancellationToken = default)
    {
        dbContext.RoleAssignments.Remove(roleAssignment);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    // Evaluation & Candidate Selection
    public async Task<IEnumerable<string>> GetPermissionsForPrincipalAsync(
        Guid principalId,
        Guid? scopeId = null,
        CancellationToken cancellationToken = default)
    {
        var principalIds = new List<Guid> { principalId };

        var principal = await dbContext.Principals
            .Include(p => p.User)
            .FirstOrDefaultAsync(p => p.Id == principalId, cancellationToken);

        if (principal?.User is not null)
        {
            var groupPrincipalIds = await (
                from ug in dbContext.UserGroups
                join g in dbContext.Groups on ug.GroupId equals g.Id
                where ug.UserId == principal.User.Id && ug.LeftAtUtc == null
                select g.PrincipalId
            ).ToListAsync(cancellationToken);

            principalIds.AddRange(groupPrincipalIds);
        }

        var query = dbContext.RoleAssignments
            .Where(ra => principalIds.Contains(ra.PrincipalId));

        if (scopeId.HasValue)
        {
            query = query.Where(ra => ra.ScopeId == scopeId.Value);
        }

        var roleIds = await query.Select(ra => ra.RoleId).Distinct().ToListAsync(cancellationToken);

        return await dbContext.RolePermissions
            .Where(rp => roleIds.Contains(rp.RoleId))
            .Select(rp => rp.Permission.Name)
            .Distinct()
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> HasPermissionAsync(
        Guid principalId,
        string permissionName,
        Guid? scopeId = null,
        CancellationToken cancellationToken = default)
    {
        var permissions = await GetPermissionsForPrincipalAsync(principalId, scopeId, cancellationToken);
        return permissions.Any(p => p.Equals(permissionName, StringComparison.OrdinalIgnoreCase));
    }

    public async Task<IEnumerable<User>> GetUsersForPrincipalSelectionAsync(CancellationToken cancellationToken = default) =>
        await dbContext.Users
            .Include(u => u.Principal)
            .AsNoTracking()
            .OrderBy(u => u.DisplayName)
            .ToListAsync(cancellationToken);

    public async Task<IEnumerable<Group>> GetGroupsForPrincipalSelectionAsync(CancellationToken cancellationToken = default) =>
        await dbContext.Groups
            .Include(g => g.Principal)
            .AsNoTracking()
            .OrderBy(g => g.Name)
            .ToListAsync(cancellationToken);

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        dbContext.SaveChangesAsync(cancellationToken);
}