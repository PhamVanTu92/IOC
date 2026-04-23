using MediatR;
using Microsoft.Extensions.Logging;
using QueryService.Application.Interfaces;
using SemanticEngine.Builder;
using SemanticEngine.Models;
using System.Text.Json;

namespace QueryService.Application.ExecuteQuery;

/// <summary>
/// Handler thực thi dynamic query qua Semantic Layer.
///
/// Pipeline:
///   1. Load SemanticDataset từ metadata store
///   2. Build SQL từ QueryInput (SqlQueryBuilder)
///   3. Check Redis cache (nếu !ForceRefresh)
///   4. Execute SQL via Dapper
///   5. Map rows → QueryResult
///   6. Cache kết quả
/// </summary>
public sealed class ExecuteQueryCommandHandler
    : IRequestHandler<ExecuteQueryCommand, QueryResult>
{
    private readonly ISemanticDatasetLoader _loader;
    private readonly IQueryExecutor _executor;
    private readonly ICacheService _cache;
    private readonly ILogger<ExecuteQueryCommandHandler> _logger;

    // Cache TTL mặc định — 5 phút
    private static readonly TimeSpan _defaultCacheTtl = TimeSpan.FromMinutes(5);

    public ExecuteQueryCommandHandler(
        ISemanticDatasetLoader loader,
        IQueryExecutor executor,
        ICacheService cache,
        ILogger<ExecuteQueryCommandHandler> logger)
    {
        _loader   = loader;
        _executor = executor;
        _cache    = cache;
        _logger   = logger;
    }

    public async Task<QueryResult> Handle(
        ExecuteQueryCommand request,
        CancellationToken cancellationToken)
    {
        var input = request.Input;

        // ─── 1. Load SemanticDataset ───────────────────────────────────────────
        var dataset = await _loader.LoadAsync(input.DatasetId, input.TenantId, cancellationToken);
        if (dataset is null)
            throw new InvalidOperationException(
                $"Dataset '{input.DatasetId}' không tồn tại hoặc không thuộc tenant '{input.TenantId}'.");

        // ─── 2. Build SQL ──────────────────────────────────────────────────────
        SqlQueryResult built;
        try
        {
            built = SqlQueryBuilder.Build(input, dataset);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "SQL build failed for dataset {DatasetId}", input.DatasetId);
            return QueryResult.Empty(new QueryExecutionMetadata
            {
                ErrorMessage = ex.Message,
                ExecutedAt = DateTimeOffset.UtcNow
            });
        }

        _logger.LogDebug("Built SQL for dataset {DatasetId}:\n{Sql}", input.DatasetId, built.Sql);

        // ─── 3. Check Redis cache ──────────────────────────────────────────────
        var cacheKey = $"query:{input.ToCacheKey()}";

        if (!input.ForceRefresh)
        {
            var cached = await _cache.GetAsync(cacheKey, cancellationToken);
            if (cached is not null)
            {
                _logger.LogDebug("Cache HIT for key {CacheKey}", cacheKey);
                var cachedResult = DeserializeCachedResult(cached, built.Columns);

                return new QueryResult
                {
                    Columns = cachedResult.Columns,
                    Rows = cachedResult.Rows,
                    Metadata = new QueryExecutionMetadata
                    {
                        GeneratedSql = built.Sql,
                        ExecutionTimeMs = 0,
                        TotalRows = cachedResult.Rows.Count,
                        FromCache = true,
                        CacheKey = cacheKey,
                        ExecutedAt = DateTimeOffset.UtcNow
                    }
                };
            }
        }

        // ─── 4. Execute SQL ────────────────────────────────────────────────────
        QueryExecutionResult execResult;
        try
        {
            execResult = await _executor.ExecuteAsync(built, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Query execution failed for dataset {DatasetId}: {Sql}",
                input.DatasetId, built.Sql);

            return QueryResult.Empty(new QueryExecutionMetadata
            {
                GeneratedSql = built.Sql,
                Parameters = built.Parameters,
                ErrorMessage = ex.Message,
                ExecutedAt = DateTimeOffset.UtcNow
            });
        }

        _logger.LogInformation(
            "Query executed: dataset={DatasetId} rows={Rows} duration={Ms}ms",
            input.DatasetId, execResult.Rows.Count, execResult.ExecutionTimeMs);

        // ─── 5. Map columns theo thứ tự SELECT ────────────────────────────────
        var resultColumns = built.Columns
            .Select(c => new QueryResultColumn
            {
                Name = c.Name,
                DisplayName = c.DisplayName,
                DataType = c.DataType,
                Format = c.Format,
                FieldType = c.FieldType
            })
            .ToList();

        // ─── 6. Cache kết quả ─────────────────────────────────────────────────
        if (execResult.Rows.Count > 0)
        {
            try
            {
                var toCache = SerializeForCache(execResult.Rows);
                await _cache.SetAsync(cacheKey, toCache, _defaultCacheTtl, cancellationToken);
            }
            catch (Exception ex)
            {
                // Cache failure không nên block response
                _logger.LogWarning(ex, "Cache write failed for key {CacheKey}", cacheKey);
            }
        }

        return new QueryResult
        {
            Columns = resultColumns,
            Rows = execResult.Rows,
            Metadata = new QueryExecutionMetadata
            {
                GeneratedSql = built.Sql,
                Parameters = built.Parameters,
                ExecutionTimeMs = execResult.ExecutionTimeMs,
                TotalRows = execResult.TotalRows,
                FromCache = false,
                CacheKey = cacheKey,
                ExecutedAt = DateTimeOffset.UtcNow
            }
        };
    }

    // ─── Cache serialization helpers ──────────────────────────────────────────

    private static string SerializeForCache(IReadOnlyList<IReadOnlyDictionary<string, object?>> rows)
    {
        // Serialize rows thành JSON array of objects
        var serializableRows = rows
            .Select(row => row.ToDictionary(
                kv => kv.Key,
                kv => SerializeValue(kv.Value)))
            .ToList();

        return JsonSerializer.Serialize(serializableRows);
    }

    private static object? SerializeValue(object? value) => value switch
    {
        DateTimeOffset dto => dto.ToString("O"),
        DateTime dt => dt.ToString("O"),
        _ => value
    };

    private static (IReadOnlyList<QueryResultColumn> Columns, IReadOnlyList<IReadOnlyDictionary<string, object?>> Rows)
        DeserializeCachedResult(string json, IReadOnlyList<ColumnDescriptor> columns)
    {
        var resultColumns = columns
            .Select(c => new QueryResultColumn
            {
                Name = c.Name,
                DisplayName = c.DisplayName,
                DataType = c.DataType,
                Format = c.Format,
                FieldType = c.FieldType
            })
            .ToList();

        var rawRows = JsonSerializer.Deserialize<List<Dictionary<string, JsonElement>>>(json)
            ?? [];

        var rows = rawRows
            .Select(row => (IReadOnlyDictionary<string, object?>)row.ToDictionary(
                kv => kv.Key,
                kv => (object?)JsonElementToObject(kv.Value)))
            .ToList();

        return (resultColumns, rows);
    }

    private static object? JsonElementToObject(JsonElement element) => element.ValueKind switch
    {
        JsonValueKind.Number when element.TryGetInt64(out var l) => l,
        JsonValueKind.Number when element.TryGetDouble(out var d) => d,
        JsonValueKind.String => element.GetString(),
        JsonValueKind.True => true,
        JsonValueKind.False => false,
        JsonValueKind.Null => null,
        _ => element.GetRawText()
    };
}
