using Dapper;
using Npgsql;

namespace Gateway.Charts;

public sealed record ChartRow(
    Guid    id,
    Guid    module_id,
    Guid    tenant_id,
    string  name,
    string? description,
    string  chart_type,
    string  config_json,
    bool    is_active,
    int     sort_order,
    Guid?   created_by,
    DateTime created_at,
    DateTime updated_at);

public sealed class ChartRepository(string connectionString)
{
    private NpgsqlConnection Conn() => new(connectionString);

    public async Task<List<ChartRow>> GetByModuleAsync(Guid moduleId, Guid tenantId, CancellationToken ct)
    {
        await using var conn = Conn();
        var rows = await conn.QueryAsync<ChartRow>(new CommandDefinition(
            "SELECT * FROM charts WHERE module_id=@M AND tenant_id=@T AND is_active=true ORDER BY sort_order, name",
            new { M = moduleId, T = tenantId }, cancellationToken: ct));
        return rows.ToList();
    }

    public async Task<ChartRow?> GetByIdAsync(Guid id, Guid tenantId, CancellationToken ct)
    {
        await using var conn = Conn();
        return await conn.QuerySingleOrDefaultAsync<ChartRow>(new CommandDefinition(
            "SELECT * FROM charts WHERE id=@Id AND tenant_id=@T",
            new { Id = id, T = tenantId }, cancellationToken: ct));
    }

    public async Task<Guid> CreateAsync(Guid moduleId, Guid tenantId, Guid createdBy,
        string name, string? description, string chartType, string configJson,
        int sortOrder, CancellationToken ct)
    {
        await using var conn = Conn();
        return await conn.ExecuteScalarAsync<Guid>(new CommandDefinition(
            """
            INSERT INTO charts (module_id, tenant_id, name, description, chart_type, config_json, sort_order, created_by)
            VALUES (@ModId, @TenantId, @Name, @Desc, @Type, @Config, @Sort, @By)
            RETURNING id
            """,
            new { ModId = moduleId, TenantId = tenantId, Name = name, Desc = description,
                  Type = chartType, Config = configJson, Sort = sortOrder, By = createdBy },
            cancellationToken: ct));
    }

    public async Task<bool> UpdateAsync(Guid id, Guid tenantId, string name, string? description,
        string chartType, string configJson, int sortOrder, CancellationToken ct)
    {
        await using var conn = Conn();
        var rows = await conn.ExecuteAsync(new CommandDefinition(
            """
            UPDATE charts SET name=@Name, description=@Desc, chart_type=@Type,
                config_json=@Config, sort_order=@Sort, updated_at=NOW()
            WHERE id=@Id AND tenant_id=@T
            """,
            new { Id = id, T = tenantId, Name = name, Desc = description,
                  Type = chartType, Config = configJson, Sort = sortOrder },
            cancellationToken: ct));
        return rows > 0;
    }

    public async Task<bool> DeleteAsync(Guid id, Guid tenantId, CancellationToken ct)
    {
        await using var conn = Conn();
        var rows = await conn.ExecuteAsync(new CommandDefinition(
            "UPDATE charts SET is_active=false, updated_at=NOW() WHERE id=@Id AND tenant_id=@T",
            new { Id = id, T = tenantId }, cancellationToken: ct));
        return rows > 0;
    }
}
