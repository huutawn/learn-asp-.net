namespace IdentityService.Api.DTOs.Auth;

public sealed record RegisterResponse(
    Guid Id,
    string Email,
    string DisplayName,
    DateTimeOffset CreatedAtUtc
);