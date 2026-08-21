using System.Security.Cryptography;
using System.Text;
using IdentityService.Api.Data;
using IdentityService.Api.DTOs.Calendar;
using IdentityService.Api.Entities;
using IdentityService.Api.Hub;
using IdentityService.Api.Messaging;
using IdentityService.Api.Repositories;
using Microsoft.EntityFrameworkCore;

namespace IdentityService.Api.Services;

public sealed class NotificationDeliveryService(
    ApplicationDbContext dbContext,
    ICalendarRepository calendarRepository,
    IHub hub,
    TimeProvider timeProvider) : INotificationDeliveryService
{
    public async Task DeliverAsync(ReminderDueMessage message, CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();
        var calendarEvent = await dbContext.Events
            .Include(x => x.Translations)
            .SingleOrDefaultAsync(x => x.Id == message.EventId, cancellationToken);
        if (calendarEvent is null || calendarEvent.Status != EventStatus.Active || message.OccurrenceStartAtUtc <= now)
        {
            return;
        }

        var reminderExists = await dbContext.Reminders.AnyAsync(
            x => x.Id == message.ReminderId && x.EventId == message.EventId,
            cancellationToken);
        if (!reminderExists)
        {
            return;
        }

        var recipientsByEvent = await calendarRepository.GetActiveRecipientsByEventAsync([message.EventId], cancellationToken);
        if (!recipientsByEvent.TryGetValue(message.EventId, out var recipients))
        {
            return;
        }

        var recipientIds = recipients.Select(x => x.Id).ToArray();
        var keys = recipientIds.ToDictionary(
            id => id,
            id => NotificationIdempotencyKey.Create(message.ReminderId, message.OccurrenceStartAtUtc, id));
        var existingNotifications = await dbContext.Notifications
            .Where(x => keys.Values.Contains(x.IdempotencyKey))
            .ToDictionaryAsync(x => x.IdempotencyKey, cancellationToken);

        foreach (var recipient in recipients)
        {
            var key = keys[recipient.Id];
            if (existingNotifications.ContainsKey(key))
            {
                continue;
            }

            var translation = SelectTranslation(calendarEvent.Translations, recipient.Language);
            var notification = new Notification
            {
                Id = Guid.NewGuid(),
                ReminderId = message.ReminderId,
                EventId = message.EventId,
                RecipientUserId = recipient.Id,
                OccurrenceStartAtUtc = message.OccurrenceStartAtUtc,
                Title = translation.Title,
                Description = translation.Description,
                SentAtUtc = now,
                IdempotencyKey = key
            };
            dbContext.Notifications.Add(notification);
            existingNotifications.Add(key, notification);
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        foreach (var recipient in recipients)
        {
            var notification = existingNotifications[keys[recipient.Id]];
            await hub.SendToUserAsync(
                recipient.Id,
                NotificationHubMethods.Notification,
                new NotificationResponse(
                    notification.Id,
                    notification.EventId,
                    notification.OccurrenceStartAtUtc,
                    notification.Title,
                    notification.Description,
                    notification.SentAtUtc,
                    notification.ReadAtUtc),
                cancellationToken);
        }
    }

    private static EventTranslation SelectTranslation(IEnumerable<EventTranslation> translations, string language)
    {
        var all = translations.ToArray();
        var normalized = language.Trim().ToLowerInvariant();
        return all.FirstOrDefault(x => x.Language.Equals(normalized, StringComparison.OrdinalIgnoreCase))
            ?? all.FirstOrDefault(x => x.Language.Equals(normalized.Split('-')[0], StringComparison.OrdinalIgnoreCase))
            ?? all.FirstOrDefault(x => x.Language.Equals("en", StringComparison.OrdinalIgnoreCase))
            ?? all[0];
    }
}

public static class NotificationIdempotencyKey
{
    public static string Create(Guid reminderId, DateTimeOffset occurrenceStartAtUtc, Guid recipientUserId)
    {
        var value = $"{reminderId:D}:{occurrenceStartAtUtc.UtcTicks}:{recipientUserId:D}";
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();
    }
}
