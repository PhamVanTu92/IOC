namespace Gateway.Schema.Inputs;

/// <summary>
/// GraphQL Input type để tạo Dimension mới trong một Dataset.
/// </summary>
public sealed record CreateDimensionInput(
    Guid DatasetId,
    string Name,
    string DisplayName,
    string ColumnName,
    string DataType,
    bool IsTimeDimension = false,
    string? Description = null,
    string? Format = null,
    string? DefaultGranularity = null,
    string? CustomSqlExpression = null,
    int SortOrder = 0
);
