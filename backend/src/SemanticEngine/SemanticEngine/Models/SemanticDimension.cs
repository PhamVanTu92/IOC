namespace SemanticEngine.Models;

/// <summary>
/// Dimension — trường có thể dùng để group/slice dữ liệu.
/// Ví dụ: Country, Category, Date, ProductName
/// </summary>
public sealed class SemanticDimension
{
    public Guid Id { get; init; }
    public required string Name { get; init; }          // định danh kỹ thuật (snake_case)
    public required string DisplayName { get; init; }   // tên hiển thị cho UI
    public string? Description { get; init; }
    public required string ColumnName { get; init; }    // tên cột thực trong DB
    public DataType DataType { get; init; } = DataType.String;
    public string? Format { get; init; }                // "yyyy-MM-dd", "#,##0", ...
    public bool IsTimeDimension { get; init; }
    public TimeGranularity? DefaultGranularity { get; init; }
    public int SortOrder { get; init; }

    /// <summary>Tạo SQL expression cho dimension này (có thể override)</summary>
    public string? CustomSqlExpression { get; init; }

    /// <summary>Trả về SQL expression thực — custom hoặc column name</summary>
    public string GetSqlExpression() =>
        CustomSqlExpression ?? $"\"{ColumnName}\"";

    /// <summary>Tạo SQL expression cho time truncation theo granularity</summary>
    public string GetTimeTruncExpression(TimeGranularity granularity) =>
        granularity switch
        {
            TimeGranularity.Hour    => $"DATE_TRUNC('hour', {GetSqlExpression()})",
            TimeGranularity.Day     => $"DATE_TRUNC('day', {GetSqlExpression()})",
            TimeGranularity.Week    => $"DATE_TRUNC('week', {GetSqlExpression()})",
            TimeGranularity.Month   => $"DATE_TRUNC('month', {GetSqlExpression()})",
            TimeGranularity.Quarter => $"DATE_TRUNC('quarter', {GetSqlExpression()})",
            TimeGranularity.Year    => $"DATE_TRUNC('year', {GetSqlExpression()})",
            _ => GetSqlExpression()
        };
}
