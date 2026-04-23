namespace SemanticEngine.Models;

/// <summary>
/// Metric — derived measure được tính từ expression SQL.
/// Ví dụ: ConversionRate = Orders / Visits, AvgOrderValue = Revenue / Orders
/// </summary>
public sealed class SemanticMetric
{
    public Guid Id { get; init; }
    public required string Name { get; init; }
    public required string DisplayName { get; init; }
    public string? Description { get; init; }

    /// <summary>
    /// SQL expression dùng tên measures làm placeholder.
    /// Ví dụ: "{{revenue}} / NULLIF({{visits}}, 0)"
    /// Sẽ được resolve thành SQL aggregate thực tế khi build query.
    /// </summary>
    public required string Expression { get; init; }

    public DataType DataType { get; init; } = DataType.Decimal;
    public string? Format { get; init; }

    /// <summary>Names của measures mà metric này phụ thuộc vào</summary>
    public IReadOnlyList<string> DependsOnMeasures { get; init; } = [];

    public int SortOrder { get; init; }

    /// <summary>
    /// Resolve expression bằng cách thay thế {{measure_name}} → SQL aggregate
    /// </summary>
    public string ResolveExpression(IDictionary<string, string> measureSqlMap)
    {
        var resolved = Expression;
        foreach (var (measureName, sqlExpr) in measureSqlMap)
        {
            resolved = resolved.Replace($"{{{{{measureName}}}}}", sqlExpr,
                StringComparison.OrdinalIgnoreCase);
        }
        return resolved;
    }
}
