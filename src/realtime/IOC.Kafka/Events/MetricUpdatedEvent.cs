namespace IOC.Kafka.Events;

// ─────────────────────────────────────────────────────────────────────────────
// Domain event contracts — serialized as CloudEvents data payloads
// ─────────────────────────────────────────────────────────────────────────────

/// <summary>
/// Fired when a dataset's metric values are refreshed (e.g. after ETL or forced refresh).
/// Consumers: SignalR broadcaster → frontend chart auto-refresh.
/// Topic: ioc.metrics.updated
/// </summary>
public sealed record MetricUpdatedEvent(
    string DatasetId,
    string Domain,
    string MetricName,
    double Value,
    string Unit,
    string TenantId,
    DateTime Timestamp);

/// <summary>
/// Fired after a semantic query executes.
/// Carries a cache key so subscribed widgets can invalidate their data.
/// Topic: ioc.query.executed
/// </summary>
public sealed record QueryExecutedEvent(
    string DatasetId,
    string CacheKey,
    string TenantId,
    long ExecutionTimeMs,
    int TotalRows,
    DateTime ExecutedAt);
