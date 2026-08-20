using IdentityService.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace IdentityService.Api.Data;

public sealed class ApplicationDbContext(
    DbContextOptions<ApplicationDbContext> options)
    : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Session> Sessions => Set<Session>();
    public DbSet<Group> Groups => Set<Group>();
    public DbSet<Position> Positions => Set<Position>();
    public DbSet<PrincipalMembership> PrincipalMemberships => Set<PrincipalMembership>();
    public DbSet<UserPosition> UserPositions => Set<UserPosition>();
    public DbSet<Event> Events => Set<Event>();
    public DbSet<EventParticipant> EventParticipants => Set<EventParticipant>();
    public DbSet<EventTranslation> EventTranslations => Set<EventTranslation>();
    public DbSet<Reminder> Reminders => Set<Reminder>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<Project> Projects => Set<Project>();
    public DbSet<ProjectTranslation> ProjectTranslations => Set<ProjectTranslation>();
    public DbSet<Team> Teams => Set<Team>();
    public DbSet<Principal> Principals => Set<Principal>();
    public DbSet<Scope> Scopes => Set<Scope>();
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
    public DbSet<RoleAssignment> RoleAssignments => Set<RoleAssignment>();
    protected override void OnModelCreating(
        ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(ApplicationDbContext).Assembly);
    }
}
