namespace DashboardService.Domain;

// ─────────────────────────────────────────────────────────────────────────────
// IDashboardRepository — persistence contract (implemented in Infrastructure)
// ─────────────────────────────────────────────────────────────────────────────

public interface IDashboardRepository
{
    Task<Dashboard?> GetByIdAsync(Guid id, Guid tenantId, CancellationToken ct = default);

    Task<IReadOnlyList<Dashboard>> ListByTenantAsync(
        Guid tenantId,
        bool includeInactive = false,
        CancellationToken ct = default);

    Task AddAsync(Dashboard dashboard, CancellationToken ct = default);

    Task UpdateAsync(Dashboard dashboard, CancellationToken ct = default);

    Task<bool> ExistsAsync(Guid id, Guid tenantId, CancellationToken ct = default);
}
