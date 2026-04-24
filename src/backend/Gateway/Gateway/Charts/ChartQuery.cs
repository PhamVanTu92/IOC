using Gateway.Infrastructure;
using HotChocolate;
using HotChocolate.Authorization;
using HotChocolate.Types;

namespace Gateway.Charts;

public sealed record ChartGql(
    Guid    Id,
    Guid    ModuleId,
    string  Name,
    string? Description,
    string  ChartType,
    string  ConfigJson,
    int     SortOrder,
    DateTime CreatedAt);

[ExtendObjectType("Query")]
public sealed class ChartQuery
{
    [Authorize]
    public async Task<List<ChartGql>> ChartsByModuleAsync(
        Guid moduleId,
        [Service] ChartRepository repo,
        [Service] TenantContext tenant,
        CancellationToken cancellationToken)
    {
        var rows = await repo.GetByModuleAsync(moduleId, tenant.TenantId, cancellationToken);
        return rows.Select(ToGql).ToList();
    }

    [Authorize]
    public async Task<ChartGql?> ChartByIdAsync(
        Guid id,
        [Service] ChartRepository repo,
        [Service] TenantContext tenant,
        CancellationToken cancellationToken)
    {
        var row = await repo.GetByIdAsync(id, tenant.TenantId, cancellationToken);
        return row is null ? null : ToGql(row);
    }

    private static ChartGql ToGql(ChartRow r) =>
        new(r.id, r.module_id, r.name, r.description, r.chart_type, r.config_json, r.sort_order, r.created_at);
}
