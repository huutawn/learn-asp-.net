namespace IdentityService.Api.Services.Impl;

using IdentityService.Api.DTOs.Rbac;
using IdentityService.Api.Entities;
using IdentityService.Api.Exceptions;
using IdentityService.Api.Repositories;

public sealed class RbacService(
    IRbacRepository rbacRepository,
    TimeProvider timeProvider) : IRbacService
{
    // Principal
    public async Task<PrincipalResponse?> GetPrincipalByIdAsync(Guid principalId, CancellationToken cancellationToken = default)
    {
        var principal = await rbacRepository.GetPrincipalDetailsByIdAsync(principalId, cancellationToken);
        return principal is null ? throw new NotFoundException("Principal not found") : MapPrincipal(principal);
    }

    public async Task<PrincipalSearchResponse> SearchPrincipalsAsync(
        PrincipalSearchQuery query,
        CancellationToken cancellationToken = default)
    {
        if (query.Limit is < 1 or > 100)
            throw new BadRequestException("Limit must be between 1 and 100.");

        PrincipalType? type = null;
        if (!string.IsNullOrWhiteSpace(query.Type))
        {
            if (!Enum.TryParse<PrincipalType>(query.Type, true, out var parsedType))
            {
                throw new BadRequestException($"Invalid principal type: '{query.Type}'.");
            }

            type = parsedType;
        }

        Guid? cursor = null;
        if (!string.IsNullOrWhiteSpace(query.Cursor))
        {
            if (!Guid.TryParse(query.Cursor, out var parsedCursor))
                throw new BadRequestException("Cursor must be a principal ID.");
            cursor = parsedCursor;
        }

        var principals = await rbacRepository.SearchPrincipalsAsync(
            type, query.Search, cursor, query.Limit, query.Available, query.ScopeId, cancellationToken);
        var hasNextPage = principals.Count > query.Limit;
        var items = principals.Take(query.Limit).Select(MapPrincipal).ToArray();
        return new PrincipalSearchResponse(
            items,
            hasNextPage ? items[^1].PrincipalId.ToString("N") : null);
    }

    public async Task SetPrincipalAvailabilityAsync(
        Guid principalId,
        bool available,
        CancellationToken cancellationToken = default)
    {
        var principal = await rbacRepository.GetPrincipalByIdAsync(principalId, cancellationToken)
            ?? throw new NotFoundException("Principal not found.");
        principal.Available = available;
        await rbacRepository.SaveChangesAsync(cancellationToken);
    }

    private static PrincipalResponse MapPrincipal(Principal principal) => principal.Type switch
    {
        PrincipalType.User when principal.User is not null => new(
            principal.Id, principal.Type.ToString(), principal.User.DisplayName, null,
            principal.User.Email, principal.Available, null),
        PrincipalType.Group when principal.Group is not null => new(
            principal.Id, principal.Type.ToString(), principal.Group.Name,
            principal.Group.Description, null, principal.Available, principal.Group.ScopeId),
        PrincipalType.Team when principal.Team is not null => new(
            principal.Id, principal.Type.ToString(), principal.Team.Name,
            principal.Team.Description, null, principal.Available, principal.Team.ScopeId),
        PrincipalType.Project when principal.Project is not null => new(
            principal.Id, principal.Type.ToString(), principal.Project.Name,
            principal.Project.Description, null, principal.Available, principal.Project.ScopeId),
        _ => new(principal.Id, principal.Type.ToString(), string.Empty, null, null, principal.Available, null)
    };

    // Scope
    public async Task<ScopeResponse> CreateScopeAsync(CreateScopeReq req, CancellationToken cancellationToken = default)
    {
        if (!Enum.TryParse<ScopeType>(req.Type, true, out var scopeType))
        {
            throw new BadRequestException($"Invalid scope type: '{req.Type}'. Allowed values: {string.Join(", ", Enum.GetNames<ScopeType>())}");
        }

        var scope = new Scope
        {
            Id = Guid.NewGuid(),
            Type = scopeType
        };

        await rbacRepository.CreateScopeAsync(scope, cancellationToken);
        return new ScopeResponse(scope.Id, scope.Type.ToString());
    }

    public async Task<ScopeResponse?> GetScopeByIdAsync(Guid scopeId, CancellationToken cancellationToken = default)
    {
        var scope = await rbacRepository.GetScopeByIdAsync(scopeId, cancellationToken);
        return scope is null ? null : new ScopeResponse(scope.Id, scope.Type.ToString());
    }

    public async Task<IEnumerable<ScopeResponse>> GetAllScopesAsync(CancellationToken cancellationToken = default)
    {
        var scopes = await rbacRepository.GetAllScopesAsync(cancellationToken);
        return scopes.Select(s => new ScopeResponse(s.Id, s.Type.ToString()));
    }

    // Permission
    public async Task<PermissionResponse> CreatePermissionAsync(CreatePermissionReq req, CancellationToken cancellationToken = default)
    {
        var name = req.Name.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new BadRequestException("Permission name is required.");
        }

        if (await rbacRepository.PermissionExistsByNameAsync(name, cancellationToken))
        {
            throw new ConflictException($"Permission '{name}' already exists.");
        }

        var permission = new Permission
        {
            Id = Guid.NewGuid(),
            Name = name,
            Description = string.IsNullOrWhiteSpace(req.Description) ? null : req.Description.Trim()
        };

        await rbacRepository.CreatePermissionAsync(permission, cancellationToken);
        return new PermissionResponse(permission.Id, permission.Name, permission.Description);
    }

    public async Task<PermissionResponse?> GetPermissionByIdAsync(Guid permissionId, CancellationToken cancellationToken = default)
    {
        var permission = await rbacRepository.GetPermissionByIdAsync(permissionId, cancellationToken);
        return permission is null ? null : new PermissionResponse(permission.Id, permission.Name, permission.Description);
    }

    public async Task<IEnumerable<PermissionResponse>> GetAllPermissionsAsync(CancellationToken cancellationToken = default)
    {
        var permissions = await rbacRepository.GetAllPermissionsAsync(cancellationToken);
        return permissions.Select(p => new PermissionResponse(p.Id, p.Name, p.Description));
    }

    public async Task<bool> DeletePermissionAsync(Guid permissionId, CancellationToken cancellationToken = default)
    {
        var permission = await rbacRepository.GetPermissionByIdAsync(permissionId, cancellationToken);
        if (permission is null)
        {
            return false;
        }

        await rbacRepository.DeletePermissionAsync(permission, cancellationToken);
        return true;
    }

    // Role
    public async Task<RoleResponse> CreateRoleAsync(CreateRoleReq req, CancellationToken cancellationToken = default)
    {
        var name = req.Name.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new BadRequestException("Role name is required.");
        }

        if (await rbacRepository.RoleExistsByNameAsync(name, cancellationToken))
        {
            throw new ConflictException($"Role '{name}' already exists.");
        }

        var role = new Role
        {
            Id = Guid.NewGuid(),
            Name = name,
            Description = string.IsNullOrWhiteSpace(req.Description) ? null : req.Description.Trim()
        };

        if (req.PermissionIds is { Count: > 0 })
        {
            var validPermissions = await rbacRepository.GetPermissionsByIdsAsync(req.PermissionIds, cancellationToken);
            foreach (var p in validPermissions)
            {
                role.RolePermissions.Add(new RolePermission
                {
                    RoleId = role.Id,
                    PermissionId = p.Id,
                    Permission = p
                });
            }
        }

        await rbacRepository.CreateRoleAsync(role, cancellationToken);

        var permissions = role.RolePermissions
            .Select(rp => new PermissionResponse(rp.PermissionId, rp.Permission?.Name ?? "", rp.Permission?.Description))
            .ToList();

        return new RoleResponse(role.Id, role.Name, role.Description, permissions);
    }

    public async Task<RoleResponse?> GetRoleByIdAsync(Guid roleId, CancellationToken cancellationToken = default)
    {
        var role = await rbacRepository.GetRoleByIdWithPermissionsAsync(roleId, cancellationToken);
        if (role is null)
        {
            return null;
        }

        var permissions = role.RolePermissions
            .Select(rp => new PermissionResponse(rp.PermissionId, rp.Permission.Name, rp.Permission.Description))
            .ToList();

        return new RoleResponse(role.Id, role.Name, role.Description, permissions);
    }

    public async Task<IEnumerable<RoleResponse>> GetAllRolesAsync(CancellationToken cancellationToken = default)
    {
        var roles = await rbacRepository.GetAllRolesAsync(cancellationToken);
        return roles.Select(r => new RoleResponse(
            r.Id,
            r.Name,
            r.Description,
            r.RolePermissions.Select(rp => new PermissionResponse(rp.PermissionId, rp.Permission.Name, rp.Permission.Description)).ToList()
        ));
    }

    public async Task<RoleResponse> AssignPermissionsToRoleAsync(Guid roleId, AssignPermissionsToRoleReq req, CancellationToken cancellationToken = default)
    {
        var role = await rbacRepository.GetRoleByIdAsync(roleId, cancellationToken)
            ?? throw new NotFoundException("Role not found.");

        await rbacRepository.SetRolePermissionsAsync(roleId, req.PermissionIds, cancellationToken);

        var updated = await rbacRepository.GetRoleByIdWithPermissionsAsync(roleId, cancellationToken);
        var permissions = updated?.RolePermissions
            .Select(rp => new PermissionResponse(rp.PermissionId, rp.Permission.Name, rp.Permission.Description))
            .ToList();

        return new RoleResponse(role.Id, role.Name, role.Description, permissions);
    }

    public async Task<bool> DeleteRoleAsync(Guid roleId, CancellationToken cancellationToken = default)
    {
        var role = await rbacRepository.GetRoleByIdAsync(roleId, cancellationToken);
        if (role is null)
        {
            return false;
        }

        await rbacRepository.DeleteRoleAsync(role, cancellationToken);
        return true;
    }

    // RoleAssignment
    public async Task<RoleAssignmentResponse> CreateRoleAssignmentAsync(CreateRoleAssignmentReq req, CancellationToken cancellationToken = default)
    {
        var role = await rbacRepository.GetRoleByIdAsync(req.RoleId, cancellationToken)
            ?? throw new NotFoundException("Role not found.");

        var principal = await rbacRepository.GetPrincipalByIdAsync(req.PrincipalId, cancellationToken)
            ?? throw new NotFoundException("Principal not found.");
        if (!principal.Available)
        {
            throw new BadRequestException("Unavailable principals cannot be assigned a role.");
        }

        Scope scope;
        if (req.ScopeId.HasValue)
        {
            scope = await rbacRepository.GetScopeByIdAsync(req.ScopeId.Value, cancellationToken)
                ?? throw new NotFoundException("Scope not found.");
        }
        else
        {
            scope = await rbacRepository.GetOrCreateDefaultScopeAsync(cancellationToken);
        }

        var existing = await rbacRepository.GetRoleAssignmentAsync(req.PrincipalId, req.RoleId, scope.Id, cancellationToken);
        if (existing is not null)
        {
            throw new ConflictException("Role is already assigned to this principal in the specified scope.");
        }

        var assignment = new RoleAssignment
        {
            Id = Guid.NewGuid(),
            RoleId = role.Id,
            PrincipalId = principal.Id,
            ScopeId = scope.Id,
            CreatedAt = timeProvider.GetUtcNow()
        };

        await rbacRepository.CreateRoleAssignmentAsync(assignment, cancellationToken);

        return new RoleAssignmentResponse(
            assignment.Id,
            assignment.RoleId,
            assignment.PrincipalId,
            assignment.ScopeId,
            role.Name,
            scope.Type.ToString(),
            assignment.CreatedAt
        );
    }

    public async Task<IEnumerable<RoleAssignmentResponse>> GetRoleAssignmentsByPrincipalIdAsync(Guid principalId, CancellationToken cancellationToken = default)
    {
        var assignments = await rbacRepository.GetRoleAssignmentsByPrincipalIdAsync(principalId, cancellationToken);
        return assignments.Select(ra => new RoleAssignmentResponse(
            ra.Id,
            ra.RoleId,
            ra.PrincipalId,
            ra.ScopeId,
            ra.Role?.Name,
            ra.Scope?.Type.ToString(),
            ra.CreatedAt
        ));
    }

    public async Task<IEnumerable<RoleAssignmentResponse>> GetRoleAssignmentsByRoleIdAsync(Guid roleId, CancellationToken cancellationToken = default)
    {
        var assignments = await rbacRepository.GetRoleAssignmentsByRoleIdAsync(roleId, cancellationToken);
        return assignments.Select(ra => new RoleAssignmentResponse(
            ra.Id,
            ra.RoleId,
            ra.PrincipalId,
            ra.ScopeId,
            ra.Role?.Name,
            ra.Scope?.Type.ToString(),
            ra.CreatedAt
        ));
    }

    public async Task<bool> DeleteRoleAssignmentAsync(Guid assignmentId, CancellationToken cancellationToken = default)
    {
        var assignment = await rbacRepository.GetRoleAssignmentByIdAsync(assignmentId, cancellationToken);
        if (assignment is null)
        {
            return false;
        }

        await rbacRepository.DeleteRoleAssignmentAsync(assignment, cancellationToken);
        return true;
    }

    public Task<bool> RemovePrincipalFromScopeAsync(
        Guid principalId,
        Guid scopeId,
        CancellationToken cancellationToken = default) =>
        rbacRepository.DeleteRoleAssignmentsAsync(principalId, scopeId, cancellationToken);

    // Authorization evaluation
    public async Task<CheckPermissionResponse> CheckPermissionAsync(
        Guid principalId,
        string permissionName,
        Guid? scopeId = null,
        CancellationToken cancellationToken = default)
    {
        var hasPermission = await rbacRepository.HasPermissionAsync(principalId, permissionName, scopeId, cancellationToken);
        return new CheckPermissionResponse(hasPermission, permissionName, principalId, scopeId);
    }

    public async Task<PrincipalPermissionsResponse> GetPermissionsForPrincipalAsync(
        Guid principalId,
        Guid? scopeId = null,
        CancellationToken cancellationToken = default)
    {
        var permissions = (await rbacRepository.GetPermissionsForPrincipalAsync(principalId, scopeId, cancellationToken)).ToArray();
        return new PrincipalPermissionsResponse(principalId, permissions);
    }
}
