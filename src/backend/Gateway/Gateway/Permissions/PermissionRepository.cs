using Dapper;
using Npgsql;

namespace Gateway.Permissions;

public sealed record ModulePermissionRow(
    Guid user_id, Guid module_id, bool can_view, bool can_edit, DateTime granted_at);

public sealed record ChartPermissionRow(
    Guid user_id, Guid chart_id, bool can_view, bool can_edit, DateTime granted_at);

public sealed class PermissionRepository(string connectionString)
{
    private NpgsqlConnection Conn() => new(connectionString);

    public async Task<HashSet<Guid>> GetUserModuleIdsAsync(Guid userId, CancellationToken ct)
    {
        await using var conn = Conn();
        var ids = await conn.QueryAsync<Guid>(new CommandDefinition(
            "SELECT module_id FROM user_module_permissions WHERE user_id=@U AND can_view=true",
            new { U = userId }, cancellationToken: ct));
        return ids.ToHashSet();
    }

    public async Task<List<ModulePermissionRow>> GetModulePermissionsAsync(Guid moduleId, CancellationToken ct)
    {
        await using var conn = Conn();
        var rows = await conn.QueryAsync<ModulePermissionRow>(new CommandDefinition(
            "SELECT user_id, module_id, can_view, can_edit, granted_at FROM user_module_permissions WHERE module_id=@M",
            new { M = moduleId }, cancellationToken: ct));
        return rows.ToList();
    }

    public async Task UpsertModulePermissionAsync(
        Guid userId, Guid moduleId, bool canView, bool canEdit, Guid grantedBy, CancellationToken ct)
    {
        await using var conn = Conn();
        await conn.ExecuteAsync(new CommandDefinition(
            """
            INSERT INTO user_module_permissions (user_id, module_id, can_view, can_edit, granted_by)
            VALUES (@UserId, @ModuleId, @View, @Edit, @By)
            ON CONFLICT (user_id, module_id)
            DO UPDATE SET can_view=@View, can_edit=@Edit, granted_by=@By, granted_at=NOW()
            """,
            new { UserId = userId, ModuleId = moduleId, View = canView, Edit = canEdit, By = grantedBy },
            cancellationToken: ct));
    }

    public async Task<bool> RevokeModulePermissionAsync(Guid userId, Guid moduleId, CancellationToken ct)
    {
        await using var conn = Conn();
        var rows = await conn.ExecuteAsync(new CommandDefinition(
            "DELETE FROM user_module_permissions WHERE user_id=@U AND module_id=@M",
            new { U = userId, M = moduleId }, cancellationToken: ct));
        return rows > 0;
    }

    public async Task UpsertChartPermissionAsync(
        Guid userId, Guid chartId, bool canView, bool canEdit, Guid grantedBy, CancellationToken ct)
    {
        await using var conn = Conn();
        await conn.ExecuteAsync(new CommandDefinition(
            """
            INSERT INTO user_chart_permissions (user_id, chart_id, can_view, can_edit, granted_by)
            VALUES (@UserId, @ChartId, @View, @Edit, @By)
            ON CONFLICT (user_id, chart_id)
            DO UPDATE SET can_view=@View, can_edit=@Edit, granted_by=@By, granted_at=NOW()
            """,
            new { UserId = userId, ChartId = chartId, View = canView, Edit = canEdit, By = grantedBy },
            cancellationToken: ct));
    }
}
