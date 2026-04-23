using DashboardService.Domain;
using MediatR;
using Microsoft.Extensions.Logging;

namespace DashboardService.Application.Commands.DeleteDashboard;

public sealed class DeleteDashboardCommandHandler(
    IDashboardRepository repository,
    ILogger<DeleteDashboardCommandHandler> logger)
    : IRequestHandler<DeleteDashboardCommand, bool>
{
    public async Task<bool> Handle(DeleteDashboardCommand cmd, CancellationToken ct)
    {
        var dashboard = await repository.GetByIdAsync(cmd.Id, cmd.TenantId, ct);
        if (dashboard is null)
        {
            logger.LogWarning("Delete attempted on non-existent dashboard {Id}", cmd.Id);
            return false;
        }

        dashboard.Deactivate();
        await repository.UpdateAsync(dashboard, ct);
        logger.LogInformation("Deleted (deactivated) dashboard {Id}", cmd.Id);
        return true;
    }
}
