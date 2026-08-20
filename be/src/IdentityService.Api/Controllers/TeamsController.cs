using IdentityService.Api.DTOs.Teams;
using IdentityService.Api.DTOs.Members;
using IdentityService.Api.Entities;
using IdentityService.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IdentityService.Api.Controllers;

[ApiController]
[Authorize(Roles = nameof(UserRole.Admin))]
[Route("api/teams")]
public sealed class TeamsController(
    ITeamService teamService,
    IMembershipService membershipService) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<TeamResponse>> Create(
        CreateTeamRequest request,
        CancellationToken cancellationToken)
    {
        var team = await teamService.CreateAsync(request, cancellationToken);
        return Created($"api/teams/{team.Id}", team);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<TeamResponse>> Get(Guid id, CancellationToken cancellationToken) =>
        (await teamService.GetByIdAsync(id, cancellationToken)) is { } team ? Ok(team) : NotFound();

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<TeamResponse>> Update(
        Guid id,
        UpdateTeamRequest request,
        CancellationToken cancellationToken) =>
        (await teamService.UpdateAsync(id, request, cancellationToken)) is { } team ? Ok(team) : NotFound();

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken) =>
        await teamService.DeleteAsync(id, cancellationToken) ? NoContent() : NotFound();

    [HttpGet("{id:guid}/members")]
    public async Task<IActionResult> GetMembers(Guid id, CancellationToken cancellationToken) =>
        (await membershipService.GetMembersAsync(PrincipalType.Team, id, cancellationToken)) is { } members
            ? Ok(members)
            : NotFound();

    [HttpPut("{id:guid}/members/{userId:guid}")]
    public async Task<IActionResult> SetMember(
        Guid id,
        Guid userId,
        SetMemberRequest request,
        CancellationToken cancellationToken) =>
        await membershipService.SetMemberAsync(
            PrincipalType.Team,
            id,
            userId,
            request.IsMember,
            cancellationToken) ? NoContent() : NotFound();
}
