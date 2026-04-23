using MediatR;
using MetadataService.Application.Datasets.DTOs;

namespace MetadataService.Application.Metrics.Commands.CreateMetric;

public sealed record CreateMetricCommand(
    Guid DatasetId,
    Guid TenantId,
    string Name,
    string DisplayName,
    string Expression,
    string[]? DependsOnMeasures = null,
    string? Description = null,
    string DataType = "decimal",
    string? Format = null,
    int SortOrder = 0
) : IRequest<MetricDto>;
