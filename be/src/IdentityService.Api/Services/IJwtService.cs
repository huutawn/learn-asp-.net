using IdentityService.Api.Entities;

namespace IdentityService.Api.Services;

public interface IJwtTokenService
{
    Task<string> GenerateAsync(
        User user,
        DateTimeOffset expiresAtUtc,
        CancellationToken cancellationToken = default);
}
