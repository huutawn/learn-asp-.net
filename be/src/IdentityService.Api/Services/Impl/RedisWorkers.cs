using IdentityService.Api.Data;
using IdentityService.Api.DTOs.Calendar;
using IdentityService.Api.Entities;
using IdentityService.Api.Messaging;
using Microsoft.EntityFrameworkCore;

namespace IdentityService.Api.Services;

public sealed class DelayedEmailSchedulerWorker(
    IDelayedEmailJobQueue queue,
    TimeProvider timeProvider,
    ILogger<DelayedEmailSchedulerWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(1));
        do
        {
            try
            {
                var moved = await queue.PromoteDueJobsAsync(timeProvider.GetUtcNow(), stoppingToken);
                if (moved > 0)
                {
                    logger.LogInformation("Promoted {Count} delayed email/reminder jobs to ready stream.", moved);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Promoting delayed email jobs failed.");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}

public sealed class FakeEmailWorker(
    IDelayedEmailJobQueue queue,
    IServiceScopeFactory scopeFactory,
    INotificationWebSocketService notificationWebSocketService,
    TimeProvider timeProvider,
    ILogger<FakeEmailWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var received = await queue.DequeueAsync(stoppingToken);
                if (received is null)
                {
                    await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
                    continue;
                }

                var job = received.Job;
                var now = timeProvider.GetUtcNow();

                using (var scope = scopeFactory.CreateScope())
                {
                    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                    var calendarEvent = await dbContext.Events
                        .FirstOrDefaultAsync(e => e.Id == job.EventId, stoppingToken);

                    // If event was cancelled or removed, skip sending notification and email
                    if (calendarEvent is null || calendarEvent.Status != EventStatus.Active)
                    {
                        logger.LogInformation(
                            "Event {EventId} is cancelled or not found. Skipping reminder for {RecipientEmail}.",
                            job.EventId,
                            job.RecipientEmail);
                        await queue.AcknowledgeAsync(received.EntryId, stoppingToken);
                        continue;
                    }

                    // Check if already delivered (idempotency check)
                    var alreadyDelivered = await dbContext.Notifications.AnyAsync(
                        n => n.ReminderId == job.ReminderId &&
                             n.RecipientUserId == job.RecipientUserId &&
                             n.OccurrenceStartAtUtc == job.OccurrenceStartAtUtc,
                        stoppingToken);

                    if (!alreadyDelivered)
                    {
                        var notification = new Notification
                        {
                            Id = Guid.NewGuid(),
                            ReminderId = job.ReminderId,
                            EventId = job.EventId,
                            RecipientUserId = job.RecipientUserId,
                            OccurrenceStartAtUtc = job.OccurrenceStartAtUtc,
                            Title = job.Title,
                            Description = job.Description,
                            SentAtUtc = now
                        };
                        dbContext.Notifications.Add(notification);
                        await dbContext.SaveChangesAsync(stoppingToken);

                        var notificationResponse = new NotificationResponse(
                            notification.Id,
                            notification.EventId,
                            notification.OccurrenceStartAtUtc,
                            notification.Title,
                            notification.Description,
                            notification.SentAtUtc,
                            null);

                        await notificationWebSocketService.SendNotificationAsync(
                            job.RecipientUserId,
                            notificationResponse,
                            stoppingToken);
                    }

                    // If recurring, calculate next occurrence and enqueue next reminder job
                    var nextOccurrence = calendarEvent.NextOccurrenceStartAfter(job.OccurrenceStartAtUtc);
                    if (nextOccurrence is not null)
                    {
                        var nextNotifyAt = nextOccurrence.Value.AddMinutes(-job.RemindBeforeMinutes);
                        var nextJob = job with
                        {
                            JobId = Guid.NewGuid(),
                            OccurrenceStartAtUtc = nextOccurrence.Value
                        };
                        await queue.EnqueueAsync(nextJob, nextNotifyAt, stoppingToken);
                    }
                }

                logger.LogInformation(
                    "Fake email sent to {RecipientEmail}: {Title}",
                    job.RecipientEmail,
                    job.Title);

                await queue.AcknowledgeAsync(received.EntryId, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Processing email job failed.");
                await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
            }
        }
    }
}
