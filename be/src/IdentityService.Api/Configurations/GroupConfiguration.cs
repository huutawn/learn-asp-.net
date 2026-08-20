namespace IdentityService.Api.Configurations;

using IdentityService.Api.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public sealed class GroupConfiguration : IEntityTypeConfiguration<Group>
{
    public void Configure(EntityTypeBuilder<Group> builder)
    {
        builder.ToTable("groups");

        builder.HasKey(g => g.Id);

        builder.Property(g => g.Id)
            .HasColumnName("id");

        builder.Property(g => g.Name)
            .HasColumnName("name")
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(g => g.Description)
            .HasColumnName("description")
            .HasMaxLength(1_000);

        builder.Property(g => g.Type)
            .HasColumnName("type")
            .IsRequired()
            .HasMaxLength(64);
        
        builder.Property(g => g.PrincipalId)
            .HasColumnName("principal_id");

        builder.HasOne(g => g.Principal)
            .WithOne(p => p.Group)
            .HasForeignKey<Group>(g => g.PrincipalId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);
        
        builder.HasIndex(g => new { g.Name, g.Type }).IsUnique();
    }
}
