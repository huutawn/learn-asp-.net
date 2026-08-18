using IdentityService.Api.DTOs.Calendar;
using IdentityService.Api.Entities;
using IdentityService.Api.Exceptions;
using IdentityService.Api.Messaging;
using IdentityService.Api.Repositories;

namespace IdentityService.Api.Services;

public sealed class CalendarService(
    ICalendarRepository calendarRepository,
    ICalendarEventPublisher eventPublisher,
    IDelayedEmailJobQueue emailJobQueue,
    TimeProvider timeProvider) : ICalendarService
{
    public async Task<CalendarEventResponse> CreateAsync(
        Guid creatorUserId,
        CreateEventRequest request,
        CancellationToken cancellationToken)
    {
        var timeZone = GetTimeZone(request.TimeZoneId);
        var startAt = request.StartAt.ToUniversalTime();
        var endAt = request.EndAt?.ToUniversalTime();
        ValidateSchedule(request, timeZone, startAt, endAt);

        var translations = NormalizeTranslations(request.Translations);
        var userIds = request.UserIds.Distinct().ToArray();
        var groupIds = request.GroupIds.Distinct().ToArray();
        

        if (request.ReminderMinutes.Any(x => x < 0 || x > 525_600))
        {
            throw new BadRequestException("Reminder minutes must be between zero and 525600.");
        }

        if (!await calendarRepository.AudienceExistsAsync(
                creatorUserId,
                userIds,
                groupIds,
                cancellationToken))
        {
            throw new BadRequestException("An event audience references an unknown user or group.");
        }

        var now = timeProvider.GetUtcNow();
        var recurrenceDays = request.RecurringWeekdays.Distinct().Order().ToArray();
        var calendarEvent = new Event
        {
            Id = Guid.NewGuid(),
            CreatedById = creatorUserId,
            StartAtUtc = startAt,
            EndAtUtc = endAt,
            TimeZoneId = timeZone.Id,
            IsRecurring = request.IsRecurring,
            RecurrenceDaysCsv = string.Join(',', recurrenceDays),
            RecurrenceEndAtUtc = request.RecurrenceEndAt?.ToUniversalTime(),
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };
        foreach (var translation in translations)
        {
            calendarEvent.Translations.Add(translation);
        }

        foreach (var userId in userIds)
        {
            calendarEvent.Participants.Add(new EventParticipant { Id = Guid.NewGuid(), UserId = userId });
        }

        foreach (var groupId in groupIds)
        {
            calendarEvent.Participants.Add(new EventParticipant { Id = Guid.NewGuid(), GroupId = groupId });
        }

        foreach (var minutes in request.ReminderMinutes.Distinct())
        {
            calendarEvent.Reminders.Add(new Reminder
            {
                Id = Guid.NewGuid(),
                RemindBeforeMinutes = minutes,
                NextNotifyAtUtc = startAt.AddMinutes(-minutes),
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            });
        }

        await calendarRepository.AddEventAsync(calendarEvent, cancellationToken);

        var recipientsByEvent = await calendarRepository.GetActiveRecipientsByEventAsync(
            [calendarEvent.Id],
            cancellationToken);
        recipientsByEvent.TryGetValue(calendarEvent.Id, out var recipients);
        recipients ??= [];

        foreach (var reminder in calendarEvent.Reminders)
        {
            var notifyAtUtc = reminder.NextNotifyAtUtc;
            foreach (var user in recipients)
            {
                var translation = SelectTranslation(calendarEvent.Translations, user.Language);
                var job = new DelayedEmailJob(
                    JobId: Guid.NewGuid(),
                    EventId: calendarEvent.Id,
                    ReminderId: reminder.Id,
                    RecipientUserId: user.Id,
                    RecipientEmail: user.Email,
                    Title: translation.Title,
                    Description: translation.Description,
                    OccurrenceStartAtUtc: startAt,
                    RemindBeforeMinutes: reminder.RemindBeforeMinutes);

                await emailJobQueue.EnqueueAsync(job, notifyAtUtc, cancellationToken);
            }
        }

        await eventPublisher.PublishAsync(
            new CalendarEventMessage(
                "calendar.event.created",
                calendarEvent.Id,
                creatorUserId,
                now),
            cancellationToken);
        return MapEvent(calendarEvent, "en");
    }
    public async Task<IReadOnlyCollection<CalendarEventResponse>> GetEventsByDayAsync(
        Guid userId,
        DateTimeOffset day,
        CancellationToken cancellationToken)
    {
        var user = await calendarRepository.GetUserAsync(userId, cancellationToken)
            ?? throw new NotFoundException("User not found.");

        var timeZone = GetTimeZone(user.TimeZoneId ?? "SE Asia Standard Time");

        // Convert the requested day to user's local date
        var userLocalTime = TimeZoneInfo.ConvertTime(day, timeZone);
        var localDate = DateOnly.FromDateTime(userLocalTime.DateTime);

        var localStart = localDate.ToDateTime(TimeOnly.MinValue);
        var localEnd = localDate.AddDays(1).ToDateTime(TimeOnly.MinValue);

        var startAtUtc = new DateTimeOffset(TimeZoneInfo.ConvertTimeToUtc(localStart, timeZone), TimeSpan.Zero);
        var endAtUtc = new DateTimeOffset(TimeZoneInfo.ConvertTimeToUtc(localEnd, timeZone), TimeSpan.Zero);

        var events = await calendarRepository.GetEventsByDayAsync(userId, startAtUtc, endAtUtc, cancellationToken);
        return events.Select(x => MapEvent(x, user.Language)).ToArray();
    }
    public async Task<IReadOnlyList<CalendarEventResponse>> GetForUserAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        var user = await calendarRepository.GetUserAsync(userId, cancellationToken)
            ?? throw new NotFoundException("User not found.");
        var events = await calendarRepository.GetEventsForUserAsync(userId, cancellationToken);
        return events.Select(x => MapEvent(x, user.Language)).ToArray();
    }

    public async Task<bool> CancelAsync(
        Guid actorUserId,
        Guid eventId,
        CancellationToken cancellationToken)
    {
        var calendarEvent = await calendarRepository.GetEventForUpdateAsync(eventId, cancellationToken);
        if (calendarEvent is null)
        {
            return false;
        }

        if (calendarEvent.CreatedById != actorUserId)
        {
            throw new ForbiddenException("Only the event creator can cancel it.");
        }

        calendarEvent.Status = EventStatus.Cancelled;
        calendarEvent.UpdatedAtUtc = timeProvider.GetUtcNow();
        await calendarRepository.SaveChangesAsync(cancellationToken);
        await eventPublisher.PublishAsync(
            new CalendarEventMessage(
                "calendar.event.cancelled",
                calendarEvent.Id,
                actorUserId,
                calendarEvent.UpdatedAtUtc),
            cancellationToken);
        return true;
    }

    public async Task<IReadOnlyList<NotificationResponse>> GetNotificationsAsync(
        Guid userId,
        CancellationToken cancellationToken) =>
        (await calendarRepository.GetNotificationsAsync(userId, cancellationToken))
            .Select(x => new NotificationResponse(
                x.Id, x.EventId, x.OccurrenceStartAtUtc, x.Title, x.Description, x.SentAtUtc, x.ReadAtUtc))
            .ToArray();

    public async Task<bool> MarkNotificationReadAsync(
        Guid userId,
        Guid notificationId,
        CancellationToken cancellationToken)
    {
        var notification = await calendarRepository.GetNotificationForUpdateAsync(
            userId,
            notificationId,
            cancellationToken);
        if (notification is null)
        {
            return false;
        }

        notification.ReadAtUtc ??= timeProvider.GetUtcNow();
        await calendarRepository.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static void ValidateSchedule(
        CreateEventRequest request,
        TimeZoneInfo timeZone,
        DateTimeOffset startAt,
        DateTimeOffset? endAt)
    {
        if (endAt is not null && endAt <= startAt)
        {
            throw new BadRequestException("Event end time must be after its start time.");
        }

        var recurrenceDays = request.RecurringWeekdays.Distinct().ToArray();
        if (request.IsRecurring)
        {
            if (recurrenceDays.Length == 0)
            {
                throw new BadRequestException("Recurring events need at least one weekday.");
            }

            if (!recurrenceDays.Contains(TimeZoneInfo.ConvertTime(startAt, timeZone).DayOfWeek))
            {
                throw new BadRequestException("Recurring weekdays must include the first event day.");
            }

            if (request.RecurrenceEndAt is not null && request.RecurrenceEndAt <= startAt)
            {
                throw new BadRequestException("Recurrence end time must be after the first event.");
            }
        }
        else if (recurrenceDays.Length > 0 || request.RecurrenceEndAt is not null)
        {
            throw new BadRequestException("Only recurring events can have recurrence settings.");
        }
    }

    private static IReadOnlyList<EventTranslation> NormalizeTranslations(
        IReadOnlyList<EventTranslationRequest> requests)
    {
        var translations = requests.Select(x => new EventTranslation
        {
            Language = x.Language.Trim().ToLowerInvariant(),
            Title = x.Title.Trim(),
            Description = string.IsNullOrWhiteSpace(x.Description) ? null : x.Description.Trim()
        }).ToArray();
        if (translations.Length == 0 || translations.Any(x =>
                string.IsNullOrWhiteSpace(x.Language) || string.IsNullOrWhiteSpace(x.Title)) ||
            translations.Select(x => x.Language).Distinct(StringComparer.OrdinalIgnoreCase).Count() != translations.Length)
        {
            throw new BadRequestException("Translations need unique languages and non-empty titles.");
        }

        return translations;
    }

    private static CalendarEventResponse MapEvent(Event calendarEvent, string language)
    {
        var translation = SelectTranslation(calendarEvent.Translations, language);
        var days = calendarEvent.RecurrenceDaysCsv.Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(x => Enum.Parse<DayOfWeek>(x, ignoreCase: true))
            .ToArray();
        return new CalendarEventResponse(
            calendarEvent.Id,
            calendarEvent.StartAtUtc,
            calendarEvent.EndAtUtc,
            calendarEvent.TimeZoneId,
            calendarEvent.Status,
            calendarEvent.IsRecurring,
            days,
            calendarEvent.RecurrenceEndAtUtc,
            translation.Title,
            translation.Description,
            calendarEvent.Reminders.Select(x => x.RemindBeforeMinutes).Order().ToArray());
    }

    private static EventTranslation SelectTranslation(
        IEnumerable<EventTranslation> translations,
        string language)
    {
        var all = translations.ToArray();
        var normalized = language.Trim().ToLowerInvariant();
        return all.FirstOrDefault(x => x.Language.Equals(normalized, StringComparison.OrdinalIgnoreCase))
            ?? all.FirstOrDefault(x => x.Language.Equals(normalized.Split('-')[0], StringComparison.OrdinalIgnoreCase))
            ?? all.FirstOrDefault(x => x.Language.Equals("en", StringComparison.OrdinalIgnoreCase))
            ?? all[0];
    }

    private static TimeZoneInfo GetTimeZone(string timeZoneId)
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById(timeZoneId.Trim());
        }
        catch (TimeZoneNotFoundException)
        {
            throw new BadRequestException("Time zone is invalid.");
        }
        catch (InvalidTimeZoneException)
        {
            throw new BadRequestException("Time zone is invalid.");
        }
    }
}
