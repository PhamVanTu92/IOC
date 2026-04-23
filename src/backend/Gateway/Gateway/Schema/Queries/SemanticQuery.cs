using System.Diagnostics;
using System.Text;
using System.Text.Json;
using Dapper;
using Gateway.Infrastructure;
using Gateway.Schema.Types;
using HotChocolate;
using HotChocolate.Types;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace Gateway.Schema.Queries;

// ─────────────────────────────────────────────────────────────────────────────
// SemanticQuery — executes dynamic queries via the Semantic Layer
//
// Flow:
//   1. Load dataset + field definitions from metadata DB
//   2. Map selected field names → column expressions
//   3. Build SQL (SELECT … FROM … WHERE … GROUP BY … ORDER BY … LIMIT …)
//   4. Execute against the data source (tableName in config_json)
//   5. Return rows as JSON strings + column metadata
// ─────────────────────────────────────────────────────────────────────────────

[ExtendObjectType(OperationTypeNames.Query)]
public sealed class SemanticQuery(ILogger<SemanticQuery> logger)
{
    // ── Row models ────────────────────────────────────────────────────────────

    private sealed record DatasetMeta(
        Guid id, string name, string config_json, bool is_active);

    private sealed record DimMeta(
        Guid id, string name, string display_name, string? description,
        string column_name, string? custom_sql_expression,
        string data_type, string? format,
        bool is_time_dimension, string? default_granularity);

    private sealed record MeasureMeta(
        Guid id, string name, string display_name, string? description,
        string column_name, string? custom_sql_expression,
        string aggregation_type, string data_type, string? format);

    private sealed record MetricMeta(
        Guid id, string name, string display_name, string? description,
        string expression, string data_type, string? format,
        string depends_on_measures);

    // ── Resolver ──────────────────────────────────────────────────────────────

    public async Task<QueryResultGql> ExecuteQueryAsync(
        QueryRequestInput input,
        [Service] IConfiguration configuration,
        [Service] TenantContext tenantContext,
        CancellationToken cancellationToken)
    {
        var sw = Stopwatch.StartNew();
        var cs = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Missing 'DefaultConnection'");

        try
        {
            await using var conn = new NpgsqlConnection(cs);
            await conn.OpenAsync(cancellationToken);

            // 1. Load metadata
            var dataset  = await LoadDatasetAsync(conn, input.DatasetId, tenantContext.TenantId, cancellationToken);
            if (dataset is null)
                return ErrorResult($"Dataset {input.DatasetId} not found", sw);

            var cfg = ParseDatasetConfig(dataset.config_json);

            var dims     = await LoadDimensionsAsync(conn, input.DatasetId, cancellationToken);
            var measures = await LoadMeasuresAsync  (conn, input.DatasetId, cancellationToken);
            var metrics  = await LoadMetricsAsync   (conn, input.DatasetId, cancellationToken);

            // 2. Resolve selected fields
            var selectedDims     = Resolve(input.Dimensions, dims,     d => d.name);
            var selectedMeasures = Resolve(input.Measures,   measures, m => m.name);
            var selectedMetrics  = Resolve(input.Metrics,    metrics,  m => m.name);

            // 3. Build column metadata (always returned even without data)
            var columns = BuildColumns(selectedDims, selectedMeasures, selectedMetrics);

            if (columns.Count == 0)
                return new QueryResultGql([], [], new QueryMetadataGql(
                    null, sw.ElapsedMilliseconds, 0, false, null, DateTime.UtcNow,
                    "No fields selected"));

            // 4. If no tableName configured → return schema only (preview mode)
            if (string.IsNullOrWhiteSpace(cfg.TableName) && string.IsNullOrWhiteSpace(cfg.CustomSql))
            {
                return new QueryResultGql(columns, [], new QueryMetadataGql(
                    null, sw.ElapsedMilliseconds, 0, false, null, DateTime.UtcNow,
                    "Dataset has no tableName configured — showing schema only"));
            }

            // 5. Build + execute SQL
            var (sql, parameters) = BuildSql(
                cfg, selectedDims, selectedMeasures, selectedMetrics,
                measures,  // allMeasures for metric expression resolution
                input.Filters, input.Sorts,
                input.Limit ?? 1000,
                input.TimeDimensionName, input.Granularity, input.TimeRange);

            logger.LogDebug("ExecuteQuery SQL: {Sql}", sql);

            var rows = await ExecuteSqlAsync(conn, sql, parameters, columns, cancellationToken);

            return new QueryResultGql(columns, rows, new QueryMetadataGql(
                GeneratedSql: sql,
                ExecutionTimeMs: sw.ElapsedMilliseconds,
                TotalRows: rows.Count,
                FromCache: false,
                CacheKey: null,
                ExecutedAt: DateTime.UtcNow,
                ErrorMessage: null));
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "ExecuteQuery failed for dataset {DatasetId}", input.DatasetId);
            return ErrorResult(ex.Message, sw);
        }
    }

    // ── Load metadata helpers ─────────────────────────────────────────────────

    private static async Task<DatasetMeta?> LoadDatasetAsync(
        NpgsqlConnection conn, Guid id, Guid tenantId, CancellationToken ct)
    {
        return await conn.QuerySingleOrDefaultAsync<DatasetMeta>(
            new CommandDefinition(
                "SELECT id, name, config_json, is_active FROM datasets WHERE id=@Id AND (tenant_id IS NULL OR tenant_id=@TenantId)",
                new { Id = id, TenantId = tenantId }, cancellationToken: ct));
    }

    private static async Task<List<DimMeta>> LoadDimensionsAsync(
        NpgsqlConnection conn, Guid datasetId, CancellationToken ct)
    {
        var rows = await conn.QueryAsync<DimMeta>(new CommandDefinition(
            "SELECT id, name, display_name, description, column_name, custom_sql_expression, data_type, format, is_time_dimension, default_granularity FROM dimensions WHERE dataset_id=@Id AND is_active=true",
            new { Id = datasetId }, cancellationToken: ct));
        return rows.ToList();
    }

    private static async Task<List<MeasureMeta>> LoadMeasuresAsync(
        NpgsqlConnection conn, Guid datasetId, CancellationToken ct)
    {
        var rows = await conn.QueryAsync<MeasureMeta>(new CommandDefinition(
            "SELECT id, name, display_name, description, column_name, custom_sql_expression, aggregation_type, data_type, format FROM measures WHERE dataset_id=@Id AND is_active=true",
            new { Id = datasetId }, cancellationToken: ct));
        return rows.ToList();
    }

    private static async Task<List<MetricMeta>> LoadMetricsAsync(
        NpgsqlConnection conn, Guid datasetId, CancellationToken ct)
    {
        var rows = await conn.QueryAsync<MetricMeta>(new CommandDefinition(
            "SELECT id, name, display_name, description, expression, data_type, format, array_to_string(depends_on_measures,',') AS depends_on_measures FROM metrics WHERE dataset_id=@Id AND is_active=true",
            new { Id = datasetId }, cancellationToken: ct));
        return rows.ToList();
    }

    // ── Resolve selected fields ───────────────────────────────────────────────

    private static List<T> Resolve<T>(
        string[]? selectedNames, List<T> all, Func<T, string> getName)
    {
        if (selectedNames is null or { Length: 0 }) return [];
        var lookup = all.ToDictionary(getName, StringComparer.OrdinalIgnoreCase);
        return selectedNames
            .Where(n => lookup.ContainsKey(n))
            .Select(n => lookup[n])
            .ToList();
    }

    // ── Build column metadata ─────────────────────────────────────────────────

    private static List<QueryColumnGql> BuildColumns(
        List<DimMeta> dims, List<MeasureMeta> measures, List<MetricMeta> metrics)
    {
        var cols = new List<QueryColumnGql>();
        cols.AddRange(dims.Select(d => new QueryColumnGql(d.name, d.display_name, d.data_type, d.format, "dimension")));
        cols.AddRange(measures.Select(m => new QueryColumnGql(m.name, m.display_name, m.data_type, m.format, "measure")));
        cols.AddRange(metrics.Select(x => new QueryColumnGql(x.name, x.display_name, x.data_type, x.format, "metric")));
        return cols;
    }

    // ── SQL builder ───────────────────────────────────────────────────────────

    private static (string sql, Dictionary<string, object?> parameters) BuildSql(
        DatasetConfig cfg,
        List<DimMeta> dims, List<MeasureMeta> measures, List<MetricMeta> metrics,
        List<MeasureMeta> allMeasures,
        QueryFilterInput[]? filters, QuerySortInput[]? sorts,
        int limit,
        string? timeDim, string? granularity, TimeRangeInput? timeRange)
    {
        var sb = new StringBuilder();
        var parameters = new Dictionary<string, object?>();

        // FROM
        var fromClause = !string.IsNullOrWhiteSpace(cfg.CustomSql)
            ? $"({cfg.CustomSql}) AS _data"
            : string.IsNullOrWhiteSpace(cfg.SchemaName)
                ? $"\"{cfg.TableName}\""
                : $"\"{cfg.SchemaName}\".\"{cfg.TableName}\"";

        // SELECT
        var selectParts = new List<string>();
        foreach (var d in dims)
        {
            var expr = !string.IsNullOrWhiteSpace(d.custom_sql_expression)
                ? d.custom_sql_expression
                : $"\"{d.column_name}\"";

            if (d.is_time_dimension && !string.IsNullOrWhiteSpace(granularity))
                expr = ApplyGranularity(expr, granularity);

            selectParts.Add($"{expr} AS \"{d.name}\"");
        }
        foreach (var m in measures)
        {
            var col = !string.IsNullOrWhiteSpace(m.custom_sql_expression)
                ? m.custom_sql_expression
                : $"\"{m.column_name}\"";
            selectParts.Add($"{m.aggregation_type.ToUpper()}({col}) AS \"{m.name}\"");
        }
        // Metrics: replace measure name references with their aggregate expressions
        // e.g. "profit / NULLIF(revenue,0)*100" → "SUM(profit) / NULLIF(SUM(revenue),0)*100"
        // Use allMeasures (not just selected) so expressions like "profit" resolve even if not selected
        foreach (var x in metrics)
        {
            var expr = ReplaceMeasureRefs(x.expression, allMeasures);
            selectParts.Add($"({expr}) AS \"{x.name}\"");
        }

        sb.Append("SELECT ").AppendJoin(", ", selectParts);
        sb.Append(" FROM ").Append(fromClause);

        // WHERE
        var whereParts = new List<string>();

        if (!string.IsNullOrWhiteSpace(timeDim) && timeRange is not null)
        {
            var timeDimMeta = dims.FirstOrDefault(d => d.name == timeDim);
            if (timeDimMeta is not null)
            {
                var col = $"\"{timeDimMeta.column_name}\"";
                if (!string.IsNullOrWhiteSpace(timeRange.From))
                {
                    whereParts.Add($"{col} >= @_timeFrom");
                    parameters["_timeFrom"] = DateTime.Parse(timeRange.From);
                }
                if (!string.IsNullOrWhiteSpace(timeRange.To))
                {
                    whereParts.Add($"{col} <= @_timeTo");
                    parameters["_timeTo"] = DateTime.Parse(timeRange.To);
                }
            }
        }

        if (filters is { Length: > 0 })
        {
            var i = 0;
            foreach (var f in filters)
            {
                var pName = $"_f{i++}";
                var col = $"\"{f.FieldName}\"";
                switch (f.Operator.ToLower())
                {
                    case "eq":   whereParts.Add($"{col} = @{pName}");  parameters[pName] = f.Value; break;
                    case "neq":  whereParts.Add($"{col} != @{pName}"); parameters[pName] = f.Value; break;
                    case "gt":   whereParts.Add($"{col} > @{pName}");  parameters[pName] = f.Value; break;
                    case "gte":  whereParts.Add($"{col} >= @{pName}"); parameters[pName] = f.Value; break;
                    case "lt":   whereParts.Add($"{col} < @{pName}");  parameters[pName] = f.Value; break;
                    case "lte":  whereParts.Add($"{col} <= @{pName}"); parameters[pName] = f.Value; break;
                    case "like": whereParts.Add($"{col} ILIKE @{pName}"); parameters[pName] = f.Value; break;
                    case "in":   if (f.Values is { Length: > 0 }) { whereParts.Add($"{col} = ANY(@{pName})"); parameters[pName] = f.Values; } break;
                    case "notnull": whereParts.Add($"{col} IS NOT NULL"); break;
                    case "isnull":  whereParts.Add($"{col} IS NULL"); break;
                }
            }
        }

        if (whereParts.Count > 0)
            sb.Append(" WHERE ").AppendJoin(" AND ", whereParts);

        // GROUP BY (only when measures/metrics are selected)
        if ((measures.Count > 0 || metrics.Count > 0) && dims.Count > 0)
        {
            var groupParts = dims.Select(d =>
            {
                var expr = !string.IsNullOrWhiteSpace(d.custom_sql_expression)
                    ? d.custom_sql_expression
                    : $"\"{d.column_name}\"";
                return d.is_time_dimension && !string.IsNullOrWhiteSpace(granularity)
                    ? ApplyGranularity(expr, granularity)
                    : expr;
            });
            sb.Append(" GROUP BY ").AppendJoin(", ", groupParts);
        }

        // ORDER BY
        if (sorts is { Length: > 0 })
        {
            var orderParts = sorts.Select(s =>
                $"\"{s.FieldName}\" {(s.Direction.ToLower() == "desc" ? "DESC" : "ASC")}");
            sb.Append(" ORDER BY ").AppendJoin(", ", orderParts);
        }

        // LIMIT
        sb.Append($" LIMIT {Math.Clamp(limit, 1, 50_000)}");

        return (sb.ToString(), parameters);
    }

    /// Replace bare measure names in a metric expression with their aggregate form.
    /// e.g. "profit" → "SUM(\"profit\")" when profit.aggregation_type = "sum"
    private static string ReplaceMeasureRefs(string expression, List<MeasureMeta> allMeasures)
    {
        var result = expression;
        // Sort longest name first to avoid partial replacements (e.g. "revenue_net" before "revenue")
        foreach (var m in allMeasures.OrderByDescending(m => m.name.Length))
        {
            var col = !string.IsNullOrWhiteSpace(m.custom_sql_expression)
                ? m.custom_sql_expression
                : $"\"{m.column_name}\"";
            var aggExpr = $"{m.aggregation_type.ToUpper()}({col})";
            // Word-boundary replacement: only replace whole word
            result = System.Text.RegularExpressions.Regex.Replace(
                result,
                $@"\b{System.Text.RegularExpressions.Regex.Escape(m.name)}\b",
                aggExpr);
        }
        return result;
    }

    private static string ApplyGranularity(string colExpr, string granularity) =>
        granularity.ToLower() switch
        {
            "day"     => $"DATE_TRUNC('day', {colExpr})",
            "week"    => $"DATE_TRUNC('week', {colExpr})",
            "month"   => $"DATE_TRUNC('month', {colExpr})",
            "quarter" => $"DATE_TRUNC('quarter', {colExpr})",
            "year"    => $"DATE_TRUNC('year', {colExpr})",
            _         => colExpr
        };

    // ── Execute SQL ───────────────────────────────────────────────────────────

    private static async Task<List<string>> ExecuteSqlAsync(
        NpgsqlConnection conn, string sql,
        Dictionary<string, object?> parameters,
        List<QueryColumnGql> columns,
        CancellationToken ct)
    {
        var dapperParams = new DynamicParameters();
        foreach (var (k, v) in parameters) dapperParams.Add(k, v);

        var rawRows = await conn.QueryAsync(
            new CommandDefinition(sql, dapperParams, cancellationToken: ct));

        return rawRows.Select(row =>
        {
            var dict = (IDictionary<string, object?>)row;
            var obj  = new Dictionary<string, object?>();
            foreach (var col in columns)
                obj[col.Name] = dict.TryGetValue(col.Name, out var val) ? val : null;
            return JsonSerializer.Serialize(obj);
        }).ToList();
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static QueryResultGql ErrorResult(string message, Stopwatch sw) =>
        new([], [], new QueryMetadataGql(
            null, sw.ElapsedMilliseconds, 0, false, null, DateTime.UtcNow, message));

    private static DatasetConfig ParseDatasetConfig(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            var r = doc.RootElement;
            return new DatasetConfig(
                SchemaName: r.TryGetProperty("schemaName", out var sn) ? sn.GetString() : null,
                TableName:  r.TryGetProperty("tableName",  out var tn) ? tn.GetString() : null,
                CustomSql:  r.TryGetProperty("customSql",  out var cs) ? cs.GetString() : null);
        }
        catch { return new DatasetConfig(null, null, null); }
    }

    private sealed record DatasetConfig(string? SchemaName, string? TableName, string? CustomSql);
}
