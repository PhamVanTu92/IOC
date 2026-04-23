using Confluent.Kafka;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace IOC.Kafka;

/// <summary>
/// Generic Kafka producer — publish messages theo CloudEvents format.
/// Topic convention: ioc.{domain}.{event}
/// </summary>
public sealed class KafkaProducer : IAsyncDisposable
{
    private readonly IProducer<string, string> _producer;
    private readonly ILogger<KafkaProducer> _logger;

    public KafkaProducer(string bootstrapServers, ILogger<KafkaProducer> logger)
    {
        _logger = logger;
        var config = new ProducerConfig
        {
            BootstrapServers = bootstrapServers,
            Acks = Acks.All,
            EnableIdempotence = true,
            MessageSendMaxRetries = 3,
        };
        _producer = new ProducerBuilder<string, string>(config).Build();
    }

    /// <summary>Publish một event theo CloudEvents format</summary>
    public async Task PublishAsync<TData>(
        string topic,
        string eventType,
        TData data,
        string? key = null,
        CancellationToken cancellationToken = default)
    {
        var cloudEvent = new
        {
            specversion = "1.0",
            type = eventType,
            source = $"/ioc/{eventType.Split('.')[1]}",
            id = Guid.NewGuid().ToString(),
            time = DateTime.UtcNow.ToString("O"),
            datacontenttype = "application/json",
            data
        };

        var messageValue = JsonSerializer.Serialize(cloudEvent);
        var message = new Message<string, string>
        {
            Key = key ?? Guid.NewGuid().ToString(),
            Value = messageValue,
        };

        try
        {
            var result = await _producer.ProduceAsync(topic, message, cancellationToken);
            _logger.LogDebug("Published to {Topic} [{Partition}@{Offset}]",
                topic, result.Partition.Value, result.Offset.Value);
        }
        catch (ProduceException<string, string> ex)
        {
            _logger.LogError(ex, "Failed to publish to topic {Topic}: {Reason}", topic, ex.Error.Reason);
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
