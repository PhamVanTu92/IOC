using DashboardService.Application.Commands.DeleteDashboard;
using DashboardService.Application.Commands.SaveDashboard;
using DashboardService.Application.DTOs;
using Gateway.Infrastructure;
using Gateway.Schema.Inputs;
using HotChocolate;
using HotChocolate.Types;
using MediatR;

namespace Gateway.Schema.Mutations;

// ─────────────────────────────────────────────────────────────────────────────
// DashboardMutation — create / update / delete dashboards
// ─────────────────────────────────────────────────────────────────────────────

[ExtendObjectType(OperationTypeNames.Mutation)]
public sealed class DashboardMutation
{
    /// <summary>Create a new dashboard. Returns the persisted dashboard with generated Id.</summary>
    public async Task<DashboardDto> CreateDashboardAsync(
        SaveDashboardInput input,
        [Service] IMediator mediator,
        [Service] TenantContext tenantContext,
        CancellationToken cancellationToken)
    {
        return await mediator.Send(
            new SaveDashboardCommand(
                Id: null,
                TenantId: tenantContext.TenantId,
                UserId: tenantContext.UserId,
                Title: input.Title,
                ConfigJson: input.ConfigJson,
                Description: input.Description),
            cancellationToken);
    }

    /// <summary>Update an existing dashboard's title, config, or description.</summary>
    public async Task<DashboardDto> UpdateDashboardAsync(
        Guid id,
        SaveDashboardInput input,
        [Service] IMediator mediator,
        [Service] TenantContext tenantContext,
        CancellationToken cancellationToken)
    {
        return await mediator.Send(
            new SaveDashboardCommand(
                Id: id,
                TenantId: tenantContext.TenantId,
                UserId: tenantContext.UserId,
                Title: input.Title,
                ConfigJson: input.ConfigJson,
                Description: input.Description),
            cancellationToken);
    }

    /// <summary>Soft-delete a dashboard (sets is_active = false).</summary>
    public async Task<bool> DeleteDashboardAsync(
        Guid id,
        [Service] IMediator mediator,
        [Service] TenantContext tenantContext,
        CancellationToken cancellationToken)
    {
        return await mediator.Send(
            new DeleteDashboardCommand(id, tenantContext.TenantId),
            cancellationToken);
    }
}
