namespace IdentityService.Api.DTOs.Auth;

public sealed record LoginResponse(
    string AccessToken,
    DateTimeOffset AccessTokenExpiresAtUtc,

    string RefreshToken,
    DateTimeOffset RefreshTokenExpiresAtUtc
);