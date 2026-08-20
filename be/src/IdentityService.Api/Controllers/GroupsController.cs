using IdentityService.Api.DTOs.Groups;
using IdentityService.Api.DTOs.Members;
using IdentityService.Api.Entities;
using IdentityService.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IdentityService.Api.Controllers;

[ApiController]
[Authorize(Roles = nameof(UserRole.Admin))]
[Route("api/groups")]
public sealed class GroupsController(
    IGroupService groupService,
    IMembershipService membershipService) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<GroupResponse>> CreateAsync(
        CreateGroupReq request,
        CancellationToken cancellationToken)
    {
        var group = await groupService.CreateAsync(request, cancellationToken);
        return Created($"api/groups/{group.Id}", group);
    }

    [HttpGet("{groupId:guid}/members")]
    public async Task<IActionResult> GetMembers(Guid groupId, CancellationToken cancellationToken) =>
        (await membershipService.GetMembersAsync(PrincipalType.Group, groupId, cancellationToken)) is { } members
            ? Ok(members)
            : NotFound();

    [HttpPut("{groupId:guid}/members/{userId:guid}")]
    public async Task<IActionResult> SetMemberAsync(
        Guid groupId,
        Guid userId,
        SetMemberRequest request,
        CancellationToken cancellationToken)
    {
        return await membershipService.SetMemberAsync(
                PrincipalType.Group,
                groupId,
                userId,
                request.IsMember,
                cancellationToken)
            ? NoContent()
            : NotFound();
    }
}
