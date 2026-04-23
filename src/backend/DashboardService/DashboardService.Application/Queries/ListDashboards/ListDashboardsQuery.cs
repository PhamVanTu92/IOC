using DashboardService.Application.DTOs;
using MediatR;

namespace DashboardService.Application.Queries.ListDashboards;

public sealed record ListDashboardsQuery(
    Guid TenantId,
    bool IncludeInactive = false) : IRequest<IReadOnlyList<DashboardSummaryDto>>;
