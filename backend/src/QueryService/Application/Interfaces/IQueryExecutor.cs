using SemanticEngine.Builder;

namespace QueryService.Application.Interfaces;

/// <summary>
/// Port — thực thi parameterized SQL và trả về raw rows.
/// Implemented bởi QueryService.Infrastructure (Dapper + Npgsql).
/// </summary>
public interface IQueryExecutor
{
    /// <summary>
    /// Thực thi SQL query, trả về rows và total count.
    /// </summary>
    /// <param name="query">SQL đã được build bởi SqlQueryBuilder</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>
    /// Rows: list của dictionaries (column_alias → value)
    /// TotalRows: tổng số rows không giới hạn bởi LIMIT (dùng cho pagination)
    /// </returns>
    Task<QueryExecutionResult> ExecuteAsync(SqlQueryResult query, CancellationToken ct = default);
}

/// <summary>
/// Kết quả raw từ database execution — trước khi map sang QueryResult.
/// </summary>
public sealed class QueryExecutionResult
{
    public required IReadOnlyList<IReadOnlyDictionary<string, object?>> Rows { get; init; }
    public required int TotalRows { get; init; }
    public required long ExecutionTimeMs { get; init; }
}
