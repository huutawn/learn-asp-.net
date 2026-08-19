using IdentityService.Api.DTOs.Projects;
using IdentityService.Api.Entities;
using IdentityService.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IdentityService.Api.Controllers;

[ApiController]
[Authorize(Roles = nameof(UserRole.Admin))]
[Route("api/projects")]
public sealed class ProjectsController(IProjectService projectService) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<ProjectResponse>> Create(
        CreateProjectRequest request,
        CancellationToken cancellationToken)
    {
        var project = await projectService.CreateAsync(request, cancellationToken);
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
}
