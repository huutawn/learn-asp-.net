namespace IdentityService.Api.DTOs.Auth;

public sealed record RegisterResponse(
    Guid Id,
    Guid PrincipalId,
    string Email,
    string DisplayName,
    DateTimeOffset CreatedAtUtc
);
