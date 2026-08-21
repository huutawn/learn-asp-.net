using IdentityService.Api.DTOs.Calendar;

namespace IdentityService.Api.Services;

public interface ICalendarService
{
    Task<CalendarEventResponse> CreateAsync(
        Guid creatorUserId,
        CreateEventRequest request,
        CancellationToken cancellationToken);
    Task<IReadOnlyList<CalendarEventResponse>> GetForUserAsync(
        Guid userId,
        CancellationToken cancellationToken);
    Task<IReadOnlyCollection<CalendarEventResponse>> GetEventsByDayAsync(
        Guid userId,
        DateTimeOffset day,
        CancellationToken cancellationToken);
    Task<bool> CancelAsync(Guid actorUserId, Guid eventId, CancellationToken cancellationToken);
    Task<bool> CancelReminderAsync(
        Guid actorUserId,
        Guid eventId,
        Guid reminderId,
        CancellationToken cancellationToken);
    Task<IReadOnlyList<CalendarEventMemberResponse>> SearchUsersAsync(
        string query,
        CancellationToken cancellationToken);
    Task<IReadOnlyList<CalendarEventMemberResponse>?> SearchParticipantsAsync(
        Guid actorUserId,
        Guid eventId,
        string query,
        CancellationToken cancellationToken);
    Task<CalendarEventMemberResponse?> AddParticipantAsync(
        Guid actorUserId,
        Guid eventId,
        Guid userId,
        CancellationToken cancellationToken);
    Task<IReadOnlyList<NotificationResponse>> GetNotificationsAsync(
        Guid userId,
        CancellationToken cancellationToken);
    Task<bool> MarkNotificationReadAsync(
        Guid userId,
        Guid notificationId,
        CancellationToken cancellationToken);
}
