namespace IOC.Kafka;

// ─────────────────────────────────────────────────────────────────────────────
// IKafkaPublisher — abstraction used by domain services to publish events
// ─────────────────────────────────────────────────────────────────────────────

public interface IKafkaPublisher
{
    /// <summary>Publish a typed event as a CloudEvent to the specified topic.</summary>
    Task PublishAsync<TData>(
        string topic,
        string eventType,
        TData data,
        string? partitionKey = null,
        CancellationToken cancellationToken = default);
}
