using IdentityService.Api.DTOs.Members;
using IdentityService.Api.Entities;
using IdentityService.Api.Exceptions;
using IdentityService.Api.Repositories;
using IdentityService.Api.Security;

namespace IdentityService.Api.Services;

public sealed class MembershipService(IMembershipRepository repository, TimeProvider timeProvider, IHttpContextAccessor httpContextAccessor) : IMembershipService
{
    public async Task<bool> SetMemberAsync(PrincipalType type, Guid resourceId, Guid actorUserId, Guid userId, SetMemberRequest request, CancellationToken ct)
    {
        var principalId = await repository.GetPrincipalIdAsync(type, resourceId, ct);
        if (principalId is null || !await repository.UserExistsAsync(userId, ct)) return false;
        var actorMembership = await repository.GetAsync(actorUserId, principalId.Value, ct);
        var claims = httpContextAccessor.HttpContext?.User;
        var admin = claims is not null && PermissionClaims.IsAdmin(claims) || await repository.IsAdminAsync(actorUserId, ct);
        var actorIsOwner = actorMembership?.IsOwner == true || claims is not null && PermissionClaims.IsOwner(claims, principalId.Value);
        var canManage = admin || actorIsOwner || claims is not null && PermissionClaims.HasResource(claims, principalId.Value, Permissions.MembershipManage) || await repository.HasPermissionAsync(actorUserId, Permissions.MembershipManage, principalId.Value, ct);
        var canManageOwners = admin || actorIsOwner || claims is not null && PermissionClaims.HasResource(claims, principalId.Value, Permissions.OwnerManage) || await repository.HasPermissionAsync(actorUserId, Permissions.OwnerManage, principalId.Value, ct);
        if (!canManage) throw new ForbiddenException("You cannot manage membership for this resource.");
        if (request.IsOwner && !canManageOwners) throw new ForbiddenException("Only an owner or admin can assign owner.");
        var requestedPermissions = request.PermissionIds ?? [];
        var assigningAccess = (request.RoleIds?.Count ?? 0) > 0 || requestedPermissions.Count > 0;
        if (!admin && !actorIsOwner)
        {
            var hasAccessGrant = claims is not null && PermissionClaims.HasResource(claims, principalId.Value, Permissions.AccessGrant) || await repository.HasPermissionAsync(actorUserId, Permissions.AccessGrant, principalId.Value, ct);
            if (assigningAccess && !hasAccessGrant)
                throw new ForbiddenException("Missing access.grant permission.");
            var effective = claims is not null ? PermissionClaims.GetResourcePermissions(claims, principalId.Value) : await repository.GetPermissionsAsync(actorUserId, principalId.Value, ct);
            var rolePermissions = await repository.GetRolePermissionNamesByIdsAsync(request.RoleIds ?? [], ct);
            if (requestedPermissions.Count > 0 || rolePermissions.Count > 0)
            {
                var names = await GetPermissionNamesAsync(requestedPermissions, ct);
                names.UnionWith(rolePermissions);
                if (names.Any(name => !effective.Contains(name))) throw new ForbiddenException("You cannot grant permissions you do not have.");
            }
        }
        var membership = await repository.GetAsync(userId, principalId.Value, ct);
        if (!request.IsMember)
        {
            if (membership is null) return true;
            if (membership.IsOwner && !canManageOwners) throw new ForbiddenException("Only an owner or admin can remove an owner.");
            if (membership.IsOwner && !await repository.HasAnotherOwnerAsync(principalId.Value, userId, ct)) throw new ConflictException("A resource must retain at least one owner.");
            membership.LeftAtUtc = timeProvider.GetUtcNow(); membership.IsOwner = false;
            await repository.SaveChangesAsync(ct); return true;
        }
        if (membership is not null && membership.IsOwner && !request.IsOwner && !canManageOwners)
            throw new ForbiddenException("Only an owner or admin can demote an owner.");
        if (membership is not null && membership.IsOwner && !request.IsOwner && !await repository.HasAnotherOwnerAsync(principalId.Value, userId, ct))
            throw new ConflictException("A resource must retain at least one owner.");
        if (membership is null)
        {
            repository.Add(new PrincipalMembership { UserId = userId, PrincipalId = principalId.Value, IsOwner = request.IsOwner, JoinedAtUtc = timeProvider.GetUtcNow() });
        }
        else { membership.LeftAtUtc = null; membership.JoinedAtUtc = timeProvider.GetUtcNow(); membership.IsOwner = request.IsOwner; await repository.SaveChangesAsync(ct); }
        await repository.ReplaceAccessAsync(userId, principalId.Value, request.RoleIds ?? [], requestedPermissions, ct);
        return true;
    }

    public async Task<IReadOnlyList<MemberResponse>?> GetMembersAsync(PrincipalType type, Guid resourceId, CancellationToken ct)
    {
        var principalId = await repository.GetPrincipalIdAsync(type, resourceId, ct);
        if (principalId is null) return null;
        var rows = await repository.GetActiveUsersAsync(principalId.Value, ct);
        var members = new List<MemberResponse>(rows.Count);
        foreach (var row in rows)
        {
            var access = await repository.GetAccessAsync(row.User.PrincipalId, principalId.Value, ct);
            members.Add(new MemberResponse(row.User.Id, row.User.PrincipalId, row.User.Email, row.User.DisplayName, row.Membership.IsOwner, access.RoleIds, access.PermissionIds));
        }
        return members;
    }

    private async Task<HashSet<string>> GetPermissionNamesAsync(IEnumerable<Guid> ids, CancellationToken ct)
    {
        // Repository keeps catalog ownership; this validation is intentionally delegated through the RBAC catalog in v1.
        return (await repository.GetPermissionNamesByIdsAsync(ids, ct)).ToHashSet(StringComparer.OrdinalIgnoreCase);
    }
}
