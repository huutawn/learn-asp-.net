using Confluent.Kafka;
using IdentityService.Api.Data;
using IdentityService.Api.Entities;
using IdentityService.Api.Messaging;
using Microsoft.EntityFrameworkCore;

namespace IdentityService.Api.Services;

public sealed class OutboxPublisherWorker(
    IServiceScopeFactory scopeFactory,
    IProducer<string, string> producer,
    TimeProvider timeProvider,
    ILogger<OutboxPublisherWorker> logger) : BackgroundService
{
    private const int BatchSize = 100;
    private static readonly TimeSpan PublishingLease = TimeSpan.FromMinutes(1);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(1));
        while (!stoppingToken.IsCancellationRequested)
        {
            await PublishDueMessagesAsync(stoppingToken);
            await timer.WaitForNextTickAsync(stoppingToken);
        }
    }

    private async Task PublishDueMessagesAsync(CancellationToken cancellationToken)
    {
        IReadOnlyList<OutboxMessage> messages;
        try
        {
            messages = await ClaimMessagesAsync(cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Claiming outbox messages failed.");
            return;
        }

        foreach (var message in messages)
        {
            try
            {
                await producer.ProduceAsync(
                    message.Topic,
                    new Message<string, string>
                    {
                        Key = message.ReminderId.ToString("D"),
                        Value = message.Payload
                    },
                    cancellationToken);
                await MarkPublishedAsync(message.Id, cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception exception)
            {
                logger.LogWarning(exception, "Publishing outbox message {OutboxMessageId} failed.", message.Id);
                await ScheduleRetryAsync(message, exception, cancellationToken);
            }
        }
    }

    private async Task<IReadOnlyList<OutboxMessage>> ClaimMessagesAsync(CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var now = timeProvider.GetUtcNow();
        var leaseExpiresAt = now.Add(PublishingLease);
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        var messages = await dbContext.OutboxMessages
            .FromSqlInterpolated($"""
                SELECT o.* FROM outbox_messages AS o
                WHERE (o.status = 'Pending' AND o.next_attempt_at_utc <= {now})
                    OR (o.status = 'Publishing' AND o.publishing_lease_expires_at_utc <= {now})
                ORDER BY o.next_attempt_at_utc
                LIMIT {BatchSize}
                FOR UPDATE SKIP LOCKED
                """)
            .ToArrayAsync(cancellationToken);
        foreach (var message in messages)
        {
            message.Status = OutboxMessageStatus.Publishing;
            message.PublishingLeaseExpiresAtUtc = leaseExpiresAt;
            message.AttemptCount++;
            message.UpdatedAtUtc = now;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return messages.Select(x => new OutboxMessage
        {
            Id = x.Id,
            ReminderId = x.ReminderId,
            Topic = x.Topic,
            Payload = x.Payload,
            AttemptCount = x.AttemptCount
        }).ToArray();
    }

    private async Task MarkPublishedAsync(Guid id, CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var now = timeProvider.GetUtcNow();
        await dbContext.OutboxMessages
            .Where(x => x.Id == id && x.Status == OutboxMessageStatus.Publishing)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(x => x.Status, OutboxMessageStatus.Published)
                .SetProperty(x => x.PublishedAtUtc, now)
                .SetProperty(x => x.PublishingLeaseExpiresAtUtc, (DateTimeOffset?)null)
                .SetProperty(x => x.UpdatedAtUtc, now), cancellationToken);
    }

    private async Task ScheduleRetryAsync(OutboxMessage message, Exception exception, CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var now = timeProvider.GetUtcNow();
        var seconds = Math.Min(300, Math.Pow(2, Math.Min(message.AttemptCount, 8)));
        await dbContext.OutboxMessages
            .Where(x => x.Id == message.Id && x.Status == OutboxMessageStatus.Publishing)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(x => x.Status, OutboxMessageStatus.Pending)
                .SetProperty(x => x.NextAttemptAtUtc, now.AddSeconds(seconds))
                .SetProperty(x => x.PublishingLeaseExpiresAtUtc, (DateTimeOffset?)null)
                .SetProperty(x => x.LastError, exception.Message[..Math.Min(exception.Message.Length, 2_000)])
                .SetProperty(x => x.UpdatedAtUtc, now), cancellationToken);
    }
}
