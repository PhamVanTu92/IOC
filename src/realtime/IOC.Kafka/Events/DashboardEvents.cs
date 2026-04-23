namespace IOC.Kafka.Events;

/// <summary>
/// Fired when a dashboard is created or updated.
/// Topic: ioc.dashboard.saved
/// </summary>
public sealed record DashboardSavedEvent(
    string DashboardId,
    string TenantId,
    string Title,
    int WidgetCount,
    string SavedBy,
    DateTime SavedAt);

/// <summary>
/// Fired when a dashboard is soft-deleted.
/// Topic: ioc.dashboard.deleted
/// </summary>
public sealed record DashboardDeletedEvent(
    string DashboardId,
    string TenantId,
    string DeletedBy,
    DateTime DeletedAt);
