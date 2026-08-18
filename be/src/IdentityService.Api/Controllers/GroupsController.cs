using IdentityService.Api.DTOs.Groups;
using IdentityService.Api.Entities;
using IdentityService.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IdentityService.Api.Controllers;

[ApiController]
[Authorize(Roles = nameof(UserRole.Admin))]
[Route("api/groups")]
public sealed class GroupsController(IGroupService groupService) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<GroupResponse>> CreateAsync(
        CreateGroupReq request,
        CancellationToken cancellationToken)
    {
        var group = await groupService.CreateAsync(request, cancellationToken);
        return Created($"api/groups/{group.Id}", group);
    }

    [HttpPut("{groupId:guid}/members/{userId:guid}")]
    public async Task<IActionResult> SetMemberAsync(
        Guid groupId,
        Guid userId,
        SetGroupMemberRequest request,
        CancellationToken cancellationToken)
    {
        return await groupService.SetMemberAsync(groupId, userId, request.IsMember, cancellationToken)
            ? NoContent()
            : NotFound();
    }
}
