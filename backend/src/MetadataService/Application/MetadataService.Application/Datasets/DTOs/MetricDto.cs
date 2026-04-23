namespace MetadataService.Application.Datasets.DTOs;

public sealed record MetricDto(
    Guid Id,
    Guid DatasetId,
    string Name,
    string DisplayName,
    string? Description,
    string Expression,
    string DataType,
    string? Format,
    string[] DependsOnMeasures,
    int SortOrder,
    bool IsActive
);
