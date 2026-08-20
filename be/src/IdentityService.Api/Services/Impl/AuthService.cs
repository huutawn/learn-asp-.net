using IdentityService.Api.DTOs.Auth;
using IdentityService.Api.Entities;
using IdentityService.Api.Repositories;
using Microsoft.AspNetCore.Identity;
using IdentityService.Api.Exceptions;

namespace IdentityService.Api.Services;

public sealed class AuthService(
    IUserRepository userRepository,
    ISessionRepository sessionRepository,
    IPasswordHasher<User> passwordHasher,
    IJwtTokenService jwtTokenService,
    RefreshTokenService refreshTokenService,
    IConfiguration configuration)
    : IAuthService
{
    public async Task<RegisterResponse> RegisterAsync(
        RegisterRequest request,
        CancellationToken cancellationToken)
    {
        var email = request.Email
            .Trim()
            .ToLowerInvariant();

        var exists =
            await userRepository.ExistsByEmailAsync(
                email,
                cancellationToken);

        if (exists)
        {
            throw new ConflictException(
                "Email already exists.");
        }

        var now = DateTimeOffset.UtcNow;
        var principalId = Guid.NewGuid();

        var user = new User
        {
            Id = Guid.NewGuid(),
            PrincipalId = principalId,
            Principal = new Principal
            {
                Id = principalId,
                Type = PrincipalType.User
            },

            Email = email,

            DisplayName =
                request.DisplayName.Trim(),

            EmailVerified = false,

            Role = UserRole.User,

            CreatedAtUtc = now,

            UpdatedAtUtc = now
        };

        user.PasswordHash =
            passwordHasher.HashPassword(
                user,
                request.Password);

        await userRepository.CreateAsync(
            user,
            cancellationToken);

        return new RegisterResponse(
            user.Id,
            user.PrincipalId,
            user.Email,
            user.DisplayName,
            user.CreatedAtUtc);
    }

    public async Task<LoginResponse> LoginAsync(
        LoginRequest request,
        CancellationToken cancellationToken)
    {
        var email = request.Email
            .Trim()
            .ToLowerInvariant();

        var user =
            await userRepository.GetByEmailAsync(
                email,
                cancellationToken);

        if (user is null)
        {
            throw new UnauthenticationException(
                "Invalid credentials.");
        }

        var verificationResult =
            passwordHasher.VerifyHashedPassword(
                user,
                user.PasswordHash,
                request.Password);

        if (verificationResult ==
            PasswordVerificationResult.Failed)
        {
            throw new UnauthenticationException(
                "Invalid credentials.");
        }

        var now = DateTimeOffset.UtcNow;
        var issuedTokens = await IssueTokensAsync(user, now, cancellationToken);

        await sessionRepository.CreateAsync(
            new Session
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                RefreshTokenHash =
                    issuedTokens.RefreshTokenHash,
                CreatedAtUtc = now,
                ExpiresAtUtc =
                    issuedTokens.Response
                        .RefreshTokenExpiresAtUtc
            },
            cancellationToken);

        return issuedTokens.Response;
    }

    public async Task<LoginResponse> RefreshAsync(
        RefreshTokenRequest request,
        CancellationToken cancellationToken)
    {
        var refreshTokenHash =
            refreshTokenService.Hash(
                request.RefreshToken);

        var session =
            await sessionRepository
                .GetByRefreshTokenHashAsync(
                    refreshTokenHash,
                    cancellationToken);

        var now = DateTimeOffset.UtcNow;

        if (session is null ||
            session.RevokedAtUtc is not null ||
            session.ExpiresAtUtc <= now)
        {
            throw new UnauthenticationException(
                "Invalid refresh token.");
        }

        var issuedTokens =
            await IssueTokensAsync(session.User, now, cancellationToken);

        var rotated =
            await sessionRepository.RotateAsync(
                session.Id,
                refreshTokenHash,
                issuedTokens.RefreshTokenHash,
                now,
                issuedTokens.Response
                    .RefreshTokenExpiresAtUtc,
                cancellationToken);

        if (!rotated)
        {
            throw new UnauthenticationException(
                "Invalid refresh token.");
        }

        return issuedTokens.Response;
    }

    private async Task<(
        LoginResponse Response,
        string RefreshTokenHash)> IssueTokensAsync(
            User user,
            DateTimeOffset now,
            CancellationToken cancellationToken)
    {
        var accessExpiresAt = now.AddMinutes(
            configuration.GetValue<int>(
                "Jwt:AccessTokenMinutes"));

        var refreshExpiresAt = now.AddDays(
            configuration.GetValue<int>(
                "Jwt:RefreshTokenDays"));

        var refreshToken =
            refreshTokenService.Generate();

        return (
            new LoginResponse(
                await jwtTokenService.GenerateAsync(
                    user,
                    accessExpiresAt,
                    cancellationToken),
                accessExpiresAt,
                refreshToken,
                refreshExpiresAt),
            refreshTokenService.Hash(refreshToken));
    }
}
