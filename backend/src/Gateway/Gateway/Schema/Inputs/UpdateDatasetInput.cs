namespace Gateway.Schema.Inputs;

/// <summary>
/// GraphQL Input type để cập nhật Dataset.
/// </summary>
public sealed record UpdateDatasetInput(
    string Name,
    string? Description = null,
    string? SchemaName = null,
    string? TableName = null,
    string? CustomSql = null
);
