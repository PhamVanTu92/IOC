using DashboardService.Application.DTOs;
using DashboardService.Domain;
using MediatR;
using Microsoft.Extensions.Logging;

namespace DashboardService.Application.Commands.SaveDashboard;

public sealed class SaveDashboardCommandHandler(
    IDashboardRepository repository,
    ILogger<SaveDashboardCommandHandler> logger)
    : IRequestHandler<SaveDashboardCommand, DashboardDto>
{
    public async Task<DashboardDto> Handle(SaveDashboardCommand cmd, CancellationToken ct)
    {
        Dashboard dashboard;

        if (cmd.Id is null)
        {
            // ── CREATE ──────────────────────────────────────────────────────
            dashboard = Dashboard.Create(
                tenantId: cmd.TenantId,
                createdBy: cmd.UserId,
                title: cmd.Title,
                configJson: cmd.ConfigJson,
                description: cmd.Description);

            await repository.AddAsync(dashboard, ct);
            logger.LogInformation("Created dashboard {Id} for tenant {TenantId}", dashboard.Id, cmd.TenantId);
        }
        else
        {
            // ── UPDATE ──────────────────────────────────────────────────────
            dashboard = await repository.GetByIdAsync(cmd.Id.Value, cmd.TenantId, ct)
                ?? throw new DashboardNotFoundException(cmd.Id.Value);

            dashboard.Update(cmd.Title, cmd.ConfigJson, cmd.Description);
            await repository.UpdateAsync(dashboard, ct);
            logger.LogInformation("Updated dashboard {Id}", dashboard.Id);
        }

        return ToDto(dashboard);
    }

    private static DashboardDto ToDto(Dashboard d) => new(
        d.Id, d.TenantId, d.CreatedBy,
        d.Title, d.Description, d.ConfigJson,
        d.IsActive, d.CreatedAt, d.UpdatedAt);
}
