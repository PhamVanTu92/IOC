using Gateway.Infrastructure;
using HotChocolate;
using HotChocolate.Authorization;
using HotChocolate.Types;

namespace Gateway.Charts;

public sealed record CreateChartInput(
    Guid    ModuleId,
    string  Name,
    string? Description,
    string  ChartType,
    string  ConfigJson,
    int     SortOrder);

public sealed record UpdateChartInput(
    Guid    Id,
    string  Name,
    string? Description,
    string  ChartType,
    string  ConfigJson,
    int     SortOrder);

[ExtendObjectType("Mutation")]
public sealed class ChartMutation
{
    [Authorize(Roles = new[] { "admin" })]
    public async Task<ChartGql> CreateChartAsync(
        CreateChartInput input,
        [Service] ChartRepository repo,
        [Service] TenantContext tenant,
        [Service] IHttpContextAccessor http,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId(http);
        var id = await repo.CreateAsync(
            input.ModuleId, tenant.TenantId, userId,
            input.Name, input.Description, input.ChartType, input.ConfigJson,
            input.SortOrder, cancellationToken);
        var row = await repo.GetByIdAsync(id, tenant.TenantId, cancellationToken)!;
        return ToGql(row!);
    }

    [Authorize(Roles = new[] { "admin", "editor" })]
    public async Task<ChartGql> UpdateChartAsync(
        UpdateChartInput input,
        [Service] ChartRepository repo,
        [Service] TenantContext tenant,
        CancellationToken cancellationToken)
    {
        var ok = await repo.UpdateAsync(
            input.Id, tenant.TenantId,
            input.Name, input.Description, input.ChartType, input.ConfigJson,
            input.SortOrder, cancellationToken);
        if (!ok) throw new HotChocolate.GraphQLException(
            HotChocolate.ErrorBuilder.New().SetMessage("Chart not found").SetCode("NOT_FOUND").Build());
        var row = await repo.GetByIdAsync(input.Id, tenant.TenantId, cancellationToken);
        return ToGql(row!);
    }

    [Authorize(Roles = new[] { "admin" })]
    public async Task<bool> DeleteChartAsync(
        Guid id,
        [Service] ChartRepository repo,
        [Service] TenantContext tenant,
        CancellationToken cancellationToken)
        => await repo.DeleteAsync(id, tenant.TenantId, cancellationToken);

    private static Guid GetUserId(IHttpContextAccessor http)
    {
        var raw = http.HttpContext?.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
               ?? http.HttpContext?.User.FindFirst("sub")?.Value ?? "";
        return Guid.TryParse(raw, out var g) ? g : Guid.Empty;
    }

    private static ChartGql ToGql(ChartRow r) =>
        new(r.id, r.module_id, r.name, r.description, r.chart_type, r.config_json, r.sort_order, r.created_at);
}
