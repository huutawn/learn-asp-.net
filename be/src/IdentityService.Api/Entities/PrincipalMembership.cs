namespace IdentityService.Api.Entities;

public sealed class PrincipalMembership
{
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    public Guid PrincipalId { get; set; }
    public Principal Principal { get; set; } = null!;
    public bool IsOwner { get; set; }
    public DateTimeOffset JoinedAtUtc { get; set; }
    public DateTimeOffset? LeftAtUtc { get; set; }
}
