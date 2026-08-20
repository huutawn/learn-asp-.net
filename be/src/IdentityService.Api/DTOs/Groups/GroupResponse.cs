namespace IdentityService.Api.DTOs.Groups;

public sealed record GroupResponse(
    Guid Id,
    Guid PrincipalId,
    string Name,
    string? Description,
    string Type);
