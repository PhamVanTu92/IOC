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
    // ── Row models (snake_case to match DB column names for Dapper) ───────────

    private sealed record DatasetRow(
        Guid id,
        Guid? tenant_id,
        string name,
        string? description,
        string config_json,
        bool is_active,
        DateTime created_at);

    private sealed record DimensionRow(
        Guid id,
        Guid dataset_id,
        string name,
        string display_name,
        string? description,
        string data_type,
        string? format,
        bool is_time_dimension,
        string? default_granularity,
        int sort_order,
        bool is_active);

    private sealed record MeasureRow(
        Guid id,
        Guid dataset_id,
        string name,
        string display_name,
        string? description,
        string aggregation_type,
        string data_type,
        string? format,
        string? filter_expression,
        int sort_order,
        bool is_active);

    private sealed record MetricRow(
        Guid id,
        Guid dataset_id,
        string name,
        string display_name,
        string? description,
        string expression,
        string data_type,
        string? format,
        string[] depends_on_measures,
        int sort_order,
        bool is_active);

    // ── Queries ───────────────────────────────────────────────────────────────

    public async Task<IReadOnlyList<DatasetSummaryGql>> DatasetsAsync(
        [Service] IConfiguration configuration,
        [Service] TenantContext tenantContext,
        CancellationToken cancellationToken,
        bool includeInactive = false)
    {
        const string sql = """
            SELECT id, tenant_id, name, description, config_json, is_active, created_at
            FROM datasets
            WHERE (tenant_id IS NULL OR tenant_id = @TenantId)
              AND (@IncludeInactive OR is_active = true)
            ORDER BY name
            """;

        await using var conn = new NpgsqlConnection(GetConnectionString(configuration));
        var rows = await conn.QueryAsync<DatasetRow>(
            new CommandDefinition(sql,
                new { TenantId = tenantContext.TenantId, IncludeInactive = includeInactive },
                cancellationToken: cancellationToken));

        return rows.Select(ToSummary).ToList();
    }

    public async Task<DatasetDetailGql?> DatasetAsync(
        Guid id,
        [Service] IConfiguration configuration,
        [Service] TenantContext tenantContext,
        CancellationToken cancellationToken)
    {
        const string datasetSql = """
            SELECT id, tenant_id, name, description, config_json, is_active, created_at
            FROM datasets
            WHERE id = @Id AND (tenant_id IS NULL OR tenant_id = @TenantId)
            """;

        const string dimSql = """
            SELECT id, dataset_id, name, display_name, description,
                   data_type, format, is_time_dimension, default_granularity, sort_order, is_active
            FROM dimensions
            WHERE dataset_id = @Id AND is_active = true
            ORDER BY sort_order
            """;

        const string measureSql = """
            SELECT id, dataset_id, name, display_name, description,
                   aggregation_type, data_type, format, filter_expression, sort_order, is_active
            FROM measures
            WHERE dataset_id = @Id AND is_active = true
            ORDER BY sort_order
            """;

        const string metricSql = """
            SELECT id, dataset_id, name, display_name, description,
                   expression, data_type, format, depends_on_measures, sort_order, is_active
            FROM metrics
            WHERE dataset_id = @Id AND is_active = true
            ORDER BY sort_order
            """;

        await using var conn = new NpgsqlConnection(GetConnectionString(configuration));
        await conn.OpenAsync(cancellationToken);

        var row = await conn.QuerySingleOrDefaultAsync<DatasetRow>(
            new CommandDefinition(datasetSql,
                new { Id = id, TenantId = tenantContext.TenantId },
                cancellationToken: cancellationToken));

        if (row is null) return null;

        var dims    = (await conn.QueryAsync<DimensionRow>(new CommandDefinition(dimSql,    new { Id = id }, cancellationToken: cancellationToken))).ToList();
        var measures= (await conn.QueryAsync<MeasureRow>  (new CommandDefinition(measureSql,new { Id = id }, cancellationToken: cancellationToken))).ToList();
        var metrics = (await conn.QueryAsync<MetricRow>   (new CommandDefinition(metricSql, new { Id = id }, cancellationToken: cancellationToken))).ToList();

        return ToDetail(row, dims, measures, metrics);
    }

    // ── Mapping ───────────────────────────────────────────────────────────────

    private static DatasetSummaryGql ToSummary(DatasetRow r)
    {
        var cfg = ParseConfig(r.config_json);
        return new DatasetSummaryGql(
            Id: r.id,
            TenantId: r.tenant_id,
            Name: r.name,
            Description: r.description,
            SourceType: cfg.SourceType,
            IsActive: r.is_active,
            CreatedAt: r.created_at,
            UpdatedAt: r.created_at);
    }

    private static DatasetDetailGql ToDetail(
        DatasetRow r,
        IReadOnlyList<DimensionRow> dims,
        IReadOnlyList<MeasureRow> measures,
        IReadOnlyList<MetricRow> metrics)
    {
        var cfg = ParseConfig(r.config_json);
        return new DatasetDetailGql(
            Id: r.id,
            TenantId: r.tenant_id,
            Name: r.name,
            Description: r.description,
            SourceType: cfg.SourceType,
            SchemaName: cfg.SchemaName,
            TableName: cfg.TableName,
            CustomSql: cfg.CustomSql,
            IsActive: r.is_active,
            CreatedAt: r.created_at,
            UpdatedAt: r.created_at,
            Dimensions: dims.Select(d => new DimensionGql(
                Id: d.id, DatasetId: d.dataset_id, Name: d.name,
                DisplayName: d.display_name, Description: d.description,
                DataType: d.data_type, Format: d.format,
                IsTimeDimension: d.is_time_dimension,
                DefaultGranularity: d.default_granularity,
                SortOrder: d.sort_order, IsActive: d.is_active)).ToList(),
            Measures: measures.Select(m => new MeasureGql(
                Id: m.id, DatasetId: m.dataset_id, Name: m.name,
                DisplayName: m.display_name, Description: m.description,
                AggregationType: m.aggregation_type, DataType: m.data_type,
                Format: m.format, FilterExpression: m.filter_expression,
                SortOrder: m.sort_order, IsActive: m.is_active)).ToList(),
            Metrics: metrics.Select(x => new MetricGql(
                Id: x.id, DatasetId: x.dataset_id, Name: x.name,
                DisplayName: x.display_name, Description: x.description,
                Expression: x.expression, DataType: x.data_type,
                Format: x.format, DependsOnMeasures: x.depends_on_measures,
                SortOrder: x.sort_order, IsActive: x.is_active)).ToList());
    }

    private static string GetConnectionString(IConfiguration configuration) =>
        configuration.GetConnectionString("DefaultConnection")
        ?? throw new InvalidOperationException("Missing 'DefaultConnection'");

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
        catch { return new DatasetConfig("postgres", null, null, null); }
    }

    private sealed record DatasetConfig(
        string SourceType,
        string? SchemaName,
        string? TableName,
        string? CustomSql);
}
