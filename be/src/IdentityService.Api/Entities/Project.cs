namespace IdentityService.Api.Entities;

public enum ProjectMemberStatus
{
    Active,
    Inactive
}
public sealed class Project
{
    public Guid Id { get; set; }

    public string Name { get; set; } = null!;
    public Guid? ScopeId { get; set; }
    public Scope? Scope { get; set; }
    public Guid PrincipalId { get; set; }
    public Principal Principal { get; set; } = null!;
    public string? Description { get; set; }
    public Guid OwnerId { get; set; }
    public User Owner { get; set; } = null!;
    public string Type { get; set; } = null!;
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
}

public sealed class ProjectTranslation
{
    public Guid ProjectId { get; set; }
    public Project Project { get; set; } = null!;
    public string Language { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
}
