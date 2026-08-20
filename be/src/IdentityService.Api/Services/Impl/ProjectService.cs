using IdentityService.Api.DTOs.Projects;
using IdentityService.Api.Entities;
using IdentityService.Api.Exceptions;
using IdentityService.Api.Repositories;

namespace IdentityService.Api.Services;

public sealed class ProjectService(IProjectRepository projectRepository, IRbacRepository rbacRepository, TimeProvider timeProvider) : IProjectService
{
    public async Task<ProjectResponse> CreateAsync(CreateProjectRequest request, Guid ownerId, CancellationToken cancellationToken)
    {
        await ValidateReferencesAsync(ownerId, request.ScopeId, cancellationToken);
        var scope = await rbacRepository.GetScopeByTypeAsync(ScopeType.Project, cancellationToken)
            ?? throw new NotFoundException("Project scope not found.");
        var now = timeProvider.GetUtcNow();
        var principalId = Guid.NewGuid();
        var project = new Project
        {
            Id = Guid.NewGuid(),
            PrincipalId = principalId,
            Principal = new Principal { Id = principalId, Type = PrincipalType.Project },
            Name = Required(request.Name, "Project name"),
            Type = Required(request.Type, "Project type"),
            Description = Optional(request.Description),
            OwnerId = ownerId,
            ScopeId = scope.Id,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };
        await projectRepository.AddAsync(project, cancellationToken);
        return Map(project);
    }

    public async Task<ProjectResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        (await projectRepository.GetByIdAsync(id, cancellationToken)) is { } project ? Map(project) : null;

    public async Task<ProjectResponse?> UpdateAsync(Guid id, UpdateProjectRequest request, CancellationToken cancellationToken)
    {
        var project = await projectRepository.GetForUpdateAsync(id, cancellationToken);
        if (project is null) return null;
        var scope = await rbacRepository.GetScopeByTypeAsync(ScopeType.Project, cancellationToken)
            ?? throw new NotFoundException("Project scope not found.");
        await ValidateReferencesAsync(request.OwnerId, scope.Id, cancellationToken);

        project.Name = Required(request.Name, "Project name");
        project.Type = Required(request.Type, "Project type");
        project.Description = Optional(request.Description);
        project.OwnerId = request.OwnerId;
        project.ScopeId = scope.Id;
        project.UpdatedAtUtc = timeProvider.GetUtcNow();
        await projectRepository.SaveChangesAsync(cancellationToken);
        return Map(project);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var project = await projectRepository.GetForUpdateAsync(id, cancellationToken);
        if (project is null) return false;
        await projectRepository.DeleteAsync(project, cancellationToken);
        return true;
    }

    private async Task ValidateReferencesAsync(Guid ownerId, Guid? scopeId, CancellationToken cancellationToken)
    {
        if (!await projectRepository.OwnerExistsAsync(ownerId, cancellationToken))
            throw new NotFoundException("Project owner not found.");
        if (scopeId.HasValue && !await projectRepository.ScopeExistsAsync(scopeId.Value, cancellationToken))
            throw new NotFoundException("Scope not found.");
    }

    private static ProjectResponse Map(Project project) => new(
        project.Id, project.PrincipalId, project.Name, project.Type, project.Description,
        project.OwnerId, project.ScopeId, project.CreatedAtUtc, project.UpdatedAtUtc);

    private static string Required(string value, string field) => string.IsNullOrWhiteSpace(value)
        ? throw new BadRequestException($"{field} is required.") : value.Trim();

    private static string? Optional(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
