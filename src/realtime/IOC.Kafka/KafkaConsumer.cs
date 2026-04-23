using Confluent.Kafka;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace IOC.Kafka;

/// <summary>
/// Background service consumer cho một Kafka topic.
/// Mỗi plugin tạo một instance cho topic của mình.
/// </summary>
public sealed class KafkaConsumerService<TMessage> : BackgroundService
    where TMessage : class
{
    private readonly string _topic;
    private readonly string _groupId;
    private readonly string _bootstrapServers;
    private readonly Func<TMessage, CancellationToken, Task> _handler;
    private readonly ILogger<KafkaConsumerService<TMessage>> _logger;

    public KafkaConsumerService(
        string topic,
        string groupId,
        string bootstrapServers,
        Func<TMessage, CancellationToken, Task> handler,
        ILogger<KafkaConsumerService<TMessage>> logger)
    {
        _topic = topic;
        _groupId = groupId;
        _bootstrapServers = bootstrapServers;
        _handler = handler;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var config = new ConsumerConfig
        {
            BootstrapServers = _bootstrapServers,
            GroupId = _groupId,
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false,    // Manual commit sau khi xử lý thành công
        };

        using var consumer = new ConsumerBuilder<string, string>(config).Build();
        consumer.Subscribe(_topic);

        _logger.LogInformation("Kafka consumer started: Topic={Topic}, Group={Group}", _topic, _groupId);

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                ConsumeResult<string, string>? result = null;
                try
                {
                    result = consumer.Consume(stoppingToken);
                    var message = JsonSerializer.Deserialize<TMessage>(result.Message.Value);
                    if (message is null)
                    {
                        _logger.LogWarning("Null message on topic {Topic}", _topic);
                        consumer.Commit(result);
                        continue;
                    }

                    await _handler(message, stoppingToken);
                    consumer.Commit(result);
                }
                catch (ConsumeException ex)
                {
                    _logger.LogError(ex, "Consume error on topic {Topic}", _topic);
                }
                catch (JsonException ex) when (result is not null)
                {
                    _logger.LogError(ex, "Deserialize error. Offset={Offset}", result.Offset.Value);
                    consumer.Commit(result); // Skip bad message
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    _logger.LogError(ex, "Handler error on topic {Topic}", _topic);
                    // Back-pressure: ngừng nhận message trong 1s
                    await Task.Delay(1000, stoppingToken);
                }
            }
        }
        finally
        {
            consumer.Close();
            _logger.LogInformation("Kafka consumer stopped: Topic={Topic}", _topic);
        }
    }
}
