using IdentityService.Api.DTOs.Projects;

namespace IdentityService.Api.Services;

public interface IProjectService
{
    Task<ProjectResponse> CreateAsync(CreateProjectRequest request, Guid ownerId, CancellationToken cancellationToken);
    Task<ProjectResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<ProjectResponse?> UpdateAsync(Guid id, UpdateProjectRequest request, CancellationToken cancellationToken);
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken);
}
