using MediatR;
using MetadataService.Application.Datasets.DTOs;

namespace MetadataService.Application.Datasets.Commands.CreateDataset;

public sealed record CreateDatasetCommand(
    Guid TenantId,
    Guid CreatedBy,
    string Name,
    string SourceType,
    string? Description = null,
    string? SchemaName = null,
    string? TableName = null,
    string? CustomSql = null
) : IRequest<DatasetDto>;
