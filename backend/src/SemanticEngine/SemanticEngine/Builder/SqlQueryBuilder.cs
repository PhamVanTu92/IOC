using System.Text;
using SemanticEngine.Models;

namespace SemanticEngine.Builder;

/// <summary>
/// SqlQueryBuilder — dịch QueryInput + SemanticDataset thành parameterized PostgreSQL.
///
/// Output format:
///   SELECT {dimensions}, {measures}, {metrics}
///   FROM   {dataset_source}
///   WHERE  {tenant_filter} AND {time_filter} AND {custom_filters}
///   GROUP  BY {dimensions}
///   HAVING {measure_filters}
///   ORDER  BY {sorts}
///   LIMIT  {limit} OFFSET {offset}
/// </summary>
public static class SqlQueryBuilder
{
    public static SqlQueryResult Build(QueryInput input, SemanticDataset dataset)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(dataset);

        var errors = input.Validate();
        if (errors.Count > 0)
            throw new InvalidOperationException(
                $"QueryInput không hợp lệ: {string.Join("; ", errors)}");

        var params_ = new Dictionary<string, object?>();
        var columns = new List<ColumnDescriptor>();
        int paramCounter = 0;
        string NextParam(string prefix = "p") => $"@{prefix}{++paramCounter}";

        // ─── 1. Resolve semantic fields ───────────────────────────────────────

        // Map measure name → SQL aggregate (dùng để resolve metric expressions)
        var measureSqlMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        // ─── 2. SELECT clause ─────────────────────────────────────────────────

        var selectParts = new List<string>();
        var groupByParts = new List<string>();

        // 2a. Dimensions
        SemanticDimension? timeDim = null;
        TimeGranularity? granularity = input.Granularity;

        foreach (var dimName in input.Dimensions)
        {
            var dim = dataset.GetDimension(dimName)
                ?? throw new InvalidOperationException(
                    $"Dimension '{dimName}' không tồn tại trong dataset '{dataset.Name}'.");

            string sqlExpr;
            if (dim.IsTimeDimension && granularity.HasValue)
            {
                sqlExpr = dim.GetTimeTruncExpression(granularity.Value);
                timeDim = dim;
            }
            else
            {
                sqlExpr = dim.GetSqlExpression();
            }

            var alias = QuoteIdentifier(dimName);
            selectParts.Add($"{sqlExpr} AS {alias}");
            groupByParts.Add(sqlExpr);

            columns.Add(new ColumnDescriptor
            {
                Name = dimName,
                DisplayName = dim.DisplayName,
                DataType = dim.DataType.ToString().ToLowerInvariant(),
                Format = dim.Format,
                FieldType = "dimension"
            });
        }

        // 2b. Measures
        foreach (var measureName in input.Measures)
        {
            var measure = dataset.GetMeasure(measureName)
                ?? throw new InvalidOperationException(
                    $"Measure '{measureName}' không tồn tại trong dataset '{dataset.Name}'.");

            var aggExpr = measure.GetAggregateExpression();
            var alias = QuoteIdentifier(measureName);
            selectParts.Add($"{aggExpr} AS {alias}");
            measureSqlMap[measureName] = aggExpr;

            columns.Add(new ColumnDescriptor
            {
                Name = measureName,
                DisplayName = measure.DisplayName,
                DataType = measure.DataType.ToString().ToLowerInvariant(),
                Format = measure.Format,
                FieldType = "measure"
            });
        }

        // 2c. Metrics — resolve {{placeholder}} → aggregate SQL
        foreach (var metricName in input.Metrics)
        {
            var metric = dataset.GetMetric(metricName)
                ?? throw new InvalidOperationException(
                    $"Metric '{metricName}' không tồn tại trong dataset '{dataset.Name}'.");

            // Đảm bảo các measures mà metric phụ thuộc đã được resolve
            foreach (var depName in metric.DependsOnMeasures)
            {
                if (!measureSqlMap.ContainsKey(depName))
                {
                    var depMeasure = dataset.GetMeasure(depName)
                        ?? throw new InvalidOperationException(
                            $"Measure '{depName}' mà metric '{metricName}' phụ thuộc không tồn tại.");
                    measureSqlMap[depName] = depMeasure.GetAggregateExpression();
                }
            }

            var resolvedExpr = metric.ResolveExpression(measureSqlMap);

            // Kiểm tra còn placeholder chưa resolve
            if (resolvedExpr.Contains("{{"))
                throw new InvalidOperationException(
                    $"Metric '{metricName}' còn placeholder chưa được resolve: {resolvedExpr}");

            var alias = QuoteIdentifier(metricName);
            selectParts.Add($"{resolvedExpr} AS {alias}");

            columns.Add(new ColumnDescriptor
            {
                Name = metricName,
                DisplayName = metric.DisplayName,
                DataType = metric.DataType.ToString().ToLowerInvariant(),
                Format = metric.Format,
                FieldType = "metric"
            });
        }

        // ─── 3. FROM clause ───────────────────────────────────────────────────

        var fromExpr = dataset.GetFromExpression();

        // ─── 4. WHERE clause ──────────────────────────────────────────────────

        var whereParts = new List<string>();
        var havingParts = new List<string>();

        // 4a. Tenant isolation — LUÔN phải có
        params_["@tenantId"] = input.TenantId;
        whereParts.Add("tenant_id = @tenantId");

        // 4b. Time dimension filter
        if (input.TimeDimensionName is not null && input.TimeRange is not null)
        {
            var td = dataset.GetDimension(input.TimeDimensionName)
                ?? throw new InvalidOperationException(
                    $"Time dimension '{input.TimeDimensionName}' không tồn tại.");

            var (from, to) = input.TimeRange.Resolve();
            var pFrom = NextParam("tFrom");
            var pTo = NextParam("tTo");
            params_[pFrom] = from;
            params_[pTo] = to;
            whereParts.Add($"{td.GetSqlExpression()} >= {pFrom} AND {td.GetSqlExpression()} <= {pTo}");
        }

        // 4c. Custom filters
        foreach (var filter in input.Filters)
        {
            var (clause, isHaving) = BuildFilterClause(
                filter, dataset, measureSqlMap, params_, NextParam);

            if (clause is null) continue;

            if (isHaving)
                havingParts.Add(clause);
            else
                whereParts.Add(clause);
        }

        // ─── 5. Assemble main SQL ─────────────────────────────────────────────

        var sql = new StringBuilder();
        sql.Append("SELECT ");
        sql.AppendLine(string.Join(", ", selectParts));
        sql.Append("FROM ").AppendLine(fromExpr);

        if (whereParts.Count > 0)
        {
            sql.Append("WHERE ");
            sql.AppendLine(string.Join("\n  AND ", whereParts));
        }

        if (groupByParts.Count > 0)
        {
            sql.Append("GROUP BY ");
            sql.AppendLine(string.Join(", ", groupByParts));
        }

        if (havingParts.Count > 0)
        {
            sql.Append("HAVING ");
            sql.AppendLine(string.Join("\n  AND ", havingParts));
        }

        // ─── 6. ORDER BY ──────────────────────────────────────────────────────

        if (input.Sorts.Count > 0)
        {
            var orderParts = new List<string>();
            foreach (var sort in input.Sorts)
            {
                // Sắp xếp theo alias trong SELECT
                var dir = sort.Direction == SortDirection.Descending ? "DESC" : "ASC";
                orderParts.Add($"{QuoteIdentifier(sort.FieldName)} {dir}");
            }
            sql.Append("ORDER BY ").AppendLine(string.Join(", ", orderParts));
        }

        // ─── 7. LIMIT / OFFSET ────────────────────────────────────────────────

        var limit = Math.Clamp(input.Limit, 1, 100_000);
        sql.Append($"LIMIT {limit} OFFSET {input.Offset}");

        // ─── 8. Count SQL (không có LIMIT/OFFSET, dùng COUNT(*)) ─────────────

        string? countSql = null;
        if (groupByParts.Count > 0)
        {
            // Khi có GROUP BY: đếm số nhóm = SELECT COUNT(*) FROM (subquery) __count
            var innerSql = new StringBuilder();
            innerSql.Append("SELECT 1 FROM ").AppendLine(fromExpr);
            if (whereParts.Count > 0)
            {
                innerSql.Append("WHERE ");
                innerSql.AppendLine(string.Join("\n  AND ", whereParts));
            }
            innerSql.Append("GROUP BY ").Append(string.Join(", ", groupByParts));
            countSql = $"SELECT COUNT(*) FROM ({innerSql}) AS __count";
        }
        else
        {
            var countBuilder = new StringBuilder();
            countBuilder.Append("SELECT COUNT(*) FROM ").AppendLine(fromExpr);
            if (whereParts.Count > 0)
            {
                countBuilder.Append("WHERE ");
                countBuilder.Append(string.Join("\n  AND ", whereParts));
            }
            countSql = countBuilder.ToString();
        }

        return new SqlQueryResult
        {
            Sql = sql.ToString(),
            CountSql = countSql,
            Parameters = params_,
            Columns = columns
        };
    }

    // ─── Filter clause builder ─────────────────────────────────────────────────

    private static (string? clause, bool isHaving) BuildFilterClause(
        QueryFilter filter,
        SemanticDataset dataset,
        Dictionary<string, string> measureSqlMap,
        Dictionary<string, object?> params_,
        Func<string, string> nextParam)
    {
        // Xác định field SQL expression
        string fieldExpr;
        bool isMeasure = false;

        var dim = dataset.GetDimension(filter.FieldName);
        if (dim is not null)
        {
            fieldExpr = dim.GetSqlExpression();
        }
        else if (measureSqlMap.TryGetValue(filter.FieldName, out var mExpr))
        {
            fieldExpr = mExpr;
            isMeasure = true; // measure filters → HAVING
        }
        else
        {
            // Field không tồn tại → bỏ qua (không throw để không break query)
            return (null, false);
        }

        string clause = filter.Operator switch
        {
            FilterOperator.Equals => BuildSimple(fieldExpr, "=", filter.Value, params_, nextParam),
            FilterOperator.NotEquals => BuildSimple(fieldExpr, "<>", filter.Value, params_, nextParam),
            FilterOperator.GreaterThan => BuildSimple(fieldExpr, ">", filter.Value, params_, nextParam),
            FilterOperator.GreaterThanOrEquals => BuildSimple(fieldExpr, ">=", filter.Value, params_, nextParam),
            FilterOperator.LessThan => BuildSimple(fieldExpr, "<", filter.Value, params_, nextParam),
            FilterOperator.LessThanOrEquals => BuildSimple(fieldExpr, "<=", filter.Value, params_, nextParam),

            FilterOperator.In => BuildIn(fieldExpr, filter.Values, params_, nextParam),
            FilterOperator.NotIn => $"NOT ({BuildIn(fieldExpr, filter.Values, params_, nextParam)})",

            FilterOperator.Contains => BuildLike(fieldExpr, filter.Value, "ILIKE", "%{0}%", params_, nextParam),
            FilterOperator.NotContains => $"NOT ({BuildLike(fieldExpr, filter.Value, "ILIKE", "%{0}%", params_, nextParam)})",
            FilterOperator.StartsWith => BuildLike(fieldExpr, filter.Value, "ILIKE", "{0}%", params_, nextParam),
            FilterOperator.EndsWith => BuildLike(fieldExpr, filter.Value, "ILIKE", "%{0}", params_, nextParam),

            FilterOperator.Between => BuildBetween(fieldExpr, filter.ValueFrom, filter.ValueTo, params_, nextParam),

            FilterOperator.IsNull => $"{fieldExpr} IS NULL",
            FilterOperator.IsNotNull => $"{fieldExpr} IS NOT NULL",

            _ => throw new InvalidOperationException($"Unsupported filter operator: {filter.Operator}")
        };

        return (clause, isMeasure);
    }

    private static string BuildSimple(string fieldExpr, string op, object? value,
        Dictionary<string, object?> params_, Func<string, string> nextParam)
    {
        var p = nextParam("p");
        params_[p] = value;
        return $"{fieldExpr} {op} {p}";
    }

    private static string BuildIn(string fieldExpr, IReadOnlyList<object>? values,
        Dictionary<string, object?> params_, Func<string, string> nextParam)
    {
        if (values is null || values.Count == 0)
            return "FALSE"; // IN (empty) → always false

        // PostgreSQL: field = ANY(@array)  — Dapper supports arrays natively với Npgsql
        var p = nextParam("arr");
        params_[p] = values.ToArray();
        return $"{fieldExpr} = ANY({p})";
    }

    private static string BuildLike(string fieldExpr, object? value, string likeOp,
        string pattern, Dictionary<string, object?> params_, Func<string, string> nextParam)
    {
        var p = nextParam("p");
        params_[p] = string.Format(pattern, value?.ToString() ?? "");
        return $"CAST({fieldExpr} AS TEXT) {likeOp} {p}";
    }

    private static string BuildBetween(string fieldExpr, object? from, object? to,
        Dictionary<string, object?> params_, Func<string, string> nextParam)
    {
        var pFrom = nextParam("from");
        var pTo = nextParam("to");
        params_[pFrom] = from;
        params_[pTo] = to;
        return $"{fieldExpr} BETWEEN {pFrom} AND {pTo}";
    }

    private static string QuoteIdentifier(string name) => $"\"{name}\"";
}
