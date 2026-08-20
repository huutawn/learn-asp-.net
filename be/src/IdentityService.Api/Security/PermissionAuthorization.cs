using System.Security.Claims;
using IdentityService.Api.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Options;

namespace IdentityService.Api.Security;

public sealed class PermissionPolicyProvider(IOptions<AuthorizationOptions> options) : IAuthorizationPolicyProvider
{
    private readonly DefaultAuthorizationPolicyProvider fallback = new(options);

    public Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        if (!PermissionPolicyName.TryParse(policyName, out var requirement))
            return fallback.GetPolicyAsync(policyName);

        var policy = new AuthorizationPolicyBuilder()
            .RequireAuthenticatedUser()
            .AddRequirements(requirement)
            .Build();
        return Task.FromResult<AuthorizationPolicy?>(policy);
    }

    public Task<AuthorizationPolicy> GetDefaultPolicyAsync() => fallback.GetDefaultPolicyAsync();
    public Task<AuthorizationPolicy?> GetFallbackPolicyAsync() => fallback.GetFallbackPolicyAsync();
}

public sealed class PermissionAuthorizationHandler(
    IMembershipRepository membershipRepository)
    : AuthorizationHandler<PermissionRequirement>
{
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        if (PermissionClaims.IsAdmin(context.User))
        {
            context.Succeed(requirement);
            return;
        }

        if (requirement.ResourceRoute is null)
        {
            if (HasClaim(context.User, PermissionClaimTypes.Permission, requirement.Permission))
                context.Succeed(requirement);
            return;
        }

        var httpContext = context.Resource switch
        {
            HttpContext value => value,
            AuthorizationFilterContext value => value.HttpContext,
            _ => null
        };
        if (httpContext is null || requirement.ResourceType is null)
            return;

        if (!Guid.TryParse(httpContext.Request.RouteValues[requirement.ResourceRoute]?.ToString(), out var resourceId))
            return;

        var resourcePrincipalId = await membershipRepository.GetPrincipalIdAsync(requirement.ResourceType.Value, resourceId, httpContext.RequestAborted);
        if (!resourcePrincipalId.HasValue)
            return;

        if (HasClaim(context.User, PermissionClaimTypes.ResourceOwner, resourcePrincipalId.Value.ToString("N")) ||
            HasClaim(context.User, PermissionClaimTypes.ResourcePermission, PermissionClaimTypes.ResourcePermissionValue(resourcePrincipalId.Value, requirement.Permission)))
        {
            context.Succeed(requirement);
        }
    }

    private static bool HasClaim(ClaimsPrincipal principal, string type, string value) =>
        principal.Claims.Any(claim => claim.Type == type && claim.Value.Equals(value, StringComparison.OrdinalIgnoreCase));
}
