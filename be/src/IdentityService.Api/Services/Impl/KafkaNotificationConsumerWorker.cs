using System.Text.Json;
using Confluent.Kafka;
using IdentityService.Api.Messaging;

namespace IdentityService.Api.Services;

public sealed class KafkaNotificationConsumerWorker(
    IServiceScopeFactory scopeFactory,
    IConfiguration configuration,
    ILogger<KafkaNotificationConsumerWorker> logger) : BackgroundService
{
    private const int MaxBatchSize = 100;

    protected override Task ExecuteAsync(CancellationToken stoppingToken) =>
        Task.Run(() => ConsumeAsync(stoppingToken), stoppingToken);

    private async Task ConsumeAsync(CancellationToken cancellationToken)
    {
        var consumerConfiguration = new ConsumerConfig
        {
            BootstrapServers = KafkaConfiguration.BootstrapServers(configuration),
            GroupId = KafkaConfiguration.ConsumerGroup(configuration),
            EnableAutoCommit = false,
            EnableAutoOffsetStore = false,
            AutoOffsetReset = AutoOffsetReset.Earliest,
            MaxPollIntervalMs = 300_000
        };

        while (!cancellationToken.IsCancellationRequested)
        {
            using var consumer = new ConsumerBuilder<string, string>(consumerConfiguration).Build();
            consumer.Subscribe(KafkaConfiguration.Topic(configuration));
            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    var consumed = consumer.Consume(cancellationToken);
                    await DeliverBatchAsync(consumer, [consumed], cancellationToken);
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                return;
            }
            catch (KafkaException exception)
            {
                logger.LogWarning(exception, "Kafka reminder consumer is unavailable; retrying.");
            }
            finally
            {
                consumer.Close();
            }

            await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);
        }
    }

    private async Task DeliverBatchAsync(
        IConsumer<string, string> consumer,
        IReadOnlyCollection<ConsumeResult<string, string>> messages,
        CancellationToken cancellationToken)
    {
        foreach (var consumed in messages.Take(MaxBatchSize))
        {
            try
            {
                var message = JsonSerializer.Deserialize<ReminderDueMessage>(consumed.Message.Value)
                    ?? throw new InvalidOperationException("Kafka reminder payload is invalid.");
                using var scope = scopeFactory.CreateScope();
                var deliveryService = scope.ServiceProvider.GetRequiredService<INotificationDeliveryService>();
                await deliveryService.DeliverAsync(message, cancellationToken);
                consumer.Commit(consumed);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Processing Kafka reminder at {TopicPartitionOffset} failed.", consumed.TopicPartitionOffset);
                consumer.Seek(consumed.TopicPartitionOffset);
                await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
                return;
            }
        }
    }
}
