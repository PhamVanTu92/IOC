using DashboardService.Application.DTOs;
using DashboardService.Domain;
using MediatR;

namespace DashboardService.Application.Queries.GetDashboard;

public sealed class GetDashboardQueryHandler(IDashboardRepository repository)
    : IRequestHandler<GetDashboardQuery, DashboardDto?>
{
    public async Task<DashboardDto?> Handle(GetDashboardQuery query, CancellationToken ct)
    {
        var d = await repository.GetByIdAsync(query.Id, query.TenantId, ct);
        if (d is null) return null;

        return new DashboardDto(
            d.Id, d.TenantId, d.CreatedBy,
            d.Title, d.Description, d.ConfigJson,
            d.IsActive, d.CreatedAt, d.UpdatedAt);
    }
}
