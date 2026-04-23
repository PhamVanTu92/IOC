namespace MetadataService.Application.Datasets.DTOs;

public sealed record DatasetDto(
    Guid Id,
    Guid TenantId,
    string Name,
    string? Description,
    string SourceType,
    string? SchemaName,
    string? TableName,
    string? CustomSql,
    bool IsActive,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    IReadOnlyList<DimensionDto> Dimensions,
    IReadOnlyList<MeasureDto> Measures,
    IReadOnlyList<MetricDto> Metrics
);
