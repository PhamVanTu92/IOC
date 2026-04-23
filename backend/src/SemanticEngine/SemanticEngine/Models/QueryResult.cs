namespace SemanticEngine.Models;

/// <summary>
/// Kết quả trả về sau khi thực thi một QueryInput.
/// </summary>
public sealed class QueryResult
{
    public required IReadOnlyList<QueryResultColumn> Columns { get; init; }
    public required IReadOnlyList<IReadOnlyDictionary<string, object?>> Rows { get; init; }
    public required QueryExecutionMetadata Metadata { get; init; }

    public static QueryResult Empty(QueryExecutionMetadata metadata) => new()
    {
        Columns = [],
        Rows = [],
        Metadata = metadata
    };
}

/// <summary>Thông tin về một cột trong kết quả query</summary>
public sealed class QueryResultColumn
{
    public required string Name { get; init; }         // tên kỹ thuật
    public required string DisplayName { get; init; }  // tên hiển thị
    public required string DataType { get; init; }     // "string", "number", "date", ...
    public string? Format { get; init; }
    public required string FieldType { get; init; }    // "dimension" | "measure" | "metric"
}

/// <summary>Metadata về execution — phục vụ debugging và monitoring</summary>
public sealed class QueryExecutionMetadata
{
    public string? GeneratedSql { get; init; }
    public IReadOnlyDictionary<string, object?>? Parameters { get; init; }
    public long ExecutionTimeMs { get; init; }
    public int TotalRows { get; init; }
    public bool FromCache { get; init; }
    public string? CacheKey { get; init; }
    public DateTimeOffset ExecutedAt { get; init; } = DateTimeOffset.UtcNow;
    public string? ErrorMessage { get; init; }
}
