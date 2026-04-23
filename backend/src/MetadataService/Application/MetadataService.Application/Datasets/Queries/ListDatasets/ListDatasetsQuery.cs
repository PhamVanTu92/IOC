using MediatR;
using MetadataService.Application.Datasets.DTOs;

namespace MetadataService.Application.Datasets.Queries.ListDatasets;

public sealed record ListDatasetsQuery(Guid TenantId, bool IncludeInactive = false)
    : IRequest<IReadOnlyList<DatasetDto>>;
