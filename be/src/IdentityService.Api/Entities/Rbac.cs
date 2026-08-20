namespace IdentityService.Api.Entities;

public sealed class Role
{
    public Guid Id { get; set; }

    public string Name { get; set; } = null!;
    public string? Description { get; set; }

    public ICollection<RolePermission> RolePermissions { get; set; }
        = new List<RolePermission>();

    public ICollection<RoleAssignment> SubjectAssignments { get; set; }
        = new List<RoleAssignment>();
}

public sealed class Permission
{
    public Guid Id { get; set; }

    public string Name { get; set; } = null!;
    public string? Description { get; set; }

    public ICollection<RolePermission> RolePermissions { get; set; }
        = new List<RolePermission>();
}

public sealed class RolePermission
{
    public Guid RoleId { get; set; }
    public Role Role { get; set; } = null!;

    public Guid PermissionId { get; set; }
    public Permission Permission { get; set; } = null!;
}

public enum PrincipalType
{
    User,
    Team,
    Group,
    Project
}

public sealed class Principal
{
    public Guid Id { get; set; }

    public PrincipalType Type { get; set; }
    public bool Available { get; set; } = true;
    public User? User { get; set; }
    public Group? Group { get; set; }
    public Team? Team { get; set; }
    public Project? Project { get; set; }

    public ICollection<RoleAssignment> SubjectAssignments { get; set; }
        = new List<RoleAssignment>();
}

public sealed class RoleAssignment
{
    public Guid Id { get; set; }

    public Guid SubjectPrincipalId { get; set; }
    public Principal SubjectPrincipal { get; set; } = null!;

    public Guid RoleId { get; set; }
    public Role Role { get; set; } = null!;

    public Guid? ResourcePrincipalId { get; set; }
    public Principal? ResourcePrincipal { get; set; }

    public Guid? GrantedByPrincipalId { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}

public sealed class PermissionGrant
{
    public Guid Id { get; set; }
    public Guid SubjectPrincipalId { get; set; }
    public Principal SubjectPrincipal { get; set; } = null!;
    public Guid PermissionId { get; set; }
    public Permission Permission { get; set; } = null!;
    public Guid? ResourcePrincipalId { get; set; }
    public Principal? ResourcePrincipal { get; set; }
    public Guid? GrantedByPrincipalId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
