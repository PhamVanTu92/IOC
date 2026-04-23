using Gateway.Middleware;
using Gateway.Schema.Inputs;
using HotChocolate;
using HotChocolate.Types;
using MediatR;
using MetadataService.Application.Datasets.Commands.CreateDataset;
using MetadataService.Application.Datasets.Commands.UpdateDataset;
using MetadataService.Application.Datasets.DTOs;
using MetadataService.Application.Dimensions.Commands.CreateDimension;
using MetadataService.Application.Measures.Commands.CreateMeasure;
using MetadataService.Application.Metrics.Commands.CreateMetric;

namespace Gateway.Schema.Mutations;

/// <summary>
/// Root Mutation extensions cho Metadata management.
/// Mỗi mutation nhận Input type, dispatch MediatR command, trả về DTO.
/// </summary>
[ExtendObjectType(OperationTypeNames.Mutation)]
public sealed class MetadataMutation
{
    // ─── Dataset mutations ─────────────────────────────────────────────────

    /// <summary>Tạo mới một Dataset.</summary>
    public async Task<DatasetDto> CreateDatasetAsync(
        CreateDatasetInput input,
        [Service] IMediator mediator,
        [Service] TenantContext tenantContext,
        CancellationToken cancellationToken)
    {
        var command = new CreateDatasetCommand(
            TenantId:    tenantContext.TenantId,
            CreatedBy:   tenantContext.UserId,
            Name:        input.Name,
            SourceType:  input.SourceType,
            Description: input.Description,
            SchemaName:  input.SchemaName,
            TableName:   input.TableName,
            CustomSql:   input.CustomSql
        );

        return await mediator.Send(command, cancellationToken);
    }

    /// <summary>Cập nhật Dataset (tên, description, source info).</summary>
    public async Task<DatasetDto> UpdateDatasetAsync(
        Guid id,
        UpdateDatasetInput input,
        [Service] IMediator mediator,
        [Service] TenantContext tenantContext,
        CancellationToken cancellationToken)
    {
        var command = new UpdateDatasetCommand(
            Id:          id,
            TenantId:    tenantContext.TenantId,
            Name:        input.Name,
            Description: input.Description,
            SchemaName:  input.SchemaName,
            TableName:   input.TableName,
            CustomSql:   input.CustomSql
        );

        return await mediator.Send(command, cancellationToken);
    }

    // ─── Dimension mutations ───────────────────────────────────────────────

    /// <summary>Thêm Dimension vào Dataset.</summary>
    public async Task<DimensionDto> CreateDimensionAsync(
        CreateDimensionInput input,
        [Service] IMediator mediator,
        [Service] TenantContext tenantContext,
        CancellationToken cancellationToken)
    {
        var command = new CreateDimensionCommand(
            DatasetId:           input.DatasetId,
            TenantId:            tenantContext.TenantId,
            Name:                input.Name,
            DisplayName:         input.DisplayName,
            ColumnName:          input.ColumnName,
            DataType:            input.DataType,
            IsTimeDimension:     input.IsTimeDimension,
            Description:         input.Description,
            Format:              input.Format,
            DefaultGranularity:  input.DefaultGranularity,
            CustomSqlExpression: input.CustomSqlExpression,
            SortOrder:           input.SortOrder
        );

        return await mediator.Send(command, cancellationToken);
    }

    // ─── Measure mutations ─────────────────────────────────────────────────

    /// <summary>Thêm Measure vào Dataset.</summary>
    public async Task<MeasureDto> CreateMeasureAsync(
        CreateMeasureInput input,
        [Service] IMediator mediator,
        [Service] TenantContext tenantContext,
        CancellationToken cancellationToken)
    {
        var command = new CreateMeasureCommand(
            DatasetId:           input.DatasetId,
            TenantId:            tenantContext.TenantId,
            Name:                input.Name,
            DisplayName:         input.DisplayName,
            ColumnName:          input.ColumnName,
            AggregationType:     input.AggregationType,
            Description:         input.Description,
            DataType:            input.DataType,
            Format:              input.Format,
            FilterExpression:    input.FilterExpression,
            CustomSqlExpression: input.CustomSqlExpression,
            SortOrder:           input.SortOrder
        );

        return await mediator.Send(command, cancellationToken);
    }

    // ─── Metric mutations ──────────────────────────────────────────────────

    /// <summary>Thêm Metric (computed expression) vào Dataset.</summary>
    public async Task<MetricDto> CreateMetricAsync(
        CreateMetricInput input,
        [Service] IMediator mediator,
        [Service] TenantContext tenantContext,
        CancellationToken cancellationToken)
    {
        var command = new CreateMetricCommand(
            DatasetId:         input.DatasetId,
            TenantId:          tenantContext.TenantId,
            Name:              input.Name,
            DisplayName:       input.DisplayName,
            Expression:        input.Expression,
            DependsOnMeasures: input.DependsOnMeasures,
            Description:       input.Description,
            DataType:          input.DataType,
            Format:            input.Format,
            SortOrder:         input.SortOrder
        );

        return await mediator.Send(command, cancellationToken);
    }
}
