using MediatR;
using MetadataService.Application.Datasets.DTOs;
using MetadataService.Domain.Interfaces;

namespace MetadataService.Application.Datasets.Queries.ListDatasets;

public sealed class ListDatasetsQueryHandler
    : IRequestHandler<ListDatasetsQuery, IReadOnlyList<DatasetDto>>
{
    private readonly IDatasetRepository _datasetRepository;

    public ListDatasetsQueryHandler(IDatasetRepository datasetRepository)
    {
        _datasetRepository = datasetRepository;
    }

    public async Task<IReadOnlyList<DatasetDto>> Handle(
        ListDatasetsQuery request,
        CancellationToken cancellationToken)
    {
        var datasets = await _datasetRepository.ListAsync(
            request.TenantId,
            request.IncludeInactive,
            cancellationToken);

        // List trả về Dataset không kèm children — đủ cho UI list view
        return datasets.Select(d => new DatasetDto(
            Id: d.Id,
            TenantId: d.TenantId,
            Name: d.Name,
            Description: d.Description,
            SourceType: d.SourceType,
            SchemaName: d.SchemaName,
            TableName: d.TableName,
            CustomSql: d.CustomSql,
            IsActive: d.IsActive,
            CreatedAt: d.CreatedAt,
            UpdatedAt: d.UpdatedAt,
            Dimensions: [],
            Measures: [],
            Metrics: []
        )).ToList();
    }
}
