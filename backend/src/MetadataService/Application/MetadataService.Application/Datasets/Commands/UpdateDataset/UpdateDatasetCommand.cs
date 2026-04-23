using MediatR;
using MetadataService.Application.Datasets.DTOs;

namespace MetadataService.Application.Datasets.Commands.UpdateDataset;

public sealed record UpdateDatasetCommand(
    Guid Id,
    Guid TenantId,
    string Name,
    string? Description,
    string? SchemaName,
    string? TableName,
    string? CustomSql
) : IRequest<DatasetDto>;
