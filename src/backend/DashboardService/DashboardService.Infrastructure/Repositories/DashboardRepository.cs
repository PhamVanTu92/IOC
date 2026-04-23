using System.Data;
using Dapper;
using DashboardService.Domain;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace DashboardService.Infrastructure.Repositories;

// ─────────────────────────────────────────────────────────────────────────────
// DashboardRepository — Dapper implementation
// All queries are scoped by tenant_id for multi-tenant isolation.
// config_json is stored as PostgreSQL JSONB.
// ─────────────────────────────────────────────────────────────────────────────

public sealed class DashboardRepository(
    string connectionString,
    ILogger<DashboardRepository> logger)
    : IDashboardRepository
{
    private IDbConnection OpenConnection() => new NpgsqlConnection(connectionString);

    // ── Row model for Dapper mapping ──────────────────────────────────────────

    // Dapper maps by constructor parameter name (case-insensitive) → must match DB column names (snake_case)
    private sealed record DashboardRow(
        Guid id,
        Guid tenant_id,
        Guid created_by,
        string title,
        string? description,
        string config_json,
        bool is_active,
        DateTime created_at,
        DateTime updated_at);

    // ── Queries ───────────────────────────────────────────────────────────────

    public async Task<Dashboard?> GetByIdAsync(Guid id, Guid tenantId, CancellationToken ct)
    {
        const string sql = """
            SELECT id, tenant_id, created_by, title, description,
                   config_json, is_active, created_at, updated_at
            FROM dashboards
            WHERE id = @Id AND tenant_id = @TenantId AND is_active = true
            """;

        using var conn = OpenConnection();
        var row = await conn.QuerySingleOrDefaultAsync<DashboardRow>(
            new CommandDefinition(sql, new { Id = id, TenantId = tenantId },
                cancellationToken: ct));

        return row is null ? null : Reconstitute(row);
    }

    public async Task<IReadOnlyList<Dashboard>> ListByTenantAsync(
        Guid tenantId, bool includeInactive, CancellationToken ct)
    {
        const string sql = """
            SELECT id, tenant_id, created_by, title, description,
                   config_json, is_active, created_at, updated_at
            FROM dashboards
            WHERE tenant_id = @TenantId
              AND (@IncludeInactive OR is_active = true)
            ORDER BY updated_at DESC
            """;

        using var conn = OpenConnection();
        var rows = await conn.QueryAsync<DashboardRow>(
            new CommandDefinition(sql,
                new { TenantId = tenantId, IncludeInactive = includeInactive },
                cancellationToken: ct));

        return rows.Select(Reconstitute).ToList();
    }

    public async Task<bool> ExistsAsync(Guid id, Guid tenantId, CancellationToken ct)
    {
        const string sql = """
            SELECT COUNT(1) FROM dashboards
            WHERE id = @Id AND tenant_id = @TenantId AND is_active = true
            """;

        using var conn = OpenConnection();
        var count = await conn.ExecuteScalarAsync<int>(
            new CommandDefinition(sql, new { Id = id, TenantId = tenantId },
                cancellationToken: ct));
        return count > 0;
    }

    // ── Commands ──────────────────────────────────────────────────────────────

    public async Task AddAsync(Dashboard dashboard, CancellationToken ct)
    {
        const string sql = """
            INSERT INTO dashboards
                (id, tenant_id, created_by, title, description,
                 config_json, is_active, created_at, updated_at)
            VALUES
                (@Id, @TenantId, @CreatedBy, @Title, @Description,
                 @ConfigJson::jsonb, @IsActive, @CreatedAt, @UpdatedAt)
            """;

        using var conn = OpenConnection();
        await conn.ExecuteAsync(
            new CommandDefinition(sql, new
            {
                dashboard.Id,
                dashboard.TenantId,
                dashboard.CreatedBy,
                dashboard.Title,
                dashboard.Description,
                dashboard.ConfigJson,
                dashboard.IsActive,
                dashboard.CreatedAt,
                dashboard.UpdatedAt,
            }, cancellationToken: ct));

        logger.LogDebug("Inserted dashboard {Id}", dashboard.Id);
    }

    public async Task UpdateAsync(Dashboard dashboard, CancellationToken ct)
    {
        const string sql = """
            UPDATE dashboards
            SET title       = @Title,
                description = @Description,
                config_json = @ConfigJson::jsonb,
                is_active   = @IsActive,
                updated_at  = @UpdatedAt
            WHERE id = @Id AND tenant_id = @TenantId
            """;

        using var conn = OpenConnection();
        var affected = await conn.ExecuteAsync(
            new CommandDefinition(sql, new
            {
                dashboard.Id,
                dashboard.TenantId,
                dashboard.Title,
                dashboard.Description,
                dashboard.ConfigJson,
                dashboard.IsActive,
                dashboard.UpdatedAt,
            }, cancellationToken: ct));

        if (affected == 0)
            logger.LogWarning("UpdateAsync affected 0 rows for dashboard {Id}", dashboard.Id);
    }

    // ── Reconstitute helper ───────────────────────────────────────────────────

    private static Dashboard Reconstitute(DashboardRow r) =>
        Dashboard.Reconstitute(
            r.id, r.tenant_id, r.created_by,
            r.title, r.description, r.config_json,
            r.is_active, r.created_at, r.updated_at);
}
