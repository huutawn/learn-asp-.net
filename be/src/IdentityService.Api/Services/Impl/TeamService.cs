using IdentityService.Api.DTOs.Teams;
using IdentityService.Api.Entities;
using IdentityService.Api.Exceptions;
using IdentityService.Api.Repositories;

namespace IdentityService.Api.Services;

public sealed class TeamService(
    ITeamRepository teamRepository,
    TimeProvider timeProvider,
    IMembershipRepository membershipRepository) : ITeamService
{
    public async Task<TeamResponse> CreateAsync(CreateTeamRequest request, Guid actorUserId, CancellationToken cancellationToken)
    {
        var name = Required(request.Name, "Team name");
        if (await teamRepository.ExistsByNameAsync(name, null, cancellationToken))
            throw new ConflictException("A team with this name already exists.");
        if (!await membershipRepository.IsAdminAsync(actorUserId, cancellationToken) && !await membershipRepository.HasPermissionAsync(actorUserId, "team.create", Guid.Empty, cancellationToken))
            throw new ForbiddenException("Missing team.create permission.");
        var now = timeProvider.GetUtcNow();
        var principalId = Guid.NewGuid();
        var team = new Team
        {
            Id = Guid.NewGuid(),
            PrincipalId = principalId,
            Principal = new Principal { Id = principalId, Type = PrincipalType.Team },
            Name = name,
            Description = Optional(request.Description),
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };
        await teamRepository.AddAsync(team, cancellationToken);
        membershipRepository.Add(new PrincipalMembership { UserId = actorUserId, PrincipalId = principalId, IsOwner = true, JoinedAtUtc = now });
        await membershipRepository.SaveChangesAsync(cancellationToken);
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
        team.Name = name;
        team.Description = Optional(request.Description);
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
        team.Id, team.PrincipalId, team.Name, team.Description, team.CreatedAtUtc, team.UpdatedAtUtc);

    private static string Required(string value, string field) => string.IsNullOrWhiteSpace(value)
        ? throw new BadRequestException($"{field} is required.") : value.Trim();

    private static string? Optional(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
