namespace IdentityService.Api.Entities;

public enum EventStatus
{
    Active,
    Cancelled
}

public enum EventParticipantStatus
{
    Active,
    Removed
}

public enum ReminderStatus
{
    Active,
    Completed,
    Cancelled
}

public sealed class Event
{
    public Guid Id { get; set; }
    public Guid CreatedById { get; set; }
    public User CreatedBy { get; set; } = null!;
    public DateTimeOffset StartAtUtc { get; set; }
    public DateTimeOffset? EndAtUtc { get; set; }
    public string TimeZoneId { get; set; } = null!;
    public EventStatus Status { get; set; } = EventStatus.Active;
    public bool IsRecurring { get; set; }
    // PostgreSQL weekday convention: 0 = Sunday through 6 = Saturday.
    public short[] RecurrenceWeekdays { get; set; } = [];
    public DateTimeOffset? RecurrenceEndAtUtc { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
    public ICollection<EventParticipant> Participants { get; } = new List<EventParticipant>();
    public ICollection<EventTranslation> Translations { get; } = new List<EventTranslation>();
    public ICollection<Reminder> Reminders { get; } = new List<Reminder>();

    public DateTimeOffset? NextOccurrenceStartAfter(DateTimeOffset occurrenceStartUtc)
    {
        if (!IsRecurring)
        {
            return null;
        }

        var timeZone = TimeZoneInfo.FindSystemTimeZoneById(TimeZoneId);
        var localStart = TimeZoneInfo.ConvertTime(occurrenceStartUtc, timeZone).DateTime;
        var days = RecurrenceWeekdays.Select(day => (DayOfWeek)day).ToHashSet();

        for (var offset = 1; offset <= 7; offset++)
        {
            var localOccurrence = localStart.Date.AddDays(offset).Add(localStart.TimeOfDay);
            if (!days.Contains(localOccurrence.DayOfWeek))
            {
                continue;
            }

            while (timeZone.IsInvalidTime(localOccurrence))
            {
                localOccurrence = localOccurrence.AddMinutes(1);
            }

            var next = new DateTimeOffset(
                TimeZoneInfo.ConvertTimeToUtc(localOccurrence, timeZone),
                TimeSpan.Zero);
            return RecurrenceEndAtUtc is not null && next > RecurrenceEndAtUtc
                ? null
                : next;
        }

        return null;
    }
}

public sealed class EventParticipant
{
    public Guid Id { get; set; }
    public Guid EventId { get; set; }
    public Event Event { get; set; } = null!;
    public Guid? UserId { get; set; }
    public User? User { get; set; }
    public Guid? GroupId { get; set; }
    public Group? Group { get; set; }
    public EventParticipantStatus Status { get; set; } = EventParticipantStatus.Active;
}

public sealed class EventTranslation
{
    public Guid EventId { get; set; }
    public Event Event { get; set; } = null!;
    public string Language { get; set; } = null!;
    public string Title { get; set; } = null!;
    public string? Description { get; set; }
}

public sealed class Reminder
{
    public Guid Id { get; set; }
    public Guid EventId { get; set; }
    public Event Event { get; set; } = null!;
    public int RemindBeforeMinutes { get; set; }
    public int? RepeatEveryMinutes { get; set; }
    public DateTimeOffset NextOccurrenceStartAtUtc { get; set; }
    public DateTimeOffset NextReminderAtUtc { get; set; }
    public ReminderStatus Status { get; set; } = ReminderStatus.Active;
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
}

public sealed class UserPosition
{
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    public Guid PositionId { get; set; }
    public Position Position { get; set; } = null!;
}

public sealed class Notification
{
    public Guid Id { get; set; }
    public Guid ReminderId { get; set; }
    public Reminder Reminder { get; set; } = null!;
    public Guid EventId { get; set; }
    public Event Event { get; set; } = null!;
    public Guid RecipientUserId { get; set; }
    public User RecipientUser { get; set; } = null!;
    public DateTimeOffset OccurrenceStartAtUtc { get; set; }
    public DateTimeOffset ReminderScheduledAtUtc { get; set; }
    public string Title { get; set; } = null!;
    public string? Description { get; set; }
    public DateTimeOffset SentAtUtc { get; set; }
    public DateTimeOffset? ReadAtUtc { get; set; }
    public string IdempotencyKey { get; set; } = null!;
}

public enum OutboxMessageStatus
{
    Pending,
    Publishing,
    Published
}

public sealed class OutboxMessage
{
    public Guid Id { get; set; }
    public Guid ReminderId { get; set; }
    public DateTimeOffset OccurrenceStartAtUtc { get; set; }
    public DateTimeOffset ScheduledReminderAtUtc { get; set; }
    public string Topic { get; set; } = null!;
    public string Payload { get; set; } = null!;
    public OutboxMessageStatus Status { get; set; } = OutboxMessageStatus.Pending;
    public int AttemptCount { get; set; }
    public DateTimeOffset NextAttemptAtUtc { get; set; }
    public DateTimeOffset? PublishingLeaseExpiresAtUtc { get; set; }
    public string? LastError { get; set; }
    public DateTimeOffset? PublishedAtUtc { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
}
