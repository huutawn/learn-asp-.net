using IdentityService.Api.DTOs.Groups;
using IdentityService.Api.DTOs.Members;
using IdentityService.Api.Entities;
using IdentityService.Api.Services;
using IdentityService.Api.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IdentityService.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/groups")]
public sealed class GroupsController(
    IGroupService groupService,
    IMembershipService membershipService) : ControllerBase
{
    [PermissionAuthorize(Permissions.GroupCreate)]
    [HttpPost]
    public async Task<ActionResult<GroupResponse>> CreateAsync(
        CreateGroupReq request,
        CancellationToken cancellationToken)
    {
        if (!User.TryGetUserId(out var actorUserId)) return Unauthorized();
        var group = await groupService.CreateAsync(request, actorUserId, cancellationToken);
        return Created($"api/groups/{group.Id}", group);
    }

    [PermissionAuthorize(Permissions.MembershipManage, ResourceRoute = "groupId", ResourceType = PrincipalType.Group)]
    [HttpGet("{groupId:guid}/members")]
    public async Task<IActionResult> GetMembers(Guid groupId, CancellationToken cancellationToken) =>
        (await membershipService.GetMembersAsync(PrincipalType.Group, groupId, cancellationToken)) is { } members
            ? Ok(members)
            : NotFound();

    [PermissionAuthorize(Permissions.MembershipManage, ResourceRoute = "groupId", ResourceType = PrincipalType.Group)]
    [HttpPut("{groupId:guid}/members/{userId:guid}")]
    public async Task<IActionResult> SetMemberAsync(
        Guid groupId,
        Guid userId,
        SetMemberRequest request,
        CancellationToken cancellationToken)
    {
        if (!User.TryGetUserId(out var actorUserId)) return Unauthorized();
        return await membershipService.SetMemberAsync(PrincipalType.Group, groupId, actorUserId, userId, request, cancellationToken)
            ? NoContent()
            : NotFound();
    }
}
