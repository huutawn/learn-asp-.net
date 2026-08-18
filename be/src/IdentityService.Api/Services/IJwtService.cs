using IdentityService.Api.Entities;

namespace IdentityService.Api.Services;

public interface IJwtTokenService
{
    string Generate(
        User user,
        DateTimeOffset expiresAtUtc);
}