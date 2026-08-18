using IdentityService.Api.Entities;

namespace IdentityService.Api.Repositories;

public readonly record struct DeliveredNotificationKey(
    Guid ReminderId,
    Guid RecipientUserId,
    DateTimeOffset OccurrenceStartAtUtc);

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
    Task<IReadOnlyList<Reminder>> GetDueRemindersAsync(
        DateTimeOffset now,
        CancellationToken cancellationToken);
    Task<IReadOnlyDictionary<Guid, IReadOnlyList<User>>> GetActiveRecipientsByEventAsync(
        IReadOnlyCollection<Guid> eventIds,
        CancellationToken cancellationToken);
    Task<ISet<DeliveredNotificationKey>> GetDeliveredNotificationKeysAsync(
        IReadOnlyCollection<Guid> reminderIds,
        DateTimeOffset earliestOccurrenceStartAtUtc,
        DateTimeOffset latestOccurrenceStartAtUtc,
        CancellationToken cancellationToken);
    void AddNotification(Notification notification);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
