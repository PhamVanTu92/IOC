namespace DashboardService.Application.DTOs;

// ─────────────────────────────────────────────────────────────────────────────
// DashboardDto — read model returned by queries and mutations
// ─────────────────────────────────────────────────────────────────────────────

public sealed record DashboardDto(
    Guid Id,
    Guid TenantId,
    Guid CreatedBy,
    string Title,
    string? Description,
    /// <summary>Full JSON string of the frontend DashboardConfig.</summary>
    string ConfigJson,
    bool IsActive,
    DateTime CreatedAt,
    DateTime UpdatedAt);

/// <summary>Lightweight summary for list views (no config payload).</summary>
public sealed record DashboardSummaryDto(
    Guid Id,
    string Title,
    string? Description,
    bool IsActive,
    DateTime UpdatedAt,
    int WidgetCount);
