using MediatR;
using MetadataService.Application.Datasets.DTOs;

namespace MetadataService.Application.Dimensions.Commands.CreateDimension;

public sealed record CreateDimensionCommand(
    Guid DatasetId,
    Guid TenantId,
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
) : IRequest<DimensionDto>;
