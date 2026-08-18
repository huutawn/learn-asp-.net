using System.Security.Claims;

namespace IdentityService.Api.Security;

public static class ClaimsPrincipalExtensions
{
    public static bool TryGetUserId(
        this ClaimsPrincipal principal,
        out Guid userId) =>
        Guid.TryParse(principal.FindFirst("sub")?.Value, out userId);
}
