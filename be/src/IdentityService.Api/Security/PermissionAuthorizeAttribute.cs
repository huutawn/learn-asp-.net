using IdentityService.Api.Entities;
using Microsoft.AspNetCore.Authorization;

namespace IdentityService.Api.Security;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public sealed class PermissionAuthorizeAttribute : AuthorizeAttribute
{
    private string permission = string.Empty;
    private string? resourceRoute;
    private PrincipalType resourceType;

    public PermissionAuthorizeAttribute(string permission)
    {
        Permission = permission;
        UpdatePolicy();
    }

    public string Permission
    {
        get => permission;
        set { permission = value; UpdatePolicy(); }
    }

    public string? ResourceRoute
    {
        get => resourceRoute;
        set { resourceRoute = value; UpdatePolicy(); }
    }

    public PrincipalType ResourceType
    {
        get => resourceType;
        set { resourceType = value; UpdatePolicy(); }
    }

    private void UpdatePolicy() => Policy = PermissionPolicyName.Build(permission, resourceRoute, resourceType);
}

public static class PermissionPolicyName
{
    private const string Prefix = "permission:";
    //input: permission:permissionName:resourceRoute:resourceType
    //output: permissionName, resourceRoute, resourceType
    public static string Build(string permission, string? resourceRoute, PrincipalType? resourceType) =>
        Prefix + Uri.EscapeDataString(permission) + ":" + Uri.EscapeDataString(resourceRoute ?? string.Empty) + ":" + (resourceRoute is null ? string.Empty : resourceType.ToString());

    public static bool TryParse(string policyName, out PermissionRequirement requirement)
    {
        requirement = null!;
        if (!policyName.StartsWith(Prefix, StringComparison.Ordinal)) return false;
        var parts = policyName[Prefix.Length..].Split(':', 3);
        if (parts.Length != 3) return false;
        if (!Enum.TryParse<PrincipalType>(parts[2], true, out var type)) type = default;
        var resourceRoute = Uri.UnescapeDataString(parts[1]);
        requirement = new PermissionRequirement(Uri.UnescapeDataString(parts[0]), string.IsNullOrEmpty(resourceRoute) ? null : resourceRoute, string.IsNullOrEmpty(parts[2]) ? null : type);
        return true;
    }
}
