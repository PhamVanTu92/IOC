using MediatR;
using MetadataService.Application.Datasets.DTOs;
using MetadataService.Domain.Exceptions;
using MetadataService.Domain.Interfaces;

namespace MetadataService.Application.Datasets.Commands.UpdateDataset;

public sealed class UpdateDatasetCommandHandler
    : IRequestHandler<UpdateDatasetCommand, DatasetDto>
{
    private readonly IDatasetRepository _datasetRepository;
    private readonly IDimensionRepository _dimensionRepository;
    private readonly IMeasureRepository _measureRepository;
    private readonly IMetricRepository _metricRepository;

    public UpdateDatasetCommandHandler(
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

    public async Task<DatasetDto> Handle(
        UpdateDatasetCommand request,
        CancellationToken cancellationToken)
    {
        var dataset = await _datasetRepository.GetByIdAsync(request.Id, request.TenantId, cancellationToken)
            ?? throw new DatasetNotFoundException(request.Id, request.TenantId);

        dataset.Update(request.Name, request.Description, request.SchemaName, request.TableName, request.CustomSql);

        var updated = await _datasetRepository.UpdateAsync(dataset, cancellationToken);

        // Load related data
        var dimensions = await _dimensionRepository.ListByDatasetAsync(updated.Id, request.TenantId, cancellationToken);
        var measures = await _measureRepository.ListByDatasetAsync(updated.Id, request.TenantId, cancellationToken);
        var metrics = await _metricRepository.ListByDatasetAsync(updated.Id, request.TenantId, cancellationToken);

        return new DatasetDto(
            Id: updated.Id,
            TenantId: updated.TenantId,
            Name: updated.Name,
            Description: updated.Description,
            SourceType: updated.SourceType,
            SchemaName: updated.SchemaName,
            TableName: updated.TableName,
            CustomSql: updated.CustomSql,
            IsActive: updated.IsActive,
            CreatedAt: updated.CreatedAt,
            UpdatedAt: updated.UpdatedAt,
            Dimensions: dimensions.Select(d => new DimensionDto(d.Id, d.DatasetId, d.Name, d.DisplayName, d.Description, d.ColumnName, d.CustomSqlExpression, d.DataType, d.Format, d.IsTimeDimension, d.DefaultGranularity, d.SortOrder, d.IsActive)).ToList(),
            Measures: measures.Select(m => new MeasureDto(m.Id, m.DatasetId, m.Name, m.DisplayName, m.Description, m.ColumnName, m.CustomSqlExpression, m.AggregationType, m.DataType, m.Format, m.FilterExpression, m.SortOrder, m.IsActive)).ToList(),
            Metrics: metrics.Select(m => new MetricDto(m.Id, m.DatasetId, m.Name, m.DisplayName, m.Description, m.Expression, m.DataType, m.Format, m.DependsOnMeasures, m.SortOrder, m.IsActive)).ToList()
        );
    }
}
