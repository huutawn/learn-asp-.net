using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using IdentityService.Api.Entities;
using Microsoft.IdentityModel.Tokens;

namespace IdentityService.Api.Services;

public sealed class JwtTokenService(
    IConfiguration configuration)
    : IJwtTokenService
{
    public string Generate(
        User user,
        DateTimeOffset expiresAtUtc)
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

        var claims = new[]
        {
            new Claim(
                JwtRegisteredClaimNames.Sub,
                user.Id.ToString()),

            new Claim(
                JwtRegisteredClaimNames.Email,
                user.Email),

            new Claim(
                "display_name",
                user.DisplayName),

            new Claim(
                "role",
                user.Role.ToString())
        };

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
