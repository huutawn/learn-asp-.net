using IdentityService.Api.Entities;
using IdentityService.Api.Repositories;
using IdentityService.Api.Services;
using IdentityService.Api.DTOs.Members;
using IdentityService.Api.Security;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;

var user = new User
{
    Id = Guid.NewGuid(),
    PrincipalId = Guid.NewGuid(),
    Email = "member@example.test",
    DisplayName = "Member",
    PasswordHash = "not-used",
    CreatedAtUtc = DateTimeOffset.UnixEpoch
};
var repository = new FakeMembershipRepository(user);
var service = new MembershipService(repository, TimeProvider.System, new HttpContextAccessor());

foreach (var type in new[] { PrincipalType.Group, PrincipalType.Team, PrincipalType.Project })
{
    var resourceId = Guid.NewGuid();
    repository.AddResource(type, resourceId, Guid.NewGuid());

    Assert(await service.SetMemberAsync(type, resourceId, user.Id, user.Id, new SetMemberRequest(), default));
    Assert((await service.GetMembersAsync(type, resourceId, default))?.Single().UserId == user.Id);

    Assert(await service.SetMemberAsync(type, resourceId, user.Id, user.Id, new SetMemberRequest(false), default));
    Assert((await service.GetMembersAsync(type, resourceId, default))?.Count == 0);
}

Assert(!await service.SetMemberAsync(PrincipalType.Group, Guid.NewGuid(), user.Id, user.Id, new SetMemberRequest(), default));

var globalAuthorization = new AuthorizationHandlerContext(
    [new PermissionRequirement(Permissions.GroupCreate, null, null)],
    new ClaimsPrincipal(new ClaimsIdentity([new Claim(PermissionClaimTypes.Permission, Permissions.GroupCreate)], "check")),
    null);
await new PermissionAuthorizationHandler(repository).HandleAsync(globalAuthorization);
Assert(globalAuthorization.HasSucceeded);

var resourcePrincipalId = Guid.NewGuid();
var teamResourceId = Guid.NewGuid();
repository.AddResource(PrincipalType.Team, teamResourceId, resourcePrincipalId);
var resourceContext = new DefaultHttpContext();
resourceContext.Request.RouteValues["id"] = teamResourceId;
var resourceAuthorization = new AuthorizationHandlerContext(
    [new PermissionRequirement(Permissions.MembershipManage, "id", PrincipalType.Team)],
    new ClaimsPrincipal(new ClaimsIdentity([new Claim(PermissionClaimTypes.ResourcePermission, PermissionClaimTypes.ResourcePermissionValue(resourcePrincipalId, Permissions.MembershipManage))], "check")),
    resourceContext);
await new PermissionAuthorizationHandler(repository).HandleAsync(resourceAuthorization);
Assert(resourceAuthorization.HasSucceeded);

var adminRole = BuiltInRbacCatalog.Roles.Single(x => x.Name == BuiltInRbacCatalog.AdminRole);
Assert(adminRole.Permissions.Count == BuiltInRbacCatalog.AllPermissions.Count);

VerifyCalendarRecurrence();

static void Assert(bool condition)
{
    if (!condition) throw new InvalidOperationException("Membership check failed.");
}

static void VerifyCalendarRecurrence()
{
    var monday = new DateTimeOffset(2026, 8, 17, 9, 0, 0, TimeSpan.Zero);
    var recurring = new Event
    {
        IsRecurring = true,
        TimeZoneId = "UTC",
        RecurrenceWeekdays = [(short)DayOfWeek.Monday, (short)DayOfWeek.Friday]
    };

    Assert(recurring.NextOccurrenceStartAfter(monday) ==
        new DateTimeOffset(2026, 8, 21, 9, 0, 0, TimeSpan.Zero));

    recurring.RecurrenceEndAtUtc = new DateTimeOffset(2026, 8, 20, 9, 0, 0, TimeSpan.Zero);
    Assert(recurring.NextOccurrenceStartAfter(monday) is null);

    var oneTime = new Event { IsRecurring = false, TimeZoneId = "UTC" };
    Assert(oneTime.NextOccurrenceStartAfter(monday) is null);
}

sealed class FakeMembershipRepository(User user) : IMembershipRepository
{
    private readonly Dictionary<(PrincipalType Type, Guid ResourceId), Guid> resources = [];
    private readonly Dictionary<(Guid UserId, Guid PrincipalId), PrincipalMembership> memberships = [];

    public void AddResource(PrincipalType type, Guid resourceId, Guid principalId) =>
        resources[(type, resourceId)] = principalId;

    public Task<Guid?> GetPrincipalIdAsync(PrincipalType type, Guid resourceId, CancellationToken cancellationToken) =>
        Task.FromResult(resources.TryGetValue((type, resourceId), out var principalId)
            ? (Guid?)principalId
            : null);

    public Task<bool> UserExistsAsync(Guid userId, CancellationToken cancellationToken) =>
        Task.FromResult(userId == user.Id);

    public Task<PrincipalMembership?> GetAsync(Guid userId, Guid principalId, CancellationToken cancellationToken) =>
        Task.FromResult(memberships.GetValueOrDefault((userId, principalId)));

    public Task<IReadOnlyList<(PrincipalMembership Membership, User User)>> GetActiveUsersAsync(Guid principalId, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<(PrincipalMembership Membership, User User)>>(memberships.Values
            .Where(x => x.PrincipalId == principalId && x.LeftAtUtc is null)
            .Select(x => (x, user))
            .ToArray());

    public Task<bool> IsAdminAsync(Guid userId, CancellationToken cancellationToken) => Task.FromResult(true);
    public Task<bool> HasPermissionAsync(Guid userId, string permission, Guid resourcePrincipalId, CancellationToken cancellationToken) => Task.FromResult(true);
    public Task<Guid?> GetUserPrincipalIdAsync(Guid userId, CancellationToken cancellationToken) => Task.FromResult<Guid?>(user.PrincipalId);
    public Task<HashSet<string>> GetPermissionsAsync(Guid userId, Guid resourcePrincipalId, CancellationToken cancellationToken) => Task.FromResult(new HashSet<string>(StringComparer.OrdinalIgnoreCase));
    public Task<IReadOnlyList<string>> GetPermissionNamesByIdsAsync(IEnumerable<Guid> permissionIds, CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<string>>([]);
    public Task<IReadOnlyList<string>> GetRolePermissionNamesByIdsAsync(IEnumerable<Guid> roleIds, CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<string>>([]);
    public Task<(IReadOnlyList<Guid> RoleIds, IReadOnlyList<Guid> PermissionIds)> GetAccessAsync(Guid subjectPrincipalId, Guid resourcePrincipalId, CancellationToken cancellationToken) => Task.FromResult<(IReadOnlyList<Guid>, IReadOnlyList<Guid>)>(([], []));
    public Task ReplaceAccessAsync(Guid userId, Guid resourcePrincipalId, IEnumerable<Guid> roleIds, IEnumerable<Guid> permissionIds, CancellationToken cancellationToken) => Task.CompletedTask;
    public Task<bool> HasAnotherOwnerAsync(Guid principalId, Guid excludedUserId, CancellationToken cancellationToken) => Task.FromResult(false);

    public void Add(PrincipalMembership membership) =>
        memberships[(membership.UserId, membership.PrincipalId)] = membership;

    public Task SaveChangesAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
