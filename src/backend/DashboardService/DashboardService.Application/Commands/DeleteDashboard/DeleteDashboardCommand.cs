using MediatR;

namespace DashboardService.Application.Commands.DeleteDashboard;

public sealed record DeleteDashboardCommand(
    Guid Id,
    Guid TenantId) : IRequest<bool>;
