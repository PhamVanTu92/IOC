namespace Gateway.Schema.Inputs;

/// <summary>
/// GraphQL Input type để tạo Dataset mới.
/// </summary>
public sealed record CreateDatasetInput(
    string Name,
    string SourceType,
    string? Description = null,
    string? SchemaName = null,
    string? TableName = null,
    string? CustomSql = null
);
