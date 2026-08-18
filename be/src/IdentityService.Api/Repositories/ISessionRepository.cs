using IdentityService.Api.Entities;

namespace IdentityService.Api.Repositories;

public interface ISessionRepository
{
    Task<Session?> GetByRefreshTokenHashAsync(
        string refreshTokenHash,
        CancellationToken cancellationToken);

    Task CreateAsync(
        Session session,
        CancellationToken cancellationToken);

    Task<bool> RotateAsync(
        Guid sessionId,
        string expectedRefreshTokenHash,
        string newRefreshTokenHash,
        DateTimeOffset rotatedAtUtc,
        DateTimeOffset expiresAtUtc,
        CancellationToken cancellationToken);
}
