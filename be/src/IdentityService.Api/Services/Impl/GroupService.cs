using IdentityService.Api.DTOs.Groups;
using IdentityService.Api.Entities;
using IdentityService.Api.Exceptions;
using IdentityService.Api.Repositories;
using IdentityService.Api.Security;

namespace IdentityService.Api.Services;

public sealed class GroupService(
    IGroupRepository groupRepository,
    IMembershipRepository membershipRepository,
    TimeProvider timeProvider,
    IHttpContextAccessor httpContextAccessor) : IGroupService
{
    public async Task<GroupResponse> CreateAsync(
        CreateGroupReq request,
        Guid actorUserId,
        CancellationToken cancellationToken)
    {
        var name = request.Name.Trim();
        var type = request.Type.Trim();
        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(type))
        {
            throw new BadRequestException("Group name and type are required.");
        }

        if (await groupRepository.ExistsByNameAndTypeAsync(name, type, cancellationToken))
        {
            throw new ConflictException("A group with this name and type already exists.");
        }
        var claims = httpContextAccessor.HttpContext?.User;
        if ((claims is null || (!PermissionClaims.IsAdmin(claims) && !PermissionClaims.HasGlobal(claims, Permissions.GroupCreate))) &&
            !await membershipRepository.IsAdminAsync(actorUserId, cancellationToken) && !await membershipRepository.HasPermissionAsync(actorUserId, Permissions.GroupCreate, Guid.Empty, cancellationToken))
            throw new ForbiddenException("Missing group.create permission.");

        var principalId = Guid.NewGuid();
        var group = new Group
        {
            Id = Guid.NewGuid(),
            PrincipalId = principalId,
            Principal = new Principal
            {
                Id = principalId,
                Type = PrincipalType.Group
            },
            Name = name,
            Description = string.IsNullOrWhiteSpace(request.Description)
                ? null
                : request.Description.Trim(),
            Type = type
        };
        await groupRepository.AddAsync(group, cancellationToken);
        membershipRepository.Add(new PrincipalMembership { UserId = actorUserId, PrincipalId = principalId, IsOwner = true, JoinedAtUtc = timeProvider.GetUtcNow() });
        await membershipRepository.SaveChangesAsync(cancellationToken);
        return new GroupResponse(group.Id, group.PrincipalId, group.Name, group.Description, group.Type);
    }

}
