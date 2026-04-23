namespace SemanticEngine.Builder;

/// <summary>
/// Kết quả từ SqlQueryBuilder — SQL đã được build cùng các tham số.
/// Sử dụng parameterized query để chống SQL injection.
/// </summary>
public sealed class SqlQueryResult
{
    public required string Sql { get; init; }

    /// <summary>
    /// Count SQL — dùng để lấy tổng số rows (không có LIMIT/OFFSET).
    /// Null nếu không cần đếm.
    /// </summary>
    public string? CountSql { get; init; }

    /// <summary>Parameters — key là @paramName, value là giá trị thực</summary>
    public IReadOnlyDictionary<string, object?> Parameters { get; init; }
        = new Dictionary<string, object?>();

    /// <summary>Danh sách columns theo thứ tự trong SELECT</summary>
    public IReadOnlyList<ColumnDescriptor> Columns { get; init; } = [];
}

/// <summary>Mô tả một cột trong SELECT clause</summary>
public sealed class ColumnDescriptor
{
    public required string Name { get; init; }           // alias trong SQL
    public required string DisplayName { get; init; }    // tên hiển thị
    public required string DataType { get; init; }       // "string" | "number" | "date" ...
    public string? Format { get; init; }
    public required string FieldType { get; init; }      // "dimension" | "measure" | "metric"
}
