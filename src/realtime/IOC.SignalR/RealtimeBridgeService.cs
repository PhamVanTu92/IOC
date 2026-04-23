using System.Text.Json;
using Confluent.Kafka;
using IOC.Kafka;
using IOC.Kafka.Events;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace IOC.SignalR;

// ─────────────────────────────────────────────────────────────────────────────
// RealtimeBridgeService — Kafka → SignalR bridge
//
// Single BackgroundService that consumes from multiple IOC topics and
// pushes updates to connected SignalR clients via DashboardHub.
// Runs as a hosted service in the Gateway process.
// ─────────────────────────────────────────────────────────────────────────────

public sealed class RealtimeBridgeService(
    IHubContext<DashboardHub> hubContext,
    ILogger<RealtimeBridgeService> logger,
    RealtimeBridgeOptions options)
    : BackgroundService
{
    // ── Topics consumed ───────────────────────────────────────────────────────

    private static readonly string[] _topics =
    [
        KafkaTopics.MetricUpdated,
        KafkaTopics.QueryExecuted,
        KafkaTopics.DashboardSaved,
        KafkaTopics.DashboardDeleted,
    ];

    private static readonly JsonSerializerOptions _jsonOpts = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    // Retry delays: 5s, 10s, 30s, 60s, 60s, ...
    private static readonly TimeSpan[] _retryDelays =
        [TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(10),
         TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(60)];

    // ── BackgroundService entry point ─────────────────────────────────────────

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation(
            "RealtimeBridgeService starting — brokers: {Brokers}", options.BootstrapServers);

        // Retry outer loop — nếu Kafka chưa sẵn sàng thì chờ và thử lại
        // KHÔNG crash host, chỉ log warning
        var attempt = 0;
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RunConsumerLoopAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                // App đang shutdown — thoát bình thường
                break;
            }
            catch (Exception ex)
            {
                var delay = _retryDelays[Math.Min(attempt++, _retryDelays.Length - 1)];
                logger.LogWarning(ex,
                    "Kafka consumer disconnected. Retry #{Attempt} in {Delay}s...",
                    attempt, delay.TotalSeconds);

                try { await Task.Delay(delay, stoppingToken); }
                catch (OperationCanceledException) { break; }
            }
        }

        logger.LogInformation("RealtimeBridgeService stopped");
    }

    // ── Consumer loop (tách riêng để retry dễ) ───────────────────────────────

    private async Task RunConsumerLoopAsync(CancellationToken stoppingToken)
    {
        var config = new ConsumerConfig
        {
            BootstrapServers = options.BootstrapServers,
            GroupId = options.ConsumerGroupId,
            AutoOffsetReset = AutoOffsetReset.Latest,
            EnableAutoCommit = false,
            SessionTimeoutMs = 30_000,
            HeartbeatIntervalMs = 3_000,
            // Giảm timeout connect để retry nhanh hơn
            SocketTimeoutMs = 10_000,
            MetadataMaxAgeMs = 10_000,
        };

        using var consumer = new ConsumerBuilder<string, string>(config)
            .SetErrorHandler((_, e) =>
            {
                // Fatal error → throw để trigger retry loop bên ngoài
                if (e.IsFatal)
                    logger.LogError("Kafka FATAL error: [{Code}] {Reason}", e.Code, e.Reason);
                else
                    logger.LogWarning("Kafka warning: [{Code}] {Reason}", e.Code, e.Reason);
            })
            .Build();

        consumer.Subscribe(_topics);
        logger.LogInformation("Subscribed to Kafka topics: {Topics}", string.Join(", ", _topics));

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                ConsumeResult<string, string>? result = null;
                try
                {
                    result = consumer.Consume(stoppingToken);
                    await DispatchAsync(result.Topic, result.Message.Value, stoppingToken);
                    consumer.Commit(result);
                }
                catch (ConsumeException ex)
                {
                    logger.LogError(ex, "Kafka consume error on topic");
                }
                catch (JsonException ex) when (result is not null)
                {
                    logger.LogWarning(ex, "Bad JSON on topic {Topic} — skipping", result.Topic);
                    consumer.Commit(result); // skip poison pill
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    logger.LogError(ex, "Dispatch error");
                    await Task.Delay(1_000, stoppingToken);
                }
            }
        }
        finally
        {
            consumer.Close();
        }
    }

    // ── Dispatch by topic ─────────────────────────────────────────────────────

    private async Task DispatchAsync(string topic, string json, CancellationToken ct)
    {
        switch (topic)
        {
            case KafkaTopics.MetricUpdated:
                await HandleMetricUpdatedAsync(json, ct);
                break;

            case KafkaTopics.QueryExecuted:
                await HandleQueryExecutedAsync(json, ct);
                break;

            case KafkaTopics.DashboardSaved:
                await HandleDashboardSavedAsync(json, ct);
                break;

            case KafkaTopics.DashboardDeleted:
                await HandleDashboardDeletedAsync(json, ct);
                break;
        }
    }

    // ── Handlers ──────────────────────────────────────────────────────────────

    private async Task HandleMetricUpdatedAsync(string json, CancellationToken ct)
    {
        var envelope = JsonSerializer.Deserialize<CloudEvent<MetricUpdatedEvent>>(json, _jsonOpts);
        if (envelope?.Data is null) return;

        var evt = envelope.Data;
        var groupName = $"domain-{evt.Domain.ToLower()}";

        // Push to domain subscribers
        await hubContext.Clients.Group(groupName)
            .SendAsync("ReceiveMetricUpdate", new
            {
                datasetId = evt.DatasetId,
                domain = evt.Domain,
                metricName = evt.MetricName,
                value = evt.Value,
                unit = evt.Unit,
                tenantId = evt.TenantId,
                timestamp = evt.Timestamp,
            }, ct);

        // Also push to dataset subscribers so widgets auto-refresh
        await hubContext.Clients.Group($"dataset-{evt.DatasetId}")
            .SendAsync("DatasetRefreshed", new
            {
                datasetId = evt.DatasetId,
                timestamp = evt.Timestamp,
            }, ct);

        logger.LogDebug("Broadcast MetricUpdated: {Dataset}/{Metric}={Value}",
            evt.DatasetId, evt.MetricName, evt.Value);
    }

    private async Task HandleQueryExecutedAsync(string json, CancellationToken ct)
    {
        var envelope = JsonSerializer.Deserialize<CloudEvent<QueryExecutedEvent>>(json, _jsonOpts);
        if (envelope?.Data is null) return;

        var evt = envelope.Data;

        // Notify dataset subscribers that fresh data is available
        await hubContext.Clients.Group($"dataset-{evt.DatasetId}")
            .SendAsync("DatasetRefreshed", new
            {
                datasetId = evt.DatasetId,
                cacheKey = evt.CacheKey,
                totalRows = evt.TotalRows,
                timestamp = evt.ExecutedAt,
            }, ct);
    }

    private async Task HandleDashboardSavedAsync(string json, CancellationToken ct)
    {
        var envelope = JsonSerializer.Deserialize<CloudEvent<DashboardSavedEvent>>(json, _jsonOpts);
        if (envelope?.Data is null) return;

        var evt = envelope.Data;

        // Push to tenant subscribers (for list page refresh)
        await hubContext.Clients.Group($"tenant-{evt.TenantId}")
            .SendAsync("DashboardUpdated", new
            {
                dashboardId = evt.DashboardId,
                title = evt.Title,
                widgetCount = evt.WidgetCount,
                savedBy = evt.SavedBy,
                savedAt = evt.SavedAt,
            }, ct);

        // Push to specific dashboard subscribers (for concurrent editors)
        await hubContext.Clients.Group($"dashboard-{evt.DashboardId}")
            .SendAsync("DashboardUpdated", new
            {
                dashboardId = evt.DashboardId,
                title = evt.Title,
                widgetCount = evt.WidgetCount,
                savedBy = evt.SavedBy,
                savedAt = evt.SavedAt,
            }, ct);

        logger.LogDebug("Broadcast DashboardSaved: {Id}", evt.DashboardId);
    }

    private async Task HandleDashboardDeletedAsync(string json, CancellationToken ct)
    {
        var envelope = JsonSerializer.Deserialize<CloudEvent<DashboardDeletedEvent>>(json, _jsonOpts);
        if (envelope?.Data is null) return;

        var evt = envelope.Data;

        await hubContext.Clients.Group($"tenant-{evt.TenantId}")
            .SendAsync("DashboardDeleted", new
            {
                dashboardId = evt.DashboardId,
                deletedAt = evt.DeletedAt,
            }, ct);

        logger.LogDebug("Broadcast DashboardDeleted: {Id}", evt.DashboardId);
    }
}

// ── Options ───────────────────────────────────────────────────────────────────

public sealed class RealtimeBridgeOptions
{
    public string BootstrapServers { get; set; } = "localhost:9092";
    public string ConsumerGroupId { get; set; } = "ioc-gateway-realtime";
}
