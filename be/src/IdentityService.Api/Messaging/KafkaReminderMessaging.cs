using Confluent.Kafka;

namespace IdentityService.Api.Messaging;

public sealed record ReminderDueMessage(
    Guid MessageId,
    Guid EventId,
    Guid ReminderId,
    DateTimeOffset OccurrenceStartAtUtc);

public static class KafkaConfiguration
{
    public static string BootstrapServers(IConfiguration configuration) =>
        configuration["Kafka:BootstrapServers"]
        ?? throw new InvalidOperationException("Kafka:BootstrapServers is required.");

    public static string Topic(IConfiguration configuration) =>
        configuration["Kafka:ReminderTopic"]
        ?? throw new InvalidOperationException("Kafka:ReminderTopic is required.");

    public static string ConsumerGroup(IConfiguration configuration) =>
        configuration["Kafka:ReminderConsumerGroup"]
        ?? throw new InvalidOperationException("Kafka:ReminderConsumerGroup is required.");
}
