namespace IdentityService.Api.Entities;

public sealed class Group
{
    public Guid Id { get; set; }
    public Guid PrincipalId { get; set; }
    public Principal Principal { get; set; } = null!;

    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public string Type { get; set; } = null!;
}
