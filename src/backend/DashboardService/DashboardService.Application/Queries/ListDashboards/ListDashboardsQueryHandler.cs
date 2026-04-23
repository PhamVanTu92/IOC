using System.Text.Json;
using DashboardService.Application.DTOs;
using DashboardService.Domain;
using MediatR;

namespace DashboardService.Application.Queries.ListDashboards;

public sealed class ListDashboardsQueryHandler(IDashboardRepository repository)
    : IRequestHandler<ListDashboardsQuery, IReadOnlyList<DashboardSummaryDto>>
{
    public async Task<IReadOnlyList<DashboardSummaryDto>> Handle(
        ListDashboardsQuery query, CancellationToken ct)
    {
        var dashboards = await repository.ListByTenantAsync(
            query.TenantId, query.IncludeInactive, ct);

        return dashboards.Select(d => new DashboardSummaryDto(
            Id: d.Id,
            Title: d.Title,
            Description: d.Description,
            IsActive: d.IsActive,
            UpdatedAt: d.UpdatedAt,
            WidgetCount: CountWidgets(d.ConfigJson)
        )).ToList();
    }

    /// <summary>
    /// Extracts widget count from the config JSON without deserializing the full config.
    /// Falls back to 0 on parse error.
    /// </summary>
    private static int CountWidgets(string configJson)
    {
        try
        {
            using var doc = JsonDocument.Parse(configJson);
            if (doc.RootElement.TryGetProperty("widgets", out var widgets) &&
                widgets.ValueKind == JsonValueKind.Array)
            {
                return widgets.GetArrayLength();
            }
        }
        catch (JsonException) { /* ignore */ }
        return 0;
    }
}
