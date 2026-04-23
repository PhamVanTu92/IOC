using MediatR;
using MetadataService.Application.Datasets.DTOs;
using MetadataService.Domain.Entities;
using MetadataService.Domain.Exceptions;
using MetadataService.Domain.Interfaces;

namespace MetadataService.Application.Datasets.Commands.CreateDataset;

public sealed class CreateDatasetCommandHandler
    : IRequestHandler<CreateDatasetCommand, DatasetDto>
{
    private readonly IDatasetRepository _datasetRepository;

    public CreateDatasetCommandHandler(IDatasetRepository datasetRepository)
    {
        _datasetRepository = datasetRepository;
    }

    public async Task<DatasetDto> Handle(
        CreateDatasetCommand request,
        CancellationToken cancellationToken)
    {
        // Check duplicate name trong cùng tenant
        if (await _datasetRepository.ExistsByNameAsync(request.Name, request.TenantId, cancellationToken))
            throw new DuplicateDatasetException(request.Name, request.TenantId);

        var dataset = Dataset.Create(
            tenantId: request.TenantId,
            name: request.Name,
            sourceType: request.SourceType,
            createdBy: request.CreatedBy,
            description: request.Description,
            schemaName: request.SchemaName,
            tableName: request.TableName,
            customSql: request.CustomSql
        );

        var created = await _datasetRepository.CreateAsync(dataset, cancellationToken);

        return MapToDto(created);
    }

    internal static DatasetDto MapToDto(Dataset d) => new(
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
    );
}
