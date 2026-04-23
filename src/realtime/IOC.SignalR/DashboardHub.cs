using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace IOC.SignalR;

// ─────────────────────────────────────────────────────────────────────────────
// DashboardHub — SignalR hub for realtime dashboard updates.
//
// Group naming conventions (must match RealtimeBridgeService):
//   domain-{domain}         ← metric updates by business domain
//   dataset-{datasetId}     ← dataset refresh (ChartWidget auto-reload)
//   tenant-{tenantId}       ← dashboard list changes for a tenant
//   dashboard-{dashboardId} ← concurrent editor updates for a specific dashboard
//
// URL: /hubs/dashboard
// ─────────────────────────────────────────────────────────────────────────────

public sealed class DashboardHub : Hub
{
    private readonly ILogger<DashboardHub> _logger;

    public DashboardHub(ILogger<DashboardHub> logger)
    {
        _logger = logger;
    }

    public override async Task OnConnectedAsync()
    {
        _logger.LogDebug("Client connected: {ConnectionId}", Context.ConnectionId);
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        _logger.LogDebug("Client disconnected: {ConnectionId}", Context.ConnectionId);
        await base.OnDisconnectedAsync(exception);
    }

    // ── Domain subscriptions ──────────────────────────────────────────────────

    /// <summary>
    /// Subscribe to a business domain (finance, hr, marketing, …).
    /// Client receives "ReceiveMetricUpdate" when metrics change.
    /// </summary>
    public async Task SubscribeToDomain(string domain)
    {
        var group = $"domain-{domain.ToLower()}";
        await Groups.AddToGroupAsync(Context.ConnectionId, group);
        _logger.LogDebug("Client {Id} → group {Group}", Context.ConnectionId, group);
    }

    public async Task UnsubscribeFromDomain(string domain)
    {
        var group = $"domain-{domain.ToLower()}";
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, group);
    }

    // ── Dataset subscriptions ─────────────────────────────────────────────────

    /// <summary>
    /// Subscribe to a dataset so ChartWidgets auto-refresh when new query
    /// results are available ("DatasetRefreshed" event).
    /// </summary>
    public async Task SubscribeToDataset(string datasetId)
    {
        var group = $"dataset-{datasetId}";
        await Groups.AddToGroupAsync(Context.ConnectionId, group);
        _logger.LogDebug("Client {Id} → group {Group}", Context.ConnectionId, group);
    }

    public async Task UnsubscribeFromDataset(string datasetId)
    {
        var group = $"dataset-{datasetId}";
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, group);
    }

    // ── Tenant subscriptions ──────────────────────────────────────────────────

    /// <summary>
    /// Subscribe to tenant-level events (dashboard list changes).
    /// Client receives "DashboardUpdated" and "DashboardDeleted".
    /// </summary>
    public async Task SubscribeToTenant(string tenantId)
    {
        var group = $"tenant-{tenantId}";
        await Groups.AddToGroupAsync(Context.ConnectionId, group);
        _logger.LogDebug("Client {Id} → group {Group}", Context.ConnectionId, group);
    }

    public async Task UnsubscribeFromTenant(string tenantId)
    {
        var group = $"tenant-{tenantId}";
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, group);
    }

    // ── Dashboard subscriptions ───────────────────────────────────────────────

    /// <summary>
    /// Subscribe to a specific dashboard (concurrent editor awareness).
    /// Client receives "DashboardUpdated" when another user saves.
    /// </summary>
    public async Task SubscribeToDashboard(string dashboardId)
    {
        var group = $"dashboard-{dashboardId}";
        await Groups.AddToGroupAsync(Context.ConnectionId, group);
        _logger.LogDebug("Client {Id} → group {Group}", Context.ConnectionId, group);
    }

    public async Task UnsubscribeFromDashboard(string dashboardId)
    {
        var group = $"dashboard-{dashboardId}";
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, group);
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// Payloads sent to clients
// ─────────────────────────────────────────────────────────────────────────────

public record MetricUpdatePayload(
    string MetricId,
    string Domain,
    double Value,
    string Unit,
    DateTime Timestamp);

// ─────────────────────────────────────────────────────────────────────────────
// DashboardNotifier — convenience service for imperative push from app code
// (use IHubContext<DashboardHub> directly in BackgroundService instead)
// ─────────────────────────────────────────────────────────────────────────────

public sealed class DashboardNotifier(
    IHubContext<DashboardHub> hubContext,
    ILogger<DashboardNotifier> logger)
{
    /// <summary>Push metric update to all clients subscribed to a domain.</summary>
    public async Task NotifyMetricUpdatedAsync(
        MetricUpdatePayload payload,
        CancellationToken cancellationToken = default)
    {
        var group = $"domain-{payload.Domain.ToLower()}";
        await hubContext.Clients.Group(group)
            .SendAsync("ReceiveMetricUpdate", payload, cancellationToken);

        logger.LogDebug("Pushed {Group}: {Metric}={Value}", group, payload.MetricId, payload.Value);
    }

    /// <summary>Notify dataset subscribers that fresh data is available.</summary>
    public async Task NotifyDatasetRefreshedAsync(
        string datasetId,
        DateTime timestamp,
        CancellationToken cancellationToken = default)
    {
        await hubContext.Clients.Group($"dataset-{datasetId}")
            .SendAsync("DatasetRefreshed", new { datasetId, timestamp }, cancellationToken);
    }
}
