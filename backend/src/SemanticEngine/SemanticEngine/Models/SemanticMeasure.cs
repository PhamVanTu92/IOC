namespace SemanticEngine.Models;

/// <summary>
/// Measure — trường có thể aggregate (SUM, AVG, COUNT...).
/// Ví dụ: Revenue, OrderCount, Quantity
/// </summary>
public sealed class SemanticMeasure
{
    public Guid Id { get; init; }
    public required string Name { get; init; }
    public required string DisplayName { get; init; }
    public string? Description { get; init; }
    public required string ColumnName { get; init; }
    public AggregationType AggregationType { get; init; } = AggregationType.Sum;
    public DataType DataType { get; init; } = DataType.Decimal;
    public string? Format { get; init; }                    // "#,##0.00", "$#,##0", "0.0%"
    public string? FilterExpression { get; init; }          // WHERE clause chỉ cho measure này
    public string? CustomSqlExpression { get; init; }       // ghi đè hoàn toàn column expression
    public int SortOrder { get; init; }

    /// <summary>
    /// Tạo SQL aggregate expression đầy đủ.
    /// Ví dụ: SUM("revenue"), COUNT(DISTINCT "user_id")
    /// </summary>
    public string GetAggregateExpression()
    {
        var colExpr = CustomSqlExpression ?? $"\"{ColumnName}\"";

        // Wrap với filter nếu có (PostgreSQL FILTER clause)
        var filterClause = FilterExpression is not null
            ? $" FILTER (WHERE {FilterExpression})"
            : string.Empty;

        return AggregationType switch
        {
            AggregationType.Sum           => $"SUM({colExpr}){filterClause}",
            AggregationType.Average       => $"AVG({colExpr}){filterClause}",
            AggregationType.Count         => $"COUNT({colExpr}){filterClause}",
            AggregationType.CountDistinct => $"COUNT(DISTINCT {colExpr}){filterClause}",
            AggregationType.Min           => $"MIN({colExpr}){filterClause}",
            AggregationType.Max           => $"MAX({colExpr}){filterClause}",
            _ => $"SUM({colExpr}){filterClause}"
        };
    }
}
