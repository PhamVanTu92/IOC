namespace Gateway.Schema.Inputs;

/// <summary>
/// GraphQL Input type để tạo Metric mới trong một Dataset.
/// </summary>
public sealed record CreateMetricInput(
    Guid DatasetId,
    string Name,
    string DisplayName,
    string Expression,
    string[]? DependsOnMeasures = null,
    string? Description = null,
    string DataType = "decimal",
    string? Format = null,
    int SortOrder = 0
);
