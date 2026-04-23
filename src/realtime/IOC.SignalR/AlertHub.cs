using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace IOC.SignalR;

/// <summary>
/// AlertHub — SignalR hub cho system alerts và notifications.
/// URL: /hubs/alerts
/// </summary>
public sealed class AlertHub : Hub
{
    private readonly ILogger<AlertHub> _logger;

    public AlertHub(ILogger<AlertHub> logger)
    {
        _logger = logger;
    }

    /// <summary>Subscribe vào alerts của một plugin domain</summary>
    public async Task SubscribeToAlerts(string domain, AlertSeverity minSeverity = AlertSeverity.Info)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"alerts-{domain}");
        _logger.LogDebug("Client {Id} subscribed to alerts: {Domain} (min: {Severity})",
            Context.ConnectionId, domain, minSeverity);
    }
}

public enum AlertSeverity { Info, Warning, Error, Critical }

public record AlertPayload(
    string Id,
    string Domain,
    AlertSeverity Severity,
    string Title,
    string Message,
    DateTime Timestamp,
    string? ActionUrl = null
);

/// <summary>Service để push alerts từ bất kỳ đâu trong hệ thống</summary>
public sealed class AlertNotifier
{
    private readonly IHubContext<AlertHub> _hubContext;

    public AlertNotifier(IHubContext<AlertHub> hubContext) => _hubContext = hubContext;

    public async Task SendAlertAsync(AlertPayload alert, CancellationToken cancellationToken = default)
    {
        await _hubContext.Clients.Group($"alerts-{alert.Domain}")
            .SendAsync("ReceiveAlert", alert, cancellationToken);
    }

    public async Task BroadcastAlertAsync(AlertPayload alert, CancellationToken cancellationToken = default)
    {
        await _hubContext.Clients.All
            .SendAsync("ReceiveAlert", alert, cancellationToken);
    }
}
