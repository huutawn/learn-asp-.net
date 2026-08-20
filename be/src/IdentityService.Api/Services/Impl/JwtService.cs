using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using IdentityService.Api.Entities;
using IdentityService.Api.Repositories;
using IdentityService.Api.Security;
using Microsoft.IdentityModel.Tokens;

namespace IdentityService.Api.Services;

public sealed class JwtTokenService(
    IConfiguration configuration,
    IRbacRepository rbacRepository)
    : IJwtTokenService
{
    public async Task<string> GenerateAsync(
        User user,
        DateTimeOffset expiresAtUtc,
        CancellationToken cancellationToken = default)
    {
        var issuer =
            configuration["Jwt:Issuer"]
            ?? throw new InvalidOperationException(
                "JWT issuer missing.");

        var audience =
            configuration["Jwt:Audience"]
            ?? throw new InvalidOperationException(
                "JWT audience missing.");

        var key =
            configuration["Jwt:Key"]
            ?? throw new InvalidOperationException(
                "JWT key missing.");

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new("display_name", user.DisplayName),
            new("role", user.Role.ToString()),
            new("principal_id", user.PrincipalId.ToString())
        };

        var snapshot = await rbacRepository.GetAuthorizationSnapshotAsync(user.Id, cancellationToken);
        claims.AddRange(snapshot.GlobalPermissions.Select(permission => new Claim(PermissionClaimTypes.Permission, permission)));
        if (snapshot.IsGlobalAdmin) claims.Add(new Claim(PermissionClaimTypes.RbacAdmin, "true"));
        claims.AddRange(snapshot.OwnedResourcePrincipalIds.Select(id => new Claim(PermissionClaimTypes.ResourceOwner, id.ToString("N"))));
        claims.AddRange(snapshot.ResourcePermissions.SelectMany(resource => resource.Permissions.Select(permission => new Claim(
            PermissionClaimTypes.ResourcePermission,
            PermissionClaimTypes.ResourcePermissionValue(resource.ResourcePrincipalId, permission)))));

        var signingKey =
            new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(key));

        var credentials =
            new SigningCredentials(
                signingKey,
                SecurityAlgorithms.HmacSha256);

        var token =
            new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                expires: expiresAtUtc.UtcDateTime,
                signingCredentials: credentials);

        return new JwtSecurityTokenHandler()
            .WriteToken(token);
    }
}
