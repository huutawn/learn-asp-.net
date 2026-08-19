using IdentityService.Api.DTOs.Teams;
using IdentityService.Api.Entities;
using IdentityService.Api.Exceptions;
using IdentityService.Api.Repositories;

namespace IdentityService.Api.Services;

public sealed class TeamService(ITeamRepository teamRepository, TimeProvider timeProvider) : ITeamService
{
    public async Task<TeamResponse> CreateAsync(CreateTeamRequest request, CancellationToken cancellationToken)
    {
        var name = Required(request.Name, "Team name");
        if (await teamRepository.ExistsByNameAsync(name, null, cancellationToken))
            throw new ConflictException("A team with this name already exists.");
        if (request.ScopeId.HasValue && !await teamRepository.ScopeExistsAsync(request.ScopeId.Value, cancellationToken))
            throw new NotFoundException("Scope not found.");

        var now = timeProvider.GetUtcNow();
        var principalId = Guid.NewGuid();
        var team = new Team
        {
            Id = Guid.NewGuid(),
            PrincipalId = principalId,
            Principal = new Principal { Id = principalId, Type = PrincipalType.Team },
            Name = name,
            Description = Optional(request.Description),
            ScopeId = request.ScopeId,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };
        await teamRepository.AddAsync(team, cancellationToken);
        return Map(team);
    }

    public async Task<TeamResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        (await teamRepository.GetByIdAsync(id, cancellationToken)) is { } team ? Map(team) : null;

    public async Task<TeamResponse?> UpdateAsync(Guid id, UpdateTeamRequest request, CancellationToken cancellationToken)
    {
        var team = await teamRepository.GetForUpdateAsync(id, cancellationToken);
        if (team is null) return null;
        var name = Required(request.Name, "Team name");
        if (await teamRepository.ExistsByNameAsync(name, id, cancellationToken))
            throw new ConflictException("A team with this name already exists.");
        if (request.ScopeId.HasValue && !await teamRepository.ScopeExistsAsync(request.ScopeId.Value, cancellationToken))
            throw new NotFoundException("Scope not found.");

        team.Name = name;
        team.Description = Optional(request.Description);
        team.ScopeId = request.ScopeId;
        team.UpdatedAtUtc = timeProvider.GetUtcNow();
        await teamRepository.SaveChangesAsync(cancellationToken);
        return Map(team);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var team = await teamRepository.GetForUpdateAsync(id, cancellationToken);
        if (team is null) return false;
        await teamRepository.DeleteAsync(team, cancellationToken);
        return true;
    }

    private static TeamResponse Map(Team team) => new(
        team.Id, team.PrincipalId, team.Name, team.Description, team.ScopeId, team.CreatedAtUtc, team.UpdatedAtUtc);

    private static string Required(string value, string field) => string.IsNullOrWhiteSpace(value)
        ? throw new BadRequestException($"{field} is required.") : value.Trim();

    private static string? Optional(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
