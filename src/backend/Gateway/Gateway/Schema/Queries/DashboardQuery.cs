using DashboardService.Application.DTOs;
using DashboardService.Application.Queries.GetDashboard;
using DashboardService.Application.Queries.ListDashboards;
using DashboardService.Domain;
using Gateway.Infrastructure;
using HotChocolate;
using HotChocolate.Types;
using MediatR;

namespace Gateway.Schema.Queries;

// ─────────────────────────────────────────────────────────────────────────────
// DashboardQuery — read operations for saved dashboards
// ─────────────────────────────────────────────────────────────────────────────

[ExtendObjectType(OperationTypeNames.Query)]
public sealed class DashboardQuery
{
    /// <summary>Load a single dashboard by id.</summary>
    public async Task<DashboardDto?> DashboardAsync(
        Guid id,
        [Service] IMediator mediator,
        [Service] TenantContext tenantContext,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new GetDashboardQuery(id, tenantContext.TenantId),
            cancellationToken);

        return result ?? throw new DashboardNotFoundException(id);
    }

    /// <summary>List all dashboards for the current tenant.</summary>
    public async Task<IReadOnlyList<DashboardSummaryDto>> DashboardsAsync(
        [Service] IMediator mediator,
        [Service] TenantContext tenantContext,
        CancellationToken cancellationToken,
        bool includeInactive = false)
    {
        return await mediator.Send(
            new ListDashboardsQuery(tenantContext.TenantId, includeInactive),
            cancellationToken);
    }
}
