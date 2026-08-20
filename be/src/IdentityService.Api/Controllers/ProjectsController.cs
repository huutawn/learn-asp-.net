using IdentityService.Api.DTOs.Projects;
using IdentityService.Api.DTOs.Members;
using IdentityService.Api.Entities;
using IdentityService.Api.Security;
using IdentityService.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IdentityService.Api.Controllers;

[ApiController]
[Route("api/projects")]
public sealed class ProjectsController(
    IProjectService projectService,
    IMembershipService membershipService) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<ProjectResponse>> Create(
        CreateProjectRequest request,
        CancellationToken cancellationToken)
    {
        if (!User.TryGetUserId(out var ownerId))
        {
            return Unauthorized();
        }
        var project = await projectService.CreateAsync(request, ownerId, cancellationToken);
        return Created($"api/projects/{project.Id}", project);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ProjectResponse>> Get(Guid id, CancellationToken cancellationToken) =>
        (await projectService.GetByIdAsync(id, cancellationToken)) is { } project ? Ok(project) : NotFound();

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ProjectResponse>> Update(
        Guid id,
        UpdateProjectRequest request,
        CancellationToken cancellationToken) =>
        (await projectService.UpdateAsync(id, request, cancellationToken)) is { } project ? Ok(project) : NotFound();

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken) =>
        await projectService.DeleteAsync(id, cancellationToken) ? NoContent() : NotFound();

    [HttpGet("{id:guid}/members")]
    public async Task<IActionResult> GetMembers(
        Guid id,
        CancellationToken cancellationToken) =>
        (await membershipService.GetMembersAsync(PrincipalType.Project, id, cancellationToken)) is { } members
            ? Ok(members)
            : NotFound();

    [HttpPut("{id:guid}/members/{userId:guid}")]
    public async Task<IActionResult> SetMember(
        Guid id,
        Guid userId,
        SetMemberRequest request,
        CancellationToken cancellationToken)
    {
        if (!User.TryGetUserId(out var actorUserId)) return Unauthorized();
        return await membershipService.SetMemberAsync(PrincipalType.Project, id, actorUserId, userId, request, cancellationToken) ? NoContent() : NotFound();
    }
}
