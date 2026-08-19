namespace IdentityService.Api.Controllers;

using IdentityService.Api.DTOs.Rbac;
using IdentityService.Api.Entities;
using IdentityService.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Authorize]
[Route("api/rbac")]
public sealed class RbacController(IRbacService rbacService) : ControllerBase
{
    // Principals
    [Authorize(Roles = nameof(UserRole.Admin))]
    [HttpPost("principals")]
    public async Task<ActionResult<PrincipalResponse>> CreatePrincipal(
        [FromBody] CreatePrincipalReq request,
        CancellationToken cancellationToken)
    {
        var result = await rbacService.CreatePrincipalAsync(request, cancellationToken);
        return Created($"api/rbac/principals/{result.Id}", result);
    }

    [HttpGet("principals")]
    public async Task<ActionResult<IEnumerable<PrincipalResponse>>> GetAllPrincipals(CancellationToken cancellationToken)
    {
        var result = await rbacService.GetAllPrincipalsAsync(cancellationToken);
        return Ok(result);
    }

    [HttpGet("principals/{id:guid}")]
    public async Task<ActionResult<PrincipalResponse>> GetPrincipalById(Guid id, CancellationToken cancellationToken)
    {
        var result = await rbacService.GetPrincipalByIdAsync(id, cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpGet("principals/for-add-member")]
    public async Task<ActionResult<PrincipalForAddMemberResponse>> GetPrincipalsForAddMember(CancellationToken cancellationToken)
    {
        var result = await rbacService.GetPrincipalsForAddMemberAsync(cancellationToken);
        return Ok(result);
    }

    [Authorize(Roles = nameof(UserRole.Admin))]
    [HttpPost("principals/add-member")]
    public async Task<IActionResult> AddMemberPrincipal(
        [FromBody] AddMemberPrincipalReq request,
        CancellationToken cancellationToken)
    {
        await rbacService.AddMemberPrincipalAsync(request, cancellationToken);
        return NoContent();
    }

    // Scopes
    [Authorize(Roles = nameof(UserRole.Admin))]
    [HttpPost("scopes")]
    public async Task<ActionResult<ScopeResponse>> CreateScope(
        [FromBody] CreateScopeReq request,
        CancellationToken cancellationToken)
    {
        var result = await rbacService.CreateScopeAsync(request, cancellationToken);
        return Created($"api/rbac/scopes/{result.Id}", result);
    }

    [HttpGet("scopes")]
    public async Task<ActionResult<IEnumerable<ScopeResponse>>> GetAllScopes(CancellationToken cancellationToken)
    {
        var result = await rbacService.GetAllScopesAsync(cancellationToken);
        return Ok(result);
    }

    [HttpGet("scopes/{id:guid}")]
    public async Task<ActionResult<ScopeResponse>> GetScopeById(Guid id, CancellationToken cancellationToken)
    {
        var result = await rbacService.GetScopeByIdAsync(id, cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    // Permissions
    [Authorize(Roles = nameof(UserRole.Admin))]
    [HttpPost("permissions")]
    public async Task<ActionResult<PermissionResponse>> CreatePermission(
        [FromBody] CreatePermissionReq request,
        CancellationToken cancellationToken)
    {
        var result = await rbacService.CreatePermissionAsync(request, cancellationToken);
        return Created($"api/rbac/permissions/{result.Id}", result);
    }

    [HttpGet("permissions")]
    public async Task<ActionResult<IEnumerable<PermissionResponse>>> GetAllPermissions(CancellationToken cancellationToken)
    {
        var result = await rbacService.GetAllPermissionsAsync(cancellationToken);
        return Ok(result);
    }

    [HttpGet("permissions/{id:guid}")]
    public async Task<ActionResult<PermissionResponse>> GetPermissionById(Guid id, CancellationToken cancellationToken)
    {
        var result = await rbacService.GetPermissionByIdAsync(id, cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [Authorize(Roles = nameof(UserRole.Admin))]
    [HttpDelete("permissions/{id:guid}")]
    public async Task<IActionResult> DeletePermission(Guid id, CancellationToken cancellationToken)
    {
        var deleted = await rbacService.DeletePermissionAsync(id, cancellationToken);
        return deleted ? NoContent() : NotFound();
    }

    // Roles
    [Authorize(Roles = nameof(UserRole.Admin))]
    [HttpPost("roles")]
    public async Task<ActionResult<RoleResponse>> CreateRole(
        [FromBody] CreateRoleReq request,
        CancellationToken cancellationToken)
    {
        var result = await rbacService.CreateRoleAsync(request, cancellationToken);
        return Created($"api/rbac/roles/{result.Id}", result);
    }

    [HttpGet("roles")]
    public async Task<ActionResult<IEnumerable<RoleResponse>>> GetAllRoles(CancellationToken cancellationToken)
    {
        var result = await rbacService.GetAllRolesAsync(cancellationToken);
        return Ok(result);
    }

    [HttpGet("roles/{id:guid}")]
    public async Task<ActionResult<RoleResponse>> GetRoleById(Guid id, CancellationToken cancellationToken)
    {
        var result = await rbacService.GetRoleByIdAsync(id, cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [Authorize(Roles = nameof(UserRole.Admin))]
    [HttpPut("roles/{id:guid}/permissions")]
    public async Task<ActionResult<RoleResponse>> AssignPermissionsToRole(
        Guid id,
        [FromBody] AssignPermissionsToRoleReq request,
        CancellationToken cancellationToken)
    {
        var result = await rbacService.AssignPermissionsToRoleAsync(id, request, cancellationToken);
        return Ok(result);
    }

    [Authorize(Roles = nameof(UserRole.Admin))]
    [HttpDelete("roles/{id:guid}")]
    public async Task<IActionResult> DeleteRole(Guid id, CancellationToken cancellationToken)
    {
        var deleted = await rbacService.DeleteRoleAsync(id, cancellationToken);
        return deleted ? NoContent() : NotFound();
    }

    // Role Assignments
    [Authorize(Roles = nameof(UserRole.Admin))]
    [HttpPost("assignments")]
    public async Task<ActionResult<RoleAssignmentResponse>> CreateRoleAssignment(
        [FromBody] CreateRoleAssignmentReq request,
        CancellationToken cancellationToken)
    {
        var result = await rbacService.CreateRoleAssignmentAsync(request, cancellationToken);
        return Created($"api/rbac/assignments/{result.Id}", result);
    }

    [HttpGet("principals/{principalId:guid}/assignments")]
    public async Task<ActionResult<IEnumerable<RoleAssignmentResponse>>> GetAssignmentsByPrincipal(
        Guid principalId,
        CancellationToken cancellationToken)
    {
        var result = await rbacService.GetRoleAssignmentsByPrincipalIdAsync(principalId, cancellationToken);
        return Ok(result);
    }

    [HttpGet("roles/{roleId:guid}/assignments")]
    public async Task<ActionResult<IEnumerable<RoleAssignmentResponse>>> GetAssignmentsByRole(
        Guid roleId,
        CancellationToken cancellationToken)
    {
        var result = await rbacService.GetRoleAssignmentsByRoleIdAsync(roleId, cancellationToken);
        return Ok(result);
    }

    [Authorize(Roles = nameof(UserRole.Admin))]
    [HttpDelete("assignments/{assignmentId:guid}")]
    public async Task<IActionResult> DeleteRoleAssignment(Guid assignmentId, CancellationToken cancellationToken)
    {
        var deleted = await rbacService.DeleteRoleAssignmentAsync(assignmentId, cancellationToken);
        return deleted ? NoContent() : NotFound();
    }

    // Permission Checking / Introspection
    [HttpGet("principals/{principalId:guid}/permissions")]
    public async Task<ActionResult<PrincipalPermissionsResponse>> GetPermissionsForPrincipal(
        Guid principalId,
        [FromQuery] Guid? scopeId,
        CancellationToken cancellationToken)
    {
        var result = await rbacService.GetPermissionsForPrincipalAsync(principalId, scopeId, cancellationToken);
        return Ok(result);
    }

    [HttpGet("principals/{principalId:guid}/check-permission")]
    public async Task<ActionResult<CheckPermissionResponse>> CheckPermission(
        Guid principalId,
        [FromQuery] string permission,
        [FromQuery] Guid? scopeId,
        CancellationToken cancellationToken)
    {
        var result = await rbacService.CheckPermissionAsync(principalId, permission, scopeId, cancellationToken);
        return Ok(result);
    }
}
