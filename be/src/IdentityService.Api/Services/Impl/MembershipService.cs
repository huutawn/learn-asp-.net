using IdentityService.Api.DTOs.Users;
using IdentityService.Api.Entities;
using IdentityService.Api.Repositories;

namespace IdentityService.Api.Services;

public sealed class MembershipService(
    IMembershipRepository membershipRepository,
    TimeProvider timeProvider) : IMembershipService
{
    public async Task<bool> SetMemberAsync(
        PrincipalType type,
        Guid resourceId,
        Guid userId,
        bool isMember,
        CancellationToken cancellationToken)
    {
        var principalId = await membershipRepository.GetPrincipalIdAsync(type, resourceId, cancellationToken);
        if (principalId is null || !await membershipRepository.UserExistsAsync(userId, cancellationToken))
        {
            return false;
        }

        var membership = await membershipRepository.GetAsync(userId, principalId.Value, cancellationToken);
        var now = timeProvider.GetUtcNow();
        if (membership is null && isMember)
        {
            membershipRepository.Add(new PrincipalMembership
            {
                UserId = userId,
                PrincipalId = principalId.Value,
                JoinedAtUtc = now
            });
        }
        else if (membership is not null)
        {
            membership.LeftAtUtc = isMember ? null : now;
            if (isMember)
            {
                membership.JoinedAtUtc = now;
            }
        }

        await membershipRepository.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<IReadOnlyList<UserResponse>?> GetMembersAsync(
        PrincipalType type,
        Guid resourceId,
        CancellationToken cancellationToken)
    {
        var principalId = await membershipRepository.GetPrincipalIdAsync(type, resourceId, cancellationToken);
        if (principalId is null)
        {
            return null;
        }

        var users = await membershipRepository.GetActiveUsersAsync(principalId.Value, cancellationToken);
        return users.Select(user => new UserResponse(
            user.Id,
            user.PrincipalId,
            user.Email,
            user.DisplayName,
            user.EmailVerified,
            user.Language,
            user.TimeZoneId,
            user.Role,
            user.CreatedAtUtc)).ToArray();
    }
}
