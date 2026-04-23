using IOC.Finance.Metrics;
using IOC.Kafka;
using IOC.Kafka.Events;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace IOC.Finance.Services;

// ─────────────────────────────────────────────────────────────────────────────
// FinanceMetricPublisher — BackgroundService that simulates live financial data
//
// In production this would:
//   1. Query a financial data source (DB / ERP / data warehouse)
//   2. Compare with previously published values
//   3. Publish a MetricUpdatedEvent only when values change
//
// For development / demo purposes it generates realistic synthetic data
// with small random fluctuations on each tick so the realtime dashboard
// shows visible updates without requiring a live ERP.
//
// Publish interval: configurable via FinancePublisherOptions (default 15s)
// ─────────────────────────────────────────────────────────────────────────────

public sealed class FinanceMetricPublisher(
    IKafkaPublisher kafka,
    ILogger<FinanceMetricPublisher> logger,
    FinancePublisherOptions options)
    : BackgroundService
{
    // Baseline values — in production these come from the DB
    private double _revenue       = 4_800_000_000;
    private double _cost          = 3_100_000_000;
    private double _budgetUsage   = 64.5;
    private int    _invoiceCount  = 312;
    private double _overdueAmount = 180_000_000;

    private readonly Random _rng = new();

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation(
            "FinanceMetricPublisher starting — interval {Interval}s, tenant {TenantId}",
            options.IntervalSeconds, options.TenantId);

        // Initial publish so the dashboard shows data immediately
        await PublishAllAsync(stoppingToken);

        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(options.IntervalSeconds));

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            await PublishAllAsync(stoppingToken);
        }
    }

    // ── Internal helpers ──────────────────────────────────────────────────────

    private async Task PublishAllAsync(CancellationToken ct)
    {
        // Simulate realistic small fluctuations
        _revenue      = Fluctuate(_revenue,       0.008);
        _cost         = Fluctuate(_cost,          0.005);
        _budgetUsage  = Math.Clamp(Fluctuate(_budgetUsage, 0.02), 0, 100);
        _invoiceCount = (int)Fluctuate(_invoiceCount, 0.03);
        _overdueAmount = Math.Max(0, Fluctuate(_overdueAmount, 0.04));

        var grossProfit = _revenue - _cost;
        var now = DateTime.UtcNow;

        var metrics = new[]
        {
            (FinanceMetrics.Revenue,      _revenue,       FinanceMetrics.Unit),
            (FinanceMetrics.Cost,         _cost,          FinanceMetrics.Unit),
            (FinanceMetrics.GrossProfit,  grossProfit,    FinanceMetrics.Unit),
            (FinanceMetrics.BudgetUsage,  _budgetUsage,   FinanceMetrics.UnitPct),
            (FinanceMetrics.InvoiceCount, (double)_invoiceCount, FinanceMetrics.UnitCount),
            (FinanceMetrics.OverdueAmount, _overdueAmount, FinanceMetrics.Unit),
        };

        var tasks = metrics.Select(async m =>
        {
            var (name, value, unit) = m;
            var evt = new MetricUpdatedEvent(
                DatasetId:  FinanceMetrics.DatasetId,
                Domain:     FinanceMetrics.Domain,
                MetricName: name,
                Value:      Math.Round(value, 2),
                Unit:       unit,
                TenantId:   options.TenantId,
                Timestamp:  now);

            try
            {
                await kafka.PublishAsync(
                    KafkaTopics.MetricUpdated,
                    KafkaTopics.MetricUpdated,
                    evt,
                    partitionKey: $"{options.TenantId}:{name}",
                    cancellationToken: ct);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogWarning(ex, "Failed to publish finance metric {Metric}", name);
            }
        });

        await Task.WhenAll(tasks);

        logger.LogDebug(
            "Finance metrics published — revenue={Revenue:N0} cost={Cost:N0} gross={Gross:N0}",
            _revenue, _cost, grossProfit);
    }

    private double Fluctuate(double value, double maxChangePct)
    {
        // ±maxChangePct random walk, biased slightly positive to simulate growth
        var change = (_rng.NextDouble() * 2 - 0.9) * maxChangePct;
        return value * (1 + change);
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// Options
// ─────────────────────────────────────────────────────────────────────────────

public sealed class FinancePublisherOptions
{
    /// <summary>How often (in seconds) to publish metric updates.</summary>
    public int IntervalSeconds { get; set; } = 15;

    /// <summary>Tenant ID used on published events.</summary>
    public string TenantId { get; set; } = "00000000-0000-0000-0000-000000000001";
}
