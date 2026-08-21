using System.ComponentModel.DataAnnotations;
using IdentityService.Api.Entities;

namespace IdentityService.Api.DTOs.Calendar;

public sealed record EventTranslationRequest(
    [param: Required, MaxLength(16)] string Language,
    [param: Required, MaxLength(200)] string Title,
    [param: MaxLength(4_000)] string? Description);

public sealed record CreateEventRequest
{
    public required DateTimeOffset StartAt { get; init; }
    public DateTimeOffset? EndAt { get; init; }
    [Required, MaxLength(128)]
    public string TimeZoneId { get; init; } = "UTC";
    public bool IsRecurring { get; init; }
    public IReadOnlyList<DayOfWeek> RecurringWeekdays { get; init; } = [];
    public DateTimeOffset? RecurrenceEndAt { get; init; }
    [MinLength(1)]
    public IReadOnlyList<EventTranslationRequest> Translations { get; init; } = [];
    public IReadOnlyList<Guid> UserIds { get; init; } = [];
    public IReadOnlyList<Guid> GroupIds { get; init; } = [];
    public IReadOnlyList<ReminderRequest> Reminders { get; init; } = [];
}

public sealed record ReminderRequest(
    int RemindBeforeMinutes,
    int? RepeatEveryMinutes);

public sealed record ReminderResponse(
    int RemindBeforeMinutes,
    int? RepeatEveryMinutes);

public sealed record CalendarEventResponse(
    Guid Id,
    DateTimeOffset StartAt,
    DateTimeOffset? EndAt,
    string TimeZoneId,
    EventStatus Status,
    bool IsRecurring,
    IReadOnlyList<DayOfWeek> RecurringWeekdays,
    DateTimeOffset? RecurrenceEndAt,
    string Title,
    string? Description,
    IReadOnlyList<ReminderResponse> Reminders);

public sealed record NotificationResponse(
    Guid Id,
    Guid EventId,
    DateTimeOffset OccurrenceStartAt,
    string Title,
    string? Description,
    DateTimeOffset SentAt,
    DateTimeOffset? ReadAt);
