using IdentityService.Api.DTOs.Teams;

namespace IdentityService.Api.Services;

public interface ITeamService
{
    Task<TeamResponse> CreateAsync(CreateTeamRequest request, Guid actorUserId, CancellationToken cancellationToken);
    Task<TeamResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<TeamResponse?> UpdateAsync(Guid id, UpdateTeamRequest request, CancellationToken cancellationToken);
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken);
}
