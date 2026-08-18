using System.ComponentModel.DataAnnotations;

namespace IdentityService.Api.DTOs.Auth;

public sealed record RefreshTokenRequest(
    [param: Required, StringLength(128, MinimumLength = 128)]
    string RefreshToken);
