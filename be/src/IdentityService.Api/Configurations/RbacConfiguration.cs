namespace IdentityService.Api.Configurations;

using IdentityService.Api.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public sealed class PrincipalConfiguration : IEntityTypeConfiguration<Principal>
{
    public void Configure(EntityTypeBuilder<Principal> builder)
    {
        builder.ToTable("principals");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id)
            .HasColumnName("id");
        builder.Property(p => p.Type)
            .HasColumnName("type")
            .HasConversion<string>()
            .IsRequired()
            .HasMaxLength(100);
        builder.HasIndex(p => p.Type);
    }
}

public sealed class ScopeConfiguration : IEntityTypeConfiguration<Scope>
{
    public void Configure(EntityTypeBuilder<Scope> builder)
    {
        builder.ToTable("scopes");
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id)
            .HasColumnName("id");
        builder.Property(s => s.Type)
            .HasColumnName("type")
            .HasConversion<string>()
            .IsRequired()
            .HasMaxLength(100);
        builder.HasIndex(s => s.Type);
    }
}

public sealed class PermissionConfiguration : IEntityTypeConfiguration<Permission>
{
    public void Configure(EntityTypeBuilder<Permission> builder)
    {
        builder.ToTable("permissions");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id)
            .HasColumnName("id");
        builder.Property(p => p.Name)
            .HasColumnName("name")
            .IsRequired()
            .HasMaxLength(100);
        builder.Property(p => p.Description)
            .HasColumnName("description")
            .HasMaxLength(1_000);
        builder.HasIndex(p => p.Name).IsUnique();
    }
}

public sealed class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> builder)
    {
        builder.ToTable("roles");
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id)
            .HasColumnName("id");
        builder.Property(r => r.Name)
            .HasColumnName("name")
            .IsRequired()
            .HasMaxLength(100);
        builder.Property(r => r.Description)
            .HasColumnName("description")
            .HasMaxLength(1_000);
        builder.HasIndex(r => r.Name).IsUnique();
    }
}

public sealed class RolePermissionConfiguration : IEntityTypeConfiguration<RolePermission>
{
    public void Configure(EntityTypeBuilder<RolePermission> builder)
    {
        builder.ToTable("role_permissions");
        builder.HasKey(rp => new { rp.RoleId, rp.PermissionId });
        builder.Property(rp => rp.RoleId)
            .HasColumnName("role_id");
        builder.Property(rp => rp.PermissionId)
            .HasColumnName("permission_id");
        builder.HasOne(rp => rp.Role)
            .WithMany(r => r.RolePermissions)
            .HasForeignKey(rp => rp.RoleId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(rp => rp.Permission)
            .WithMany(p => p.RolePermissions)
            .HasForeignKey(rp => rp.PermissionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class RoleAssignmentConfiguration : IEntityTypeConfiguration<RoleAssignment>
{
    public void Configure(EntityTypeBuilder<RoleAssignment> builder)
    {
        builder.ToTable("role_assignments");
        builder.HasKey(ra => ra.Id);
        builder.Property(ra => ra.Id)
            .HasColumnName("id");
        builder.Property(ra => ra.RoleId)
            .HasColumnName("role_id")
            .IsRequired();
        builder.Property(ra => ra.PrincipalId)
            .HasColumnName("principal_id")
            .IsRequired();
        builder.Property(ra => ra.ScopeId)
            .HasColumnName("scope_id")
            .IsRequired();
        builder.Property(ra => ra.GrantedByPrincipalId)
            .HasColumnName("granted_by_principal_id");
        builder.Property(ra => ra.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.HasOne(ra => ra.Role)
            .WithMany(r => r.Assignments)
            .HasForeignKey(ra => ra.RoleId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(ra => ra.Principal)
            .WithMany(p => p.Assignments)
            .HasForeignKey(ra => ra.PrincipalId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(ra => ra.Scope)
            .WithMany(s => s.Assignments)
            .HasForeignKey(ra => ra.ScopeId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(ra => new { ra.PrincipalId, ra.RoleId, ra.ScopeId }).IsUnique();
    }
}

