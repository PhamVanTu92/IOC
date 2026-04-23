using Microsoft.AspNetCore.SignalR;

namespace Gateway.Hubs;

/// <summary>
/// SignalR Hub cho realtime dashboard updates.
/// Client subscribe vào các domain group để nhận metric updates từ Kafka.
/// </summary>
public sealed class DashboardHub : Hub
{
    private readonly ILogger<DashboardHub> _logger;

    public DashboardHub(ILogger<DashboardHub> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Client subscribe vào domain group (vd: "finance", "hr", "marketing").
    /// Server sẽ push ReceiveMetricUpdate khi có dữ liệu mới.
    /// </summary>
    public async Task SubscribeToDomain(string domain)
    {
        var groupName = $"{domain}-{GetTenantId()}";
        await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
        _logger.LogInformation(
            "Connection {ConnectionId} subscribed to domain group {Group}",
            Context.ConnectionId, groupName);
    }

    /// <summary>
    /// Client unsubscribe khỏi domain group.
    /// </summary>
    public async Task UnsubscribeFromDomain(string domain)
    {
        var groupName = $"{domain}-{GetTenantId()}";
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
    }

    /// <summary>
    /// Subscribe vào một dataset cụ thể để nhận updates khi data thay đổi.
    /// </summary>
    public async Task SubscribeToDataset(Guid datasetId)
    {
        var groupName = $"dataset-{datasetId}-{GetTenantId()}";
        await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
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

    private string GetTenantId()
    {
        // Lấy tenant_id từ JWT claim — fallback sang dev default
        return Context.User?.FindFirst("tid")?.Value
            ?? Context.User?.FindFirst("tenant_id")?.Value
            ?? "00000000-0000-0000-0000-000000000001";
    }
}
