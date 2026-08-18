using IdentityService.Api.DTOs.Users;
using IdentityService.Api.Entities;
using IdentityService.Api.Security;
using IdentityService.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace IdentityService.Api.Controllers;

[ApiController]
[Route("api/users")]
public sealed class UserController(IUserService userService) : ControllerBase
{
    [Authorize(Roles = nameof(UserRole.Admin))]
    [HttpGet]
    public async Task<ActionResult<PagedUsersResponse>> GetPage(
        [FromQuery] UserListQuery query,
        CancellationToken cancellationToken = default)
    {
        return Ok(await userService.GetPageAsync(query, cancellationToken));
    }

    [Authorize(Roles = nameof(UserRole.Admin))]
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<UserResponse?>> GetById(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var userResponse = await userService.GetByIdAsync(id, cancellationToken);
        if (userResponse is null)
        {
            return NotFound();
        }

        return Ok(userResponse);
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<ActionResult<UserResponse?>> GetMe(CancellationToken cancellationToken = default)
    {
        if (!User.TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        var userResponse = await userService.GetByIdAsync(userId, cancellationToken);
        if (userResponse is null)
        {
            return NotFound();
        }

        return Ok(userResponse);
    }

    [Authorize(Roles = nameof(UserRole.Admin))]
    [HttpPut("{id:guid}/role")]
    public async Task<IActionResult> UpdateRole(
        Guid id,
        UpdateUserRoleRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!User.TryGetUserId(out var actorUserId))
        {
            return Unauthorized();
        }

        var updated = await userService.UpdateRoleAsync(
            actorUserId,
            id,
            request,
            cancellationToken);

        return updated ? NoContent() : NotFound();
    }
}
