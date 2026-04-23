using MediatR;
using MetadataService.Application.Datasets.DTOs;
using MetadataService.Domain.Entities;
using MetadataService.Domain.Exceptions;
using MetadataService.Domain.Interfaces;

namespace MetadataService.Application.Dimensions.Commands.CreateDimension;

public sealed class CreateDimensionCommandHandler
    : IRequestHandler<CreateDimensionCommand, DimensionDto>
{
    private readonly IDatasetRepository _datasetRepository;
    private readonly IDimensionRepository _dimensionRepository;

    public CreateDimensionCommandHandler(
        IDatasetRepository datasetRepository,
        IDimensionRepository dimensionRepository)
    {
        _datasetRepository = datasetRepository;
        _dimensionRepository = dimensionRepository;
    }

    public async Task<DimensionDto> Handle(
        CreateDimensionCommand request,
        CancellationToken cancellationToken)
    {
        // Verify dataset tồn tại và thuộc tenant
        if (!await _datasetRepository.ExistsAsync(request.DatasetId, request.TenantId, cancellationToken))
            throw new DatasetNotFoundException(request.DatasetId, request.TenantId);

        var dimension = Dimension.Create(
            datasetId: request.DatasetId,
            tenantId: request.TenantId,
            name: request.Name,
            displayName: request.DisplayName,
            columnName: request.ColumnName,
            dataType: request.DataType,
            isTimeDimension: request.IsTimeDimension,
            description: request.Description,
            format: request.Format,
            defaultGranularity: request.DefaultGranularity,
            customSqlExpression: request.CustomSqlExpression,
            sortOrder: request.SortOrder
        );

        var created = await _dimensionRepository.CreateAsync(dimension, cancellationToken);

        return new DimensionDto(
            created.Id, created.DatasetId, created.Name, created.DisplayName,
            created.Description, created.ColumnName, created.CustomSqlExpression,
            created.DataType, created.Format, created.IsTimeDimension,
            created.DefaultGranularity, created.SortOrder, created.IsActive);
    }
}
