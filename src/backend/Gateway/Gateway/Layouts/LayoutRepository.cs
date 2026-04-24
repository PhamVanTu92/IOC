using Dapper;
using Npgsql;

namespace Gateway.Layouts;

public sealed record LayoutRow(
    Guid  id,
    Guid  user_id,
    Guid? module_id,
    string layout_json,
    DateTime created_at,
    DateTime updated_at);

public sealed class LayoutRepository(string connectionString)
{
    private NpgsqlConnection Conn() => new(connectionString);

    public async Task<LayoutRow?> GetAsync(Guid userId, Guid? moduleId, CancellationToken ct)
    {
        await using var conn = Conn();
        return await conn.QuerySingleOrDefaultAsync<LayoutRow>(new CommandDefinition(
            moduleId.HasValue
                ? "SELECT * FROM dashboard_layouts WHERE user_id=@U AND module_id=@M"
                : "SELECT * FROM dashboard_layouts WHERE user_id=@U AND module_id IS NULL",
            new { U = userId, M = moduleId },
            cancellationToken: ct));
    }

    public async Task UpsertAsync(Guid userId, Guid? moduleId, string layoutJson, CancellationToken ct)
    {
        await using var conn = Conn();
        await conn.ExecuteAsync(new CommandDefinition(
            moduleId.HasValue
                ? """
                  INSERT INTO dashboard_layouts (user_id, module_id, layout_json)
                  VALUES (@U, @M, @Json)
                  ON CONFLICT (user_id, module_id)
                  DO UPDATE SET layout_json=@Json, updated_at=NOW()
                  """
                : """
                  INSERT INTO dashboard_layouts (user_id, module_id, layout_json)
                  VALUES (@U, NULL, @Json)
                  ON CONFLICT (user_id, module_id)
                  DO UPDATE SET layout_json=@Json, updated_at=NOW()
                  """,
            new { U = userId, M = moduleId, Json = layoutJson },
            cancellationToken: ct));
    }
}
