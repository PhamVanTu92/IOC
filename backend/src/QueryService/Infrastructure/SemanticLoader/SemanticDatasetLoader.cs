using Dapper;
using Npgsql;
using QueryService.Application.Interfaces;
using SemanticEngine.Models;

namespace QueryService.Infrastructure.SemanticLoader;

/// <summary>
/// Dapper-based implementation của ISemanticDatasetLoader.
/// Đọc trực tiếp từ các bảng datasets/dimensions/measures/metrics qua Npgsql.
/// Sử dụng song song queries để giảm latency.
/// </summary>
public sealed class SemanticDatasetLoader : ISemanticDatasetLoader
{
    private readonly string _connectionString;

    public SemanticDatasetLoader(string connectionString)
        => _connectionString = connectionString;

    public async Task<SemanticDataset?> LoadAsync(
        Guid datasetId, Guid tenantId, CancellationToken ct = default)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync(ct);

        // Load dataset, dimensions, measures, metrics song song
        var datasetTask    = LoadDatasetRow(conn, datasetId, tenantId, ct);
        var dimensionsTask = LoadDimensions(conn, datasetId, tenantId, ct);
        var measuresTask   = LoadMeasures(conn, datasetId, tenantId, ct);
        var metricsTask    = LoadMetrics(conn, datasetId, tenantId, ct);

        await Task.WhenAll(datasetTask, dimensionsTask, measuresTask, metricsTask);

        var row = await datasetTask;
        if (row is null) return null;

        return new SemanticDataset
        {
            Id          = row.id,
            TenantId    = row.tenant_id,
            Name        = row.name,
            Description = row.description,
            SourceType  = row.source_type,
            SchemaName  = row.schema_name,
            TableName   = row.table_name,
            CustomSql   = row.custom_sql,
            Dimensions  = (await dimensionsTask).Select(MapDimension).ToList(),
            Measures    = (await measuresTask).Select(MapMeasure).ToList(),
            Metrics     = (await metricsTask).Select(MapMetric).ToList()
        };
    }

    // ─── Raw DB queries ────────────────────────────────────────────────────────

    private static async Task<DatasetRow?> LoadDatasetRow(
        NpgsqlConnection conn, Guid datasetId, Guid tenantId, CancellationToken ct)
    {
        const string sql = """
            SELECT id, tenant_id, name, description, source_type,
                   schema_name, table_name, custom_sql
            FROM datasets
            WHERE id = @datasetId AND tenant_id = @tenantId AND is_active = TRUE
            """;
        return await conn.QuerySingleOrDefaultAsync<DatasetRow>(
            new CommandDefinition(sql, new { datasetId, tenantId }, cancellationToken: ct));
    }

    private static async Task<IEnumerable<DimensionRow>> LoadDimensions(
        NpgsqlConnection conn, Guid datasetId, Guid tenantId, CancellationToken ct)
    {
        const string sql = """
            SELECT id, name, display_name, description, column_name,
                   custom_sql_expression, data_type, format,
                   is_time_dimension, default_granularity, sort_order
            FROM dimensions
            WHERE dataset_id = @datasetId AND tenant_id = @tenantId AND is_active = TRUE
            ORDER BY sort_order ASC, name ASC
            """;
        return await conn.QueryAsync<DimensionRow>(
            new CommandDefinition(sql, new { datasetId, tenantId }, cancellationToken: ct));
    }

    private static async Task<IEnumerable<MeasureRow>> LoadMeasures(
        NpgsqlConnection conn, Guid datasetId, Guid tenantId, CancellationToken ct)
    {
        const string sql = """
            SELECT id, name, display_name, description, column_name,
                   custom_sql_expression, aggregation_type, data_type,
                   format, filter_expression, sort_order
            FROM measures
            WHERE dataset_id = @datasetId AND tenant_id = @tenantId AND is_active = TRUE
            ORDER BY sort_order ASC, name ASC
            """;
        return await conn.QueryAsync<MeasureRow>(
            new CommandDefinition(sql, new { datasetId, tenantId }, cancellationToken: ct));
    }

    private static async Task<IEnumerable<MetricRow>> LoadMetrics(
        NpgsqlConnection conn, Guid datasetId, Guid tenantId, CancellationToken ct)
    {
        const string sql = """
            SELECT id, name, display_name, description, expression,
                   data_type, format, depends_on_measures, sort_order
            FROM metrics
            WHERE dataset_id = @datasetId AND tenant_id = @tenantId AND is_active = TRUE
            ORDER BY sort_order ASC, name ASC
            """;
        return await conn.QueryAsync<MetricRow>(
            new CommandDefinition(sql, new { datasetId, tenantId }, cancellationToken: ct));
    }

    // ─── Mappers: DB row → SemanticEngine model ────────────────────────────────

    private static SemanticDimension MapDimension(DimensionRow r) => new()
    {
        Id               = r.id,
        Name             = r.name,
        DisplayName      = r.display_name,
        Description      = r.description,
        ColumnName       = r.column_name,
        CustomSqlExpression = r.custom_sql_expression,
        DataType         = ParseDataType(r.data_type),
        Format           = r.format,
        IsTimeDimension  = r.is_time_dimension,
        DefaultGranularity = r.default_granularity is not null
            ? ParseGranularity(r.default_granularity)
            : null,
        SortOrder = r.sort_order
    };

    private static SemanticMeasure MapMeasure(MeasureRow r) => new()
    {
        Id                 = r.id,
        Name               = r.name,
        DisplayName        = r.display_name,
        Description        = r.description,
        ColumnName         = r.column_name,
        CustomSqlExpression = r.custom_sql_expression,
        AggregationType    = ParseAggregationType(r.aggregation_type),
        DataType           = ParseDataType(r.data_type),
        Format             = r.format,
        FilterExpression   = r.filter_expression,
        SortOrder          = r.sort_order
    };

    private static SemanticMetric MapMetric(MetricRow r) => new()
    {
        Id               = r.id,
        Name             = r.name,
        DisplayName      = r.display_name,
        Description      = r.description,
        Expression       = r.expression,
        DataType         = ParseDataType(r.data_type),
        Format           = r.format,
        DependsOnMeasures = r.depends_on_measures ?? [],
        SortOrder        = r.sort_order
    };

    // ─── Enum parsers ──────────────────────────────────────────────────────────

    private static DataType ParseDataType(string? value) =>
        value?.ToLowerInvariant() switch
        {
            "string"   => DataType.String,
            "number"   => DataType.Number,
            "integer"  => DataType.Integer,
            "decimal"  => DataType.Decimal,
            "date"     => DataType.Date,
            "datetime" => DataType.DateTime,
            "boolean"  => DataType.Boolean,
            "json"     => DataType.Json,
            _          => DataType.String
        };

    private static AggregationType ParseAggregationType(string? value) =>
        value?.ToLowerInvariant() switch
        {
            "sum"            => AggregationType.Sum,
            "average"        => AggregationType.Average,
            "count"          => AggregationType.Count,
            "count_distinct" => AggregationType.CountDistinct,
            "min"            => AggregationType.Min,
            "max"            => AggregationType.Max,
            "running_total"  => AggregationType.RunningTotal,
            _                => AggregationType.Sum
        };

    private static TimeGranularity ParseGranularity(string value) =>
        value.ToLowerInvariant() switch
        {
            "hour"    => TimeGranularity.Hour,
            "day"     => TimeGranularity.Day,
            "week"    => TimeGranularity.Week,
            "month"   => TimeGranularity.Month,
            "quarter" => TimeGranularity.Quarter,
            "year"    => TimeGranularity.Year,
            _         => TimeGranularity.Day
        };

    // ─── DB row types (Dapper mapping) ─────────────────────────────────────────

    private sealed record DatasetRow(
        Guid id, Guid tenant_id, string name, string? description,
        string source_type, string? schema_name, string? table_name, string? custom_sql);

    private sealed record DimensionRow(
        Guid id, string name, string display_name, string? description,
        string column_name, string? custom_sql_expression, string? data_type,
        string? format, bool is_time_dimension, string? default_granularity, int sort_order);

    private sealed record MeasureRow(
        Guid id, string name, string display_name, string? description,
        string column_name, string? custom_sql_expression, string? aggregation_type,
        string? data_type, string? format, string? filter_expression, int sort_order);

    private sealed record MetricRow(
        Guid id, string name, string display_name, string? description,
        string expression, string? data_type, string? format,
        string[]? depends_on_measures, int sort_order);
}
