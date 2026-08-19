using System.ComponentModel.DataAnnotations;

namespace IdentityService.Api.DTOs.Teams;

public sealed record CreateTeamRequest(
    [param: Required, MaxLength(100)] string Name,
    [param: MaxLength(1_000)] string? Description = null,
    Guid? ScopeId = null);

public sealed record UpdateTeamRequest(
    [param: Required, MaxLength(100)] string Name,
    [param: MaxLength(1_000)] string? Description = null,
    Guid? ScopeId = null);

public sealed record TeamResponse(
    Guid Id,
    Guid PrincipalId,
    string Name,
    string? Description,
    Guid? ScopeId,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc);
