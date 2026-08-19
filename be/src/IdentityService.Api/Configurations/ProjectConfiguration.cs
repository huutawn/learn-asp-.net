namespace IdentityService.Api.Configurations;

using IdentityService.Api.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public sealed class ProjectConfiguration : IEntityTypeConfiguration<Project>
{
    public void Configure(EntityTypeBuilder<Project> builder)
    {
        builder.ToTable("projects");

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

        builder.Property(p => p.Type)
            .HasColumnName("type")
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(p => p.ScopeId)
            .HasColumnName("scope_id");

        builder.Property(p => p.PrincipalId)
            .HasColumnName("principal_id")
            .IsRequired();

        builder.Property(p => p.OwnerId)
            .HasColumnName("owner_id")
            .IsRequired();

        builder.Property(p => p.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .IsRequired();

        builder.Property(p => p.UpdatedAtUtc)
            .HasColumnName("updated_at_utc")
            .IsRequired();

        builder.HasOne(p => p.Principal)
            .WithMany()
            .HasForeignKey(p => p.PrincipalId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(p => p.Scope)
            .WithMany()
            .HasForeignKey(p => p.ScopeId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(p => p.Owner)
            .WithMany()
            .HasForeignKey(p => p.OwnerId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class ProjectTranslationConfiguration : IEntityTypeConfiguration<ProjectTranslation>
{
    public void Configure(EntityTypeBuilder<ProjectTranslation> builder)
    {
        builder.ToTable("project_translations");
        builder.HasKey(x => new { x.ProjectId, x.Language });
        builder.Property(x => x.ProjectId).HasColumnName("project_id");
        builder.Property(x => x.Language).HasColumnName("language").HasMaxLength(16);
        builder.Property(x => x.Name).HasColumnName("name").HasMaxLength(200).IsRequired();
        builder.Property(x => x.Description).HasColumnName("description");
        builder.HasOne(x => x.Project).WithMany().HasForeignKey(x => x.ProjectId).OnDelete(DeleteBehavior.Cascade);
    }
}