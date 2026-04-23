using MediatR;
using MetadataService.Application.Datasets.DTOs;

namespace MetadataService.Application.Datasets.Queries.GetDataset;

public sealed record GetDatasetQuery(Guid DatasetId, Guid TenantId) : IRequest<DatasetDto>;
