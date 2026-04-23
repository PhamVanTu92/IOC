using DashboardService.Application.DTOs;
using MediatR;

namespace DashboardService.Application.Queries.GetDashboard;

public sealed record GetDashboardQuery(
    Guid Id,
    Guid TenantId) : IRequest<DashboardDto?>;
