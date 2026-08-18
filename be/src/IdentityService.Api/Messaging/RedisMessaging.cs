using System.Text.Json;
using StackExchange.Redis;

namespace IdentityService.Api.Messaging;

public sealed record CalendarEventMessage(
    string Name,
    Guid EventId,
    Guid CreatedByUserId,
    DateTimeOffset OccurredAtUtc);

public sealed record DelayedEmailJob(
    Guid JobId,
    Guid EventId,
    Guid ReminderId,
    Guid RecipientUserId,
    string RecipientEmail,
    string Title,
    string? Description,
    DateTimeOffset OccurrenceStartAtUtc,
    int RemindBeforeMinutes);

public sealed record ReceivedEmailJob(string EntryId, DelayedEmailJob Job);

public interface ICalendarEventPublisher
{
    Task PublishAsync(CalendarEventMessage message, CancellationToken cancellationToken);
}

public interface IDelayedEmailJobQueue
{
    Task EnqueueAsync(DelayedEmailJob job, DateTimeOffset availableAtUtc, CancellationToken cancellationToken);
    Task<int> PromoteDueJobsAsync(DateTimeOffset now, CancellationToken cancellationToken);
    Task<ReceivedEmailJob?> DequeueAsync(CancellationToken cancellationToken);
    Task AcknowledgeAsync(string entryId, CancellationToken cancellationToken);
}

public sealed class RedisCalendarEventPublisher(IConnectionMultiplexer redis)
    : ICalendarEventPublisher
{
    public async Task PublishAsync(CalendarEventMessage message, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await redis.GetSubscriber().PublishAsync(
            RedisChannel.Literal("calendar.events"),
            JsonSerializer.Serialize(message));
    }
}

public sealed class RedisDelayedEmailJobQueue(IConnectionMultiplexer redis)
    : IDelayedEmailJobQueue
{
    private const string DelayedJobsKey = "calendar:email:delayed";
    private const string ReadyJobsStream = "calendar:email:ready";
    private const string ConsumerGroup = "calendar-email-workers";
    private const int BatchSize = 100;
    private const string PromoteScript = """
        local jobs = redis.call('ZRANGEBYSCORE', KEYS[1], '-inf', ARGV[1], 'LIMIT', 0, ARGV[2])
        local moved = 0
        for _, payload in ipairs(jobs) do
            if redis.call('ZREM', KEYS[1], payload) == 1 then
                redis.call('XADD', KEYS[2], '*', 'payload', payload)
                moved = moved + 1
            end
        end
        return moved
        """;
    private readonly string consumerName = $"{Environment.MachineName}-{Guid.NewGuid():N}";
    private bool consumerGroupCreated;

    public async Task EnqueueAsync(
        DelayedEmailJob job,
        DateTimeOffset availableAtUtc,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await redis.GetDatabase().SortedSetAddAsync(
            DelayedJobsKey,
            JsonSerializer.Serialize(job),
            availableAtUtc.ToUnixTimeMilliseconds());
    }

    public async Task<int> PromoteDueJobsAsync(DateTimeOffset now, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var moved = await redis.GetDatabase().ScriptEvaluateAsync(
            PromoteScript,
            [DelayedJobsKey, ReadyJobsStream],
            [now.ToUnixTimeMilliseconds(), BatchSize]);
        return (int)(long)moved;
    }

    public async Task<ReceivedEmailJob?> DequeueAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var database = redis.GetDatabase();
        await EnsureConsumerGroupAsync(database);
        var entries = await database.StreamReadGroupAsync(
            ReadyJobsStream,
            ConsumerGroup,
            consumerName,
            ">",
            count: 1);
        if (entries.Length == 0)
        {
            return null;
        }

        var payload = entries[0].Values.Single(x => x.Name == "payload").Value.ToString();
        return new ReceivedEmailJob(
            entries[0].Id.ToString(),
            JsonSerializer.Deserialize<DelayedEmailJob>(payload)
                ?? throw new InvalidOperationException("Redis email job payload is invalid."));
    }

    public async Task AcknowledgeAsync(string entryId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await redis.GetDatabase().StreamAcknowledgeAsync(ReadyJobsStream, ConsumerGroup, entryId);
    }

    private async Task EnsureConsumerGroupAsync(IDatabase database)
    {
        if (consumerGroupCreated)
        {
            return;
        }

        try
        {
            await database.StreamCreateConsumerGroupAsync(
                ReadyJobsStream,
                ConsumerGroup,
                StreamPosition.NewMessages,
                createStream: true);
        }
        catch (RedisServerException exception) when (exception.Message.StartsWith("BUSYGROUP", StringComparison.Ordinal))
        {
        }

        consumerGroupCreated = true;
    }
}
