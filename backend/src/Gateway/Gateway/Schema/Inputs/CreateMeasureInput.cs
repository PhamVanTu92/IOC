namespace Gateway.Schema.Inputs;

/// <summary>
/// GraphQL Input type để tạo Measure mới trong một Dataset.
/// </summary>
public sealed record CreateMeasureInput(
    Guid DatasetId,
    string Name,
    string DisplayName,
    string ColumnName,
    string AggregationType,
    string? Description = null,
    string DataType = "decimal",
    string? Format = null,
    string? FilterExpression = null,
    string? CustomSqlExpression = null,
    int SortOrder = 0
);
