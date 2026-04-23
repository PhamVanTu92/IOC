namespace MetadataService.Application.Datasets.DTOs;

public sealed record MeasureDto(
    Guid Id,
    Guid DatasetId,
    string Name,
    string DisplayName,
    string? Description,
    string ColumnName,
    string? CustomSqlExpression,
    string AggregationType,
    string DataType,
    string? Format,
    string? FilterExpression,
    int SortOrder,
    bool IsActive
);
