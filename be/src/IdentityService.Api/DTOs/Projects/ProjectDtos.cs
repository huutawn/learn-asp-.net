using System.ComponentModel.DataAnnotations;

namespace IdentityService.Api.DTOs.Projects;

public sealed record CreateProjectRequest(
    [param: Required, MaxLength(100)] string Name,
    [param: Required, MaxLength(64)] string Type,
    [param: MaxLength(1_000)] string? Description = null);

public sealed record UpdateProjectRequest(
    [param: Required, MaxLength(100)] string Name,
    [param: Required, MaxLength(64)] string Type,
    [param: Required] Guid OwnerId,
    [param: MaxLength(1_000)] string? Description = null);

public sealed record ProjectResponse(
    Guid Id,
    Guid PrincipalId,
    string Name,
    string Type,
    string? Description,
    Guid OwnerId,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc);
