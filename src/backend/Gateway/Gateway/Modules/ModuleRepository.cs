using Dapper;
using Npgsql;

namespace Gateway.Modules;

// Row model
public sealed record ModuleRow(
    Guid    id,
    Guid    tenant_id,
    string  name,
    string  slug,
    string? description,
    string  icon,
    string  color,
    bool    is_active,
    int     sort_order,
    Guid?   created_by,
    DateTime created_at,
    DateTime updated_at);

public sealed class ModuleRepository(string connectionString)
{
    private NpgsqlConnection Conn() => new(connectionString);

    public async Task<List<ModuleRow>> GetByTenantAsync(Guid tenantId, CancellationToken ct)
    {
        await using var conn = Conn();
        var rows = await conn.QueryAsync<ModuleRow>(new CommandDefinition(
            "SELECT * FROM modules WHERE tenant_id=@T AND is_active=true ORDER BY sort_order, name",
            new { T = tenantId }, cancellationToken: ct));
        return rows.ToList();
    }

    public async Task<ModuleRow?> GetByIdAsync(Guid id, Guid tenantId, CancellationToken ct)
    {
        await using var conn = Conn();
        return await conn.QuerySingleOrDefaultAsync<ModuleRow>(new CommandDefinition(
            "SELECT * FROM modules WHERE id=@Id AND tenant_id=@T",
            new { Id = id, T = tenantId }, cancellationToken: ct));
    }

    public async Task<Guid> CreateAsync(Guid tenantId, Guid createdBy, string name, string slug,
        string? description, string icon, string color, int sortOrder, CancellationToken ct)
    {
        await using var conn = Conn();
        return await conn.ExecuteScalarAsync<Guid>(new CommandDefinition(
            """
            INSERT INTO modules (tenant_id, name, slug, description, icon, color, sort_order, created_by)
            VALUES (@TenantId, @Name, @Slug, @Desc, @Icon, @Color, @Sort, @By)
            RETURNING id
            """,
            new { TenantId = tenantId, Name = name, Slug = slug, Desc = description,
                  Icon = icon, Color = color, Sort = sortOrder, By = createdBy },
            cancellationToken: ct));
    }

    public async Task<bool> UpdateAsync(Guid id, Guid tenantId, string name, string? description,
        string icon, string color, int sortOrder, CancellationToken ct)
    {
        await using var conn = Conn();
        var rows = await conn.ExecuteAsync(new CommandDefinition(
            """
            UPDATE modules SET name=@Name, description=@Desc, icon=@Icon, color=@Color,
                sort_order=@Sort, updated_at=NOW()
            WHERE id=@Id AND tenant_id=@T
            """,
            new { Id = id, T = tenantId, Name = name, Desc = description,
                  Icon = icon, Color = color, Sort = sortOrder },
            cancellationToken: ct));
        return rows > 0;
    }

    public async Task<bool> DeleteAsync(Guid id, Guid tenantId, CancellationToken ct)
    {
        await using var conn = Conn();
        var rows = await conn.ExecuteAsync(new CommandDefinition(
            "UPDATE modules SET is_active=false, updated_at=NOW() WHERE id=@Id AND tenant_id=@T",
            new { Id = id, T = tenantId }, cancellationToken: ct));
        return rows > 0;
    }
}
