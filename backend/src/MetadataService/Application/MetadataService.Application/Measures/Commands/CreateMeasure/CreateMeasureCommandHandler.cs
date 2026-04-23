using MediatR;
using MetadataService.Application.Datasets.DTOs;
using MetadataService.Domain.Entities;
using MetadataService.Domain.Exceptions;
using MetadataService.Domain.Interfaces;

namespace MetadataService.Application.Measures.Commands.CreateMeasure;

public sealed class CreateMeasureCommandHandler
    : IRequestHandler<CreateMeasureCommand, MeasureDto>
{
    private readonly IDatasetRepository _datasetRepository;
    private readonly IMeasureRepository _measureRepository;

    public CreateMeasureCommandHandler(
        IDatasetRepository datasetRepository,
        IMeasureRepository measureRepository)
    {
        _datasetRepository = datasetRepository;
        _measureRepository = measureRepository;
    }

    public async Task<MeasureDto> Handle(
        CreateMeasureCommand request,
        CancellationToken cancellationToken)
    {
        if (!await _datasetRepository.ExistsAsync(request.DatasetId, request.TenantId, cancellationToken))
            throw new DatasetNotFoundException(request.DatasetId, request.TenantId);

        var measure = Measure.Create(
            datasetId: request.DatasetId,
            tenantId: request.TenantId,
            name: request.Name,
            displayName: request.DisplayName,
            columnName: request.ColumnName,
            aggregationType: request.AggregationType,
            description: request.Description,
            dataType: request.DataType,
            format: request.Format,
            filterExpression: request.FilterExpression,
            customSqlExpression: request.CustomSqlExpression,
            sortOrder: request.SortOrder
        );

        var created = await _measureRepository.CreateAsync(measure, cancellationToken);

        return new MeasureDto(
            created.Id, created.DatasetId, created.Name, created.DisplayName,
            created.Description, created.ColumnName, created.CustomSqlExpression,
            created.AggregationType, created.DataType, created.Format,
            created.FilterExpression, created.SortOrder, created.IsActive);
    }
}
