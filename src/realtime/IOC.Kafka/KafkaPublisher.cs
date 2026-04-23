using System.Text.Json;
using Confluent.Kafka;
using IOC.Kafka.Events;
using Microsoft.Extensions.Logging;

namespace IOC.Kafka;

// ─────────────────────────────────────────────────────────────────────────────
// KafkaPublisher — singleton IKafkaPublisher backed by Confluent.Kafka producer
// Wraps messages in CloudEvents 1.0 envelope.
// ─────────────────────────────────────────────────────────────────────────────

public sealed class KafkaPublisher : IKafkaPublisher, IAsyncDisposable
{
    private readonly IProducer<string, string> _producer;
    private readonly ILogger<KafkaPublisher> _logger;

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public KafkaPublisher(string bootstrapServers, ILogger<KafkaPublisher> logger)
    {
        _logger = logger;
        var config = new ProducerConfig
        {
            BootstrapServers = bootstrapServers,
            Acks = Acks.All,
            EnableIdempotence = true,
            MessageSendMaxRetries = 3,
            RetryBackoffMs = 200,
        };
        _producer = new ProducerBuilder<string, string>(config).Build();
    }

    public async Task PublishAsync<TData>(
        string topic,
        string eventType,
        TData data,
        string? partitionKey = null,
        CancellationToken cancellationToken = default)
    {
        // Extract domain from event type (e.g. "ioc.dashboard.saved" → "dashboard")
        var parts = eventType.Split('.');
        var source = parts.Length >= 2 ? $"/ioc/{parts[1]}" : "/ioc/system";

        var envelope = CloudEvent<TData>.Create(eventType, source, data);
        var payload = JsonSerializer.Serialize(envelope, _jsonOptions);

        var message = new Message<string, string>
        {
            Key = partitionKey ?? Guid.NewGuid().ToString(),
            Value = payload,
        };

        try
        {
            var result = await _producer.ProduceAsync(topic, message, cancellationToken);
            _logger.LogDebug(
                "Published {EventType} to {Topic} [{Partition}@{Offset}]",
                eventType, topic, result.Partition.Value, result.Offset.Value);
        }
        catch (ProduceException<string, string> ex)
        {
            _logger.LogError(ex,
                "Failed to publish {EventType} to {Topic}: {Reason}",
                eventType, topic, ex.Error.Reason);
            throw;
        }
    }

    public async ValueTask DisposeAsync()
    {
        _producer.Flush(TimeSpan.FromSeconds(10));
        _producer.Dispose();
        await Task.CompletedTask;
    }
}
