using MediatR;
using MetadataService.Application.Datasets.DTOs;

namespace MetadataService.Application.Measures.Commands.CreateMeasure;

public sealed record CreateMeasureCommand(
    Guid DatasetId,
    Guid TenantId,
    string Name,
    string DisplayName,
    string ColumnName,
    string AggregationType,
    string? Description = null,
    string DataType = "decimal",
    string? Format = null,
    string? FilterExpression = null,
    string? CustomSqlExpression = null,
    int SortOrder = 0
) : IRequest<MeasureDto>;
