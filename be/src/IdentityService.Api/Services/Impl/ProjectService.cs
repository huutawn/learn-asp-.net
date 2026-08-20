using IdentityService.Api.DTOs.Projects;
using IdentityService.Api.Entities;
using IdentityService.Api.Exceptions;
using IdentityService.Api.Repositories;

namespace IdentityService.Api.Services;

public sealed class ProjectService(IProjectRepository projectRepository, TimeProvider timeProvider, IMembershipRepository membershipRepository) : IProjectService
{
    public async Task<ProjectResponse> CreateAsync(CreateProjectRequest request, Guid ownerId, CancellationToken cancellationToken)
    {
        if (!await membershipRepository.IsAdminAsync(ownerId, cancellationToken) && !await membershipRepository.HasPermissionAsync(ownerId, "project.create", Guid.Empty, cancellationToken))
            throw new ForbiddenException("Missing project.create permission.");
        await ValidateReferencesAsync(ownerId, cancellationToken);
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
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };
        await projectRepository.AddAsync(project, cancellationToken);
        membershipRepository.Add(new PrincipalMembership { UserId = ownerId, PrincipalId = principalId, IsOwner = true, JoinedAtUtc = now });
        await membershipRepository.SaveChangesAsync(cancellationToken);
        return Map(project);
    }

    public async Task<ProjectResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        (await projectRepository.GetByIdAsync(id, cancellationToken)) is { } project ? Map(project) : null;

    public async Task<ProjectResponse?> UpdateAsync(Guid id, UpdateProjectRequest request, CancellationToken cancellationToken)
    {
        var project = await projectRepository.GetForUpdateAsync(id, cancellationToken);
        if (project is null) return null;
        await ValidateReferencesAsync(request.OwnerId, cancellationToken);

        project.Name = Required(request.Name, "Project name");
        project.Type = Required(request.Type, "Project type");
        project.Description = Optional(request.Description);
        project.OwnerId = request.OwnerId;
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

    private async Task ValidateReferencesAsync(Guid ownerId, CancellationToken cancellationToken)
    {
        if (!await projectRepository.OwnerExistsAsync(ownerId, cancellationToken))
            throw new NotFoundException("Project owner not found.");
    }

    private static ProjectResponse Map(Project project) => new(
        project.Id, project.PrincipalId, project.Name, project.Type, project.Description,
        project.OwnerId, project.CreatedAtUtc, project.UpdatedAtUtc);

    private static string Required(string value, string field) => string.IsNullOrWhiteSpace(value)
        ? throw new BadRequestException($"{field} is required.") : value.Trim();

    private static string? Optional(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
