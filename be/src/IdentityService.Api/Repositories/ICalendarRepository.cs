using IdentityService.Api.Entities;

namespace IdentityService.Api.Repositories;

public interface ICalendarRepository
{
    Task<bool> AudienceExistsAsync(
        Guid creatorUserId,
        IReadOnlyCollection<Guid> userIds,
        IReadOnlyCollection<Guid> groupIds,
        CancellationToken cancellationToken);
    Task AddEventAsync(Event calendarEvent, CancellationToken cancellationToken);
    Task<User?> GetUserAsync(Guid userId, CancellationToken cancellationToken);
    Task<IReadOnlyList<Event>> GetEventsForUserAsync(Guid userId, CancellationToken cancellationToken);
    Task<Event?> GetEventForUpdateAsync(Guid eventId, CancellationToken cancellationToken);
    Task<IReadOnlyList<User>> SearchUsersAsync(
        string query,
        int limit,
        CancellationToken cancellationToken);
    Task<IReadOnlyList<User>> SearchUsersForEventAsync(
        Guid eventId,
        string query,
        int limit,
        CancellationToken cancellationToken);
    Task<User?> GetUserForEventParticipantAsync(Guid userId, CancellationToken cancellationToken);
    Task<IReadOnlyList<Notification>> GetNotificationsAsync(Guid userId, CancellationToken cancellationToken);
    Task<Notification?> GetNotificationForUpdateAsync(
        Guid userId,
        Guid notificationId,
        CancellationToken cancellationToken);
    Task<IReadOnlyList<Event>> GetEventsByDayAsync(
        Guid userId,
        DateTimeOffset startAtUtc,
        DateTimeOffset endAtUtc,
        CancellationToken cancellationToken);
    Task<IReadOnlyDictionary<Guid, IReadOnlyList<User>>> GetActiveRecipientsByEventAsync(
        IReadOnlyCollection<Guid> eventIds,
        CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
