namespace IdentityService.Api.Entities;


public sealed class Position
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
}
