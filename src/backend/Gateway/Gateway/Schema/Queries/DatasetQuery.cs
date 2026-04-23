using System.Text.Json;
using Dapper;
using Gateway.Infrastructure;
using Gateway.Schema.Types;
using HotChocolate;
using HotChocolate.Types;
using Microsoft.Extensions.Configuration;
using Npgsql;

namespace Gateway.Schema.Queries;

// ─────────────────────────────────────────────────────────────────────────────
// DatasetQuery — read operations for datasets (semantic layer registry)
// ─────────────────────────────────────────────────────────────────────────────

[ExtendObjectType(OperationTypeNames.Query)]
public sealed class DatasetQuery
{
    // ── Row model ─────────────────────────────────────────────────────────────

    private sealed record DatasetRow(
        Guid Id,
        Guid? TenantId,
        string Name,
        string? Description,
        string ConfigJson,
        bool IsActive,
        DateTime CreatedAt);

    // ── Queries ───────────────────────────────────────────────────────────────

    /// <summary>List all datasets visible to the current tenant.</summary>
    public async Task<IReadOnlyList<DatasetSummaryGql>> DatasetsAsync(
        [Service] IConfiguration configuration,
        [Service] TenantContext tenantContext,
        CancellationToken cancellationToken,
        bool includeInactive = false)
    {
        var cs = GetConnectionString(configuration);

        const string sql = """
            SELECT id, tenant_id, name, description, config_json, is_active, created_at
            FROM datasets
            WHERE (tenant_id IS NULL OR tenant_id = @TenantId)
              AND (@IncludeInactive OR is_active = true)
            ORDER BY name
            """;

        await using var conn = new NpgsqlConnection(cs);
        var rows = await conn.QueryAsync<DatasetRow>(
            new CommandDefinition(sql,
                new { TenantId = tenantContext.TenantId, IncludeInactive = includeInactive },
                cancellationToken: cancellationToken));

        return rows.Select(ToSummary).ToList();
    }

    /// <summary>Get a single dataset by id.</summary>
    public async Task<DatasetDetailGql?> DatasetAsync(
        Guid id,
        [Service] IConfiguration configuration,
        [Service] TenantContext tenantContext,
        CancellationToken cancellationToken)
    {
        var cs = GetConnectionString(configuration);

        const string sql = """
            SELECT id, tenant_id, name, description, config_json, is_active, created_at
            FROM datasets
            WHERE id = @Id
              AND (tenant_id IS NULL OR tenant_id = @TenantId)
            """;

        await using var conn = new NpgsqlConnection(cs);
        var row = await conn.QuerySingleOrDefaultAsync<DatasetRow>(
            new CommandDefinition(sql,
                new { Id = id, TenantId = tenantContext.TenantId },
                cancellationToken: cancellationToken));

        return row is null ? null : ToDetail(row);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static string GetConnectionString(IConfiguration configuration) =>
        configuration.GetConnectionString("DefaultConnection")
        ?? throw new InvalidOperationException("Missing 'DefaultConnection'");

    private static DatasetSummaryGql ToSummary(DatasetRow r)
    {
        var cfg = ParseConfig(r.ConfigJson);
        return new DatasetSummaryGql(
            Id: r.Id,
            TenantId: r.TenantId,
            Name: r.Name,
            Description: r.Description,
            SourceType: cfg.SourceType,
            IsActive: r.IsActive,
            CreatedAt: r.CreatedAt,
            UpdatedAt: r.CreatedAt);
    }

    private static DatasetDetailGql ToDetail(DatasetRow r)
    {
        var cfg = ParseConfig(r.ConfigJson);
        return new DatasetDetailGql(
            Id: r.Id,
            TenantId: r.TenantId,
            Name: r.Name,
            Description: r.Description,
            SourceType: cfg.SourceType,
            SchemaName: cfg.SchemaName,
            TableName: cfg.TableName,
            CustomSql: cfg.CustomSql,
            IsActive: r.IsActive,
            CreatedAt: r.CreatedAt,
            UpdatedAt: r.CreatedAt,
            Dimensions: [],
            Measures: [],
            Metrics: []);
    }

    private static DatasetConfig ParseConfig(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            return new DatasetConfig(
                SourceType: root.TryGetProperty("sourceType", out var st) ? st.GetString() ?? "postgres" : "postgres",
                SchemaName: root.TryGetProperty("schemaName", out var sn) ? sn.GetString() : null,
                TableName:  root.TryGetProperty("tableName",  out var tn) ? tn.GetString() : null,
                CustomSql:  root.TryGetProperty("customSql",  out var cs) ? cs.GetString() : null);
        }
        catch
        {
            return new DatasetConfig("postgres", null, null, null);
        }
    }

    private sealed record DatasetConfig(
        string SourceType,
        string? SchemaName,
        string? TableName,
        string? CustomSql);
}
