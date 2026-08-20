namespace IdentityService.Api.Entities;

public sealed class Team
{
    public Guid Id { get; set; }
    public Guid PrincipalId { get; set; }
    public Principal Principal { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
}
