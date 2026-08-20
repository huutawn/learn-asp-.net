using IdentityService.Api.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IdentityService.Api.Configurations;


public sealed class PositionConfiguration : IEntityTypeConfiguration<Position>
{
    public void Configure(EntityTypeBuilder<Position> builder)
    {
        builder.ToTable("positions");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.Name).HasColumnName("name").HasMaxLength(100).IsRequired();
        builder.Property(x => x.Description).HasColumnName("description").HasMaxLength(1_000);
        builder.HasIndex(x => x.Name).IsUnique();
    }
}

public sealed class PrincipalMembershipConfiguration : IEntityTypeConfiguration<PrincipalMembership>
{
    public void Configure(EntityTypeBuilder<PrincipalMembership> builder)
    {
        builder.ToTable("principal_memberships");
        builder.HasKey(x => new { x.UserId, x.PrincipalId });
        builder.Property(x => x.UserId).HasColumnName("user_id");
        builder.Property(x => x.PrincipalId).HasColumnName("principal_id");
        builder.Property(x => x.JoinedAtUtc).HasColumnName("joined_at_utc").IsRequired();
        builder.Property(x => x.LeftAtUtc).HasColumnName("left_at_utc");
        builder.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.Principal).WithMany().HasForeignKey(x => x.PrincipalId).OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(x => new { x.PrincipalId, x.LeftAtUtc });
    }
}

public sealed class UserPositionConfiguration : IEntityTypeConfiguration<UserPosition>
{
    public void Configure(EntityTypeBuilder<UserPosition> builder)
    {
        builder.ToTable("user_positions");
        builder.HasKey(x => new { x.UserId, x.PositionId });
        builder.Property(x => x.UserId).HasColumnName("user_id");
        builder.Property(x => x.PositionId).HasColumnName("position_id");
        builder.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.Position).WithMany().HasForeignKey(x => x.PositionId).OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class EventConfiguration : IEntityTypeConfiguration<Event>
{
    public void Configure(EntityTypeBuilder<Event> builder)
    {
        builder.ToTable("events");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.CreatedById).HasColumnName("created_by").IsRequired();
        builder.Property(x => x.StartAtUtc).HasColumnName("start_at_utc").IsRequired();
        builder.Property(x => x.EndAtUtc).HasColumnName("end_at_utc");
        builder.Property(x => x.TimeZoneId).HasColumnName("timezone").HasMaxLength(128).IsRequired();
        builder.Property(x => x.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(x => x.IsRecurring).HasColumnName("is_recurring").IsRequired();
        builder.Property(x => x.RecurrenceDaysCsv).HasColumnName("recurrence_dates").HasMaxLength(64).IsRequired();
        builder.Property(x => x.RecurrenceEndAtUtc).HasColumnName("recurrence_end_at_utc");
        builder.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc").IsRequired();
        builder.Property(x => x.UpdatedAtUtc).HasColumnName("updated_at_utc").IsRequired();
        builder.HasOne(x => x.CreatedBy).WithMany().HasForeignKey(x => x.CreatedById).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => new { x.Status, x.StartAtUtc });
    }
}

public sealed class EventParticipantConfiguration : IEntityTypeConfiguration<EventParticipant>
{
    public void Configure(EntityTypeBuilder<EventParticipant> builder)
    {
        builder.ToTable("events_participants", x =>
            x.HasCheckConstraint("ck_events_participants_one_target", "(user_id IS NULL) <> (group_id IS NULL)"));
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.EventId).HasColumnName("event_id").IsRequired();
        builder.Property(x => x.UserId).HasColumnName("user_id");
        builder.Property(x => x.GroupId).HasColumnName("group_id");
        builder.Property(x => x.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.HasOne(x => x.Event).WithMany(x => x.Participants).HasForeignKey(x => x.EventId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.Group).WithMany().HasForeignKey(x => x.GroupId).OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(x => new { x.EventId, x.UserId }).IsUnique();
        builder.HasIndex(x => new { x.EventId, x.GroupId }).IsUnique();
    }
}

public sealed class EventTranslationConfiguration : IEntityTypeConfiguration<EventTranslation>
{
    public void Configure(EntityTypeBuilder<EventTranslation> builder)
    {
        builder.ToTable("event_translations");
        builder.HasKey(x => new { x.EventId, x.Language });
        builder.Property(x => x.EventId).HasColumnName("event_id");
        builder.Property(x => x.Language).HasColumnName("language").HasMaxLength(16);
        builder.Property(x => x.Title).HasColumnName("title").HasMaxLength(200).IsRequired();
        builder.Property(x => x.Description).HasColumnName("description");
        builder.HasOne(x => x.Event).WithMany(x => x.Translations).HasForeignKey(x => x.EventId).OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class ReminderConfiguration : IEntityTypeConfiguration<Reminder>
{
    public void Configure(EntityTypeBuilder<Reminder> builder)
    {
        builder.ToTable("reminders");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.EventId).HasColumnName("event_id").IsRequired();
        builder.Property(x => x.RemindBeforeMinutes).HasColumnName("remind_before_minutes").IsRequired();
        builder.Property(x => x.NextNotifyAtUtc).HasColumnName("next_notify_at_utc").IsRequired();
        builder.Property(x => x.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc").IsRequired();
        builder.Property(x => x.UpdatedAtUtc).HasColumnName("updated_at_utc").IsRequired();
        builder.HasOne(x => x.Event).WithMany(x => x.Reminders).HasForeignKey(x => x.EventId).OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(x => new { x.Status, x.NextNotifyAtUtc });
        builder.HasIndex(x => new { x.EventId, x.RemindBeforeMinutes }).IsUnique();
    }
}

public sealed class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.ToTable("notifications");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.ReminderId).HasColumnName("reminder_id").IsRequired();
        builder.Property(x => x.EventId).HasColumnName("event_id").IsRequired();
        builder.Property(x => x.RecipientUserId).HasColumnName("recipient_user_id").IsRequired();
        builder.Property(x => x.OccurrenceStartAtUtc).HasColumnName("occurrence_start_at_utc").IsRequired();
        builder.Property(x => x.Title).HasColumnName("title").HasMaxLength(200).IsRequired();
        builder.Property(x => x.Description).HasColumnName("description");
        builder.Property(x => x.SentAtUtc).HasColumnName("sent_at_utc").IsRequired();
        builder.Property(x => x.ReadAtUtc).HasColumnName("read_at_utc");
        builder.HasOne(x => x.Reminder).WithMany().HasForeignKey(x => x.ReminderId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.Event).WithMany().HasForeignKey(x => x.EventId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.RecipientUser).WithMany().HasForeignKey(x => x.RecipientUserId).OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(x => new { x.ReminderId, x.RecipientUserId, x.OccurrenceStartAtUtc }).IsUnique();
        builder.HasIndex(x => new { x.RecipientUserId, x.ReadAtUtc, x.SentAtUtc });
    }
}
