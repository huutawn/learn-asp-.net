using System.Text.Json;
using IdentityService.Api.Data;
using IdentityService.Api.Entities;
using IdentityService.Api.Messaging;
using Microsoft.EntityFrameworkCore;

namespace IdentityService.Api.Services;

public sealed class ReminderSchedulerWorker(
    IServiceScopeFactory scopeFactory,
    IConfiguration configuration,
    TimeProvider timeProvider,
    ILogger<ReminderSchedulerWorker> logger) : BackgroundService
{
    private const int BatchSize = 1_000;
    private readonly int maxBatchesPerRun = Math.Max(
        1,
        configuration.GetValue<int?>("ReminderScheduler:MaxBatchesPerRun") ?? 10);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(5));
        while (!stoppingToken.IsCancellationRequested)
        {
            await ScheduleDueRemindersAsync(stoppingToken);
            await timer.WaitForNextTickAsync(stoppingToken);
        }
    }

    private async Task ScheduleDueRemindersAsync(CancellationToken cancellationToken)
    {
        try
        {
            var totalScheduled = 0;
            for (var batchNumber = 0; batchNumber < maxBatchesPerRun; batchNumber++)
            {
                var scheduled = await ScheduleBatchAsync(cancellationToken);
                totalScheduled += scheduled;
                if (scheduled < BatchSize)
                {
                    break;
                }
            }

            if (totalScheduled > 0)
            {
                logger.LogInformation("Scheduled {Count} due reminders into the outbox.", totalScheduled);
            }

            if (totalScheduled == BatchSize * maxBatchesPerRun)
            {
                logger.LogWarning(
                    "Scheduler reached its per-run capacity of {Capacity} reminders. " +
                    "Increase ReminderScheduler:MaxBatchesPerRun or scale scheduler instances.",
                    totalScheduled);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Scheduling due reminders failed.");
        }
    }

    private async Task<int> ScheduleBatchAsync(CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var now = timeProvider.GetUtcNow();
        var topic = KafkaConfiguration.Topic(configuration);
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        var reminders = await dbContext.Reminders
            .FromSqlInterpolated($"""
                SELECT r.* FROM reminders AS r
                WHERE r.status = 'Active' AND r.next_reminder_at_utc <= {now}
                ORDER BY r.next_reminder_at_utc
                LIMIT {BatchSize}
                FOR UPDATE SKIP LOCKED
                """)
            .Include(x => x.Event)
                .ThenInclude(x => x.Translations)
            .ToArrayAsync(cancellationToken);

        foreach (var reminder in reminders)
        {
            ScheduleReminder(dbContext, reminder, topic, now);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return reminders.Length;
    }

    private static void ScheduleReminder(
        ApplicationDbContext dbContext,
        Reminder reminder,
        string topic,
        DateTimeOffset now)
    {
        var calendarEvent = reminder.Event;
        reminder.UpdatedAtUtc = now;

        if (calendarEvent.Status != EventStatus.Active)
        {
            reminder.Status = ReminderStatus.Cancelled;
            return;
        }

        if (reminder.NextOccurrenceStartAtUtc <= now)
        {
            AdvanceToNextOccurrence(reminder, now);
            return;
        }

        var message = new ReminderDueMessage(
            Guid.NewGuid(),
            calendarEvent.Id,
            reminder.Id,
            reminder.NextOccurrenceStartAtUtc);
        dbContext.OutboxMessages.Add(new OutboxMessage
        {
            Id = message.MessageId,
            ReminderId = reminder.Id,
            OccurrenceStartAtUtc = reminder.NextOccurrenceStartAtUtc,
            Topic = topic,
            Payload = JsonSerializer.Serialize(message),
            NextAttemptAtUtc = now,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        });

        var repeatedReminderAt = reminder.RepeatEveryMinutes is { } interval
            ? reminder.NextReminderAtUtc.AddMinutes(interval)
            : (DateTimeOffset?)null;
        if (repeatedReminderAt is not null && repeatedReminderAt < reminder.NextOccurrenceStartAtUtc)
        {
            reminder.NextReminderAtUtc = repeatedReminderAt.Value;
            return;
        }

        AdvanceToNextOccurrence(reminder, now);
    }

    private static void AdvanceToNextOccurrence(Reminder reminder, DateTimeOffset now)
    {
        var nextOccurrence = reminder.Event.NextOccurrenceStartAfter(reminder.NextOccurrenceStartAtUtc);
        while (nextOccurrence is not null && nextOccurrence <= now)
        {
            nextOccurrence = reminder.Event.NextOccurrenceStartAfter(nextOccurrence.Value);
        }

        if (nextOccurrence is null)
        {
            reminder.Status = ReminderStatus.Completed;
            return;
        }

        reminder.NextOccurrenceStartAtUtc = nextOccurrence.Value;
        reminder.NextReminderAtUtc = nextOccurrence.Value.AddMinutes(-reminder.RemindBeforeMinutes);
    }
}
