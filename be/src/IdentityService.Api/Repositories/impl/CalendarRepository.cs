using IdentityService.Api.Data;
using IdentityService.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace IdentityService.Api.Repositories;

public sealed class CalendarRepository(ApplicationDbContext dbContext) : ICalendarRepository
{
    public async Task<bool> AudienceExistsAsync(
        Guid creatorUserId,
        IReadOnlyCollection<Guid> userIds,
        IReadOnlyCollection<Guid> groupIds,
        CancellationToken cancellationToken)
    {
        if (!await dbContext.Users.AnyAsync(x => x.Id == creatorUserId, cancellationToken))
        {
            return false;
        }

        return await dbContext.Users.CountAsync(x => userIds.Contains(x.Id), cancellationToken) == userIds.Count &&
            await dbContext.Groups.CountAsync(x => groupIds.Contains(x.Id), cancellationToken) == groupIds.Count;
    }

    public async Task AddEventAsync(Event calendarEvent, CancellationToken cancellationToken)
    {
        dbContext.Events.Add(calendarEvent);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public Task<User?> GetUserAsync(Guid userId, CancellationToken cancellationToken) =>
        dbContext.Users.AsNoTracking().SingleOrDefaultAsync(x => x.Id == userId, cancellationToken);

    public async Task<IReadOnlyList<Event>> GetEventsForUserAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        var groupIds = await (
                from membership in dbContext.PrincipalMemberships.AsNoTracking()
                join grp in dbContext.Groups on membership.PrincipalId equals grp.PrincipalId
                where membership.UserId == userId && membership.LeftAtUtc == null
                select grp.Id)
            .ToArrayAsync(cancellationToken);
        return await dbContext.Events.AsNoTracking()
            .Include(x => x.Translations)
            .Include(x => x.Reminders)
            .Where(x => x.Status == EventStatus.Active && x.Participants.Any(p =>
                p.Status == EventParticipantStatus.Active &&
                (p.UserId == userId || (p.GroupId != null && groupIds.Contains(p.GroupId.Value)))))
            .OrderBy(x => x.StartAtUtc)
            .ToArrayAsync(cancellationToken);
    }

    public Task<Event?> GetEventForUpdateAsync(Guid eventId, CancellationToken cancellationToken) =>
        dbContext.Events.SingleOrDefaultAsync(x => x.Id == eventId, cancellationToken);

    public async Task<IReadOnlyList<Notification>> GetNotificationsAsync(
        Guid userId,
        CancellationToken cancellationToken) =>
        await dbContext.Notifications.AsNoTracking()
            .Where(x => x.RecipientUserId == userId)
            .OrderByDescending(x => x.SentAtUtc)
            .ToArrayAsync(cancellationToken);

    public Task<Notification?> GetNotificationForUpdateAsync(
        Guid userId,
        Guid notificationId,
        CancellationToken cancellationToken) =>
        dbContext.Notifications.SingleOrDefaultAsync(
            x => x.Id == notificationId && x.RecipientUserId == userId,
            cancellationToken);

    public async Task<IReadOnlyList<Event>> GetEventsByDayAsync(
        Guid userId,
        DateTimeOffset startAtUtc,
        DateTimeOffset endAtUtc,
        CancellationToken cancellationToken)
    {
        var groupIds = await (
                from membership in dbContext.PrincipalMemberships.AsNoTracking()
                join grp in dbContext.Groups on membership.PrincipalId equals grp.PrincipalId
                where membership.UserId == userId && membership.LeftAtUtc == null
                select grp.Id)
            .ToArrayAsync(cancellationToken);
        return await dbContext.Events.AsNoTracking()
            .Include(x => x.Translations)
            .Include(x => x.Reminders)
            .Where(x => x.Status == EventStatus.Active &&
                x.StartAtUtc >= startAtUtc && x.StartAtUtc < endAtUtc &&
                x.Participants.Any(p =>
                    p.Status == EventParticipantStatus.Active &&
                    (p.UserId == userId || (p.GroupId != null && groupIds.Contains(p.GroupId.Value)))))
            .OrderBy(x => x.StartAtUtc)
            .ToArrayAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Reminder>> GetDueRemindersAsync(
        DateTimeOffset now,
        CancellationToken cancellationToken) =>
        await dbContext.Reminders.AsSplitQuery()
            .Include(x => x.Event).ThenInclude(x => x.Participants)
            .Include(x => x.Event).ThenInclude(x => x.Translations)
            .Where(x => x.Status == ReminderStatus.Active && x.NextNotifyAtUtc <= now)
            .OrderBy(x => x.NextNotifyAtUtc)
            .Take(100)
            .ToArrayAsync(cancellationToken);

    public async Task<IReadOnlyDictionary<Guid, IReadOnlyList<User>>> GetActiveRecipientsByEventAsync(
        IReadOnlyCollection<Guid> eventIds,
        CancellationToken cancellationToken)
    {
        var directRecipients = await dbContext.EventParticipants.AsNoTracking()
            .Where(x => eventIds.Contains(x.EventId) &&
                x.Status == EventParticipantStatus.Active && x.UserId != null)
            .Select(x => new { x.EventId, UserId = x.UserId!.Value })
            .ToArrayAsync(cancellationToken);
        var groupRecipients = await dbContext.EventParticipants.AsNoTracking()
            .Where(x => eventIds.Contains(x.EventId) &&
                x.Status == EventParticipantStatus.Active && x.GroupId != null)
            .Join(
                dbContext.Groups.AsNoTracking(),
                participant => participant.GroupId!.Value,
                group => group.Id,
                (participant, group) => new { participant, group.PrincipalId })
            .Join(
                dbContext.PrincipalMemberships.AsNoTracking().Where(x => x.LeftAtUtc == null),
                item => item.PrincipalId,
                membership => membership.PrincipalId,
                (item, membership) => new { item.participant.EventId, membership.UserId })
            .ToArrayAsync(cancellationToken);
        var recipientIdsByEvent = directRecipients
            .Select(x => (x.EventId, x.UserId))
            .Concat(groupRecipients.Select(x => (x.EventId, x.UserId)))
            .GroupBy(x => x.EventId)
            .ToDictionary(x => x.Key, x => x.Select(y => y.UserId).Distinct().ToArray());
        var recipientIds = recipientIdsByEvent.Values.SelectMany(x => x).Distinct().ToArray();
        var users = await dbContext.Users.AsNoTracking()
            .Where(x => recipientIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, cancellationToken);
        return recipientIdsByEvent.ToDictionary(
            x => x.Key,
            x => (IReadOnlyList<User>)x.Value
                .Where(users.ContainsKey)
                .Select(id => users[id])
                .ToArray());
    }

    public async Task<ISet<DeliveredNotificationKey>> GetDeliveredNotificationKeysAsync(
        IReadOnlyCollection<Guid> reminderIds,
        DateTimeOffset earliestOccurrenceStartAtUtc,
        DateTimeOffset latestOccurrenceStartAtUtc,
        CancellationToken cancellationToken) =>
        (await dbContext.Notifications.AsNoTracking()
            .Where(x => reminderIds.Contains(x.ReminderId) &&
                x.OccurrenceStartAtUtc >= earliestOccurrenceStartAtUtc &&
                x.OccurrenceStartAtUtc <= latestOccurrenceStartAtUtc)
            .Select(x => new DeliveredNotificationKey(
                x.ReminderId,
                x.RecipientUserId,
                x.OccurrenceStartAtUtc))
            .ToArrayAsync(cancellationToken))
        .ToHashSet();

    public void AddNotification(Notification notification) => dbContext.Notifications.Add(notification);

    public Task SaveChangesAsync(CancellationToken cancellationToken) =>
        dbContext.SaveChangesAsync(cancellationToken);
}
