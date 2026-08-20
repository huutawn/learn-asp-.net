using IdentityService.Api.Entities;
using IdentityService.Api.Repositories;
using IdentityService.Api.Services;

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
var service = new MembershipService(repository, TimeProvider.System);

foreach (var type in new[] { PrincipalType.Group, PrincipalType.Team, PrincipalType.Project })
{
    var resourceId = Guid.NewGuid();
    repository.AddResource(type, resourceId, Guid.NewGuid());

    Assert(await service.SetMemberAsync(type, resourceId, user.Id, true, default));
    Assert((await service.GetMembersAsync(type, resourceId, default))?.Single().Id == user.Id);

    Assert(await service.SetMemberAsync(type, resourceId, user.Id, false, default));
    Assert((await service.GetMembersAsync(type, resourceId, default))?.Count == 0);
}

Assert(!await service.SetMemberAsync(PrincipalType.Group, Guid.NewGuid(), user.Id, true, default));

static void Assert(bool condition)
{
    if (!condition) throw new InvalidOperationException("Membership check failed.");
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

    public Task<IReadOnlyList<User>> GetActiveUsersAsync(Guid principalId, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<User>>(memberships.Values
            .Where(x => x.PrincipalId == principalId && x.LeftAtUtc is null)
            .Select(_ => user)
            .ToArray());

    public void Add(PrincipalMembership membership) =>
        memberships[(membership.UserId, membership.PrincipalId)] = membership;

    public Task SaveChangesAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
