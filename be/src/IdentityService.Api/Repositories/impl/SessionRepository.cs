using IdentityService.Api.Data;
using IdentityService.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace IdentityService.Api.Repositories;

public sealed class SessionRepository(
    ApplicationDbContext dbContext)
    : ISessionRepository
{
    public Task<Session?> GetByRefreshTokenHashAsync(
        string refreshTokenHash,
        CancellationToken cancellationToken)
    {
        return dbContext.Sessions
            .AsNoTracking()
            .Include(x => x.User)
            .FirstOrDefaultAsync(
                x =>
                    x.RefreshTokenHash ==
                    refreshTokenHash,
                cancellationToken);
    }

    public async Task CreateAsync(
        Session session,
        CancellationToken cancellationToken)
    {
        await dbContext.Sessions.AddAsync(
            session,
            cancellationToken);
        await dbContext.SaveChangesAsync(
            cancellationToken);
    }

    public async Task<bool> RotateAsync(
        Guid sessionId,
        string expectedRefreshTokenHash,
        string newRefreshTokenHash,
        DateTimeOffset rotatedAtUtc,
        DateTimeOffset expiresAtUtc,
        CancellationToken cancellationToken)
    {
        var updated = await dbContext.Sessions
            .Where(x =>
                x.Id == sessionId &&
                x.RefreshTokenHash ==
                    expectedRefreshTokenHash &&
                x.RevokedAtUtc == null &&
                x.ExpiresAtUtc > rotatedAtUtc)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(
                        x => x.RefreshTokenHash,
                        newRefreshTokenHash)
                    .SetProperty(
                        x => x.LastRotatedAtUtc,
                        rotatedAtUtc)
                    .SetProperty(
                        x => x.ExpiresAtUtc,
                        expiresAtUtc),
                cancellationToken);

        return updated == 1;
    }
}
