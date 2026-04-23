using MediatR;
using MetadataService.Application.Datasets.DTOs;
using MetadataService.Domain.Exceptions;
using MetadataService.Domain.Interfaces;

namespace MetadataService.Application.Datasets.Queries.GetDataset;

public sealed class GetDatasetQueryHandler : IRequestHandler<GetDatasetQuery, DatasetDto>
{
    private readonly IDatasetRepository _datasetRepository;
    private readonly IDimensionRepository _dimensionRepository;
    private readonly IMeasureRepository _measureRepository;
    private readonly IMetricRepository _metricRepository;

    public GetDatasetQueryHandler(
        IDatasetRepository datasetRepository,
        IDimensionRepository dimensionRepository,
        IMeasureRepository measureRepository,
        IMetricRepository metricRepository)
    {
        _datasetRepository = datasetRepository;
        _dimensionRepository = dimensionRepository;
        _measureRepository = measureRepository;
        _metricRepository = metricRepository;
    }

    public async Task<DatasetDto> Handle(GetDatasetQuery request, CancellationToken cancellationToken)
    {
        var dataset = await _datasetRepository.GetByIdAsync(request.DatasetId, request.TenantId, cancellationToken)
            ?? throw new DatasetNotFoundException(request.DatasetId, request.TenantId);

        var dimensions = await _dimensionRepository.ListByDatasetAsync(dataset.Id, request.TenantId, cancellationToken);
        var measures   = await _measureRepository.ListByDatasetAsync(dataset.Id, request.TenantId, cancellationToken);
        var metrics    = await _metricRepository.ListByDatasetAsync(dataset.Id, request.TenantId, cancellationToken);

        return new DatasetDto(
            Id: dataset.Id,
            TenantId: dataset.TenantId,
            Name: dataset.Name,
            Description: dataset.Description,
            SourceType: dataset.SourceType,
            SchemaName: dataset.SchemaName,
            TableName: dataset.TableName,
            CustomSql: dataset.CustomSql,
            IsActive: dataset.IsActive,
            CreatedAt: dataset.CreatedAt,
            UpdatedAt: dataset.UpdatedAt,
            Dimensions: dimensions.Select(d => new DimensionDto(
                d.Id, d.DatasetId, d.Name, d.DisplayName, d.Description,
                d.ColumnName, d.CustomSqlExpression, d.DataType, d.Format,
                d.IsTimeDimension, d.DefaultGranularity, d.SortOrder, d.IsActive)).ToList(),
            Measures: measures.Select(m => new MeasureDto(
                m.Id, m.DatasetId, m.Name, m.DisplayName, m.Description,
                m.ColumnName, m.CustomSqlExpression, m.AggregationType, m.DataType,
                m.Format, m.FilterExpression, m.SortOrder, m.IsActive)).ToList(),
            Metrics: metrics.Select(m => new MetricDto(
                m.Id, m.DatasetId, m.Name, m.DisplayName, m.Description,
                m.Expression, m.DataType, m.Format, m.DependsOnMeasures, m.SortOrder, m.IsActive)).ToList()
        );
    }
}
