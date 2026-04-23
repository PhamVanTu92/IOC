namespace MetadataService.Application.Datasets.DTOs;

public sealed record DimensionDto(
    Guid Id,
    Guid DatasetId,
    string Name,
    string DisplayName,
    string? Description,
    string ColumnName,
    string? CustomSqlExpression,
    string DataType,
    string? Format,
    bool IsTimeDimension,
    string? DefaultGranularity,
    int SortOrder,
    bool IsActive
);
