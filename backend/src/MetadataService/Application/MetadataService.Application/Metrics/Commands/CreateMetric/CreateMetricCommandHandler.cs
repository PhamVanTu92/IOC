using MediatR;
using MetadataService.Application.Datasets.DTOs;
using MetadataService.Domain.Entities;
using MetadataService.Domain.Exceptions;
using MetadataService.Domain.Interfaces;

namespace MetadataService.Application.Metrics.Commands.CreateMetric;

public sealed class CreateMetricCommandHandler
    : IRequestHandler<CreateMetricCommand, MetricDto>
{
    private readonly IDatasetRepository _datasetRepository;
    private readonly IMetricRepository _metricRepository;
    private readonly IMeasureRepository _measureRepository;

    public CreateMetricCommandHandler(
        IDatasetRepository datasetRepository,
        IMetricRepository metricRepository,
        IMeasureRepository measureRepository)
    {
        _datasetRepository = datasetRepository;
        _metricRepository = metricRepository;
        _measureRepository = measureRepository;
    }

    public async Task<MetricDto> Handle(
        CreateMetricCommand request,
        CancellationToken cancellationToken)
    {
        if (!await _datasetRepository.ExistsAsync(request.DatasetId, request.TenantId, cancellationToken))
            throw new DatasetNotFoundException(request.DatasetId, request.TenantId);

        // Validate that referenced measures exist
        if (request.DependsOnMeasures?.Length > 0)
        {
            var measures = await _measureRepository.ListByDatasetAsync(
                request.DatasetId, request.TenantId, cancellationToken);
            var measureNames = measures.Select(m => m.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);

            var missing = request.DependsOnMeasures
                .Where(m => !measureNames.Contains(m))
                .ToList();

            if (missing.Count > 0)
                throw new InvalidOperationException(
                    $"Measures not found in dataset: {string.Join(", ", missing)}");
        }

        var metric = Metric.Create(
            datasetId: request.DatasetId,
            tenantId: request.TenantId,
            name: request.Name,
            displayName: request.DisplayName,
            expression: request.Expression,
            dependsOnMeasures: request.DependsOnMeasures,
            description: request.Description,
            dataType: request.DataType,
            format: request.Format,
            sortOrder: request.SortOrder
        );

        var created = await _metricRepository.CreateAsync(metric, cancellationToken);

        return new MetricDto(
            created.Id, created.DatasetId, created.Name, created.DisplayName,
            created.Description, created.Expression, created.DataType, created.Format,
            created.DependsOnMeasures, created.SortOrder, created.IsActive);
    }
}
