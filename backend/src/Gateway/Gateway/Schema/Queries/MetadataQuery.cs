using Gateway.Middleware;
using HotChocolate;
using HotChocolate.Types;
using MediatR;
using MetadataService.Application.Datasets.DTOs;
using MetadataService.Application.Datasets.Queries.GetDataset;
using MetadataService.Application.Datasets.Queries.ListDatasets;
using SemanticEngine.Models;
using Gateway.Schema.Inputs;

namespace Gateway.Schema.Queries;

/// <summary>
/// Root Query extensions cho Metadata + Query Execution.
/// Sử dụng [ExtendObjectType] pattern của HotChocolate v14.
/// </summary>
[ExtendObjectType(OperationTypeNames.Query)]
public sealed class MetadataQuery
{
    // ─── Dataset queries ───────────────────────────────────────────────────

    /// <summary>Lấy chi tiết một Dataset kèm toàn bộ Dimensions, Measures, Metrics.</summary>
    public async Task<DatasetDto?> DatasetAsync(
        Guid id,
        [Service] IMediator mediator,
        [Service] TenantContext tenantContext,
        CancellationToken cancellationToken)
    {
        var query = new GetDatasetQuery(id, tenantContext.TenantId);
        return await mediator.Send(query, cancellationToken);
    }

    /// <summary>Liệt kê tất cả Datasets của tenant hiện tại.</summary>
    public async Task<IReadOnlyList<DatasetDto>> DatasetsAsync(
        [Service] IMediator mediator,
        [Service] TenantContext tenantContext,
        CancellationToken cancellationToken,
        bool includeInactive = false)
    {
        var query = new ListDatasetsQuery(tenantContext.TenantId, includeInactive);
        return await mediator.Send(query, cancellationToken);
    }
}

/// <summary>
/// Query execution — chạy dynamic SQL query qua Semantic Layer.
/// Tách thành extension riêng để dễ kiểm soát permission.
/// </summary>
[ExtendObjectType(OperationTypeNames.Query)]
public sealed class QueryExecutionQuery
{
    /// <summary>
    /// Thực thi một dynamic query qua Semantic Layer.
    /// Kết quả được cache trong Redis (trừ khi forceRefresh = true).
    /// </summary>
    public async Task<QueryResult> ExecuteQueryAsync(
        QueryRequestInput input,
        [Service] IMediator mediator,
        [Service] TenantContext tenantContext,
        CancellationToken cancellationToken)
    {
        var queryInput = MapToQueryInput(input, tenantContext.TenantId);

        var errors = queryInput.Validate();
        if (errors.Count > 0)
            throw new GraphQLException(errors.Select(e => new Error(e)).ToArray());

        var command = new QueryService.Application.ExecuteQuery.ExecuteQueryCommand(queryInput);
        return await mediator.Send(command, cancellationToken);
    }

    private static QueryInput MapToQueryInput(QueryRequestInput input, Guid tenantId)
    {
        TimeRange? timeRange = null;
        if (input.TimeRange is not null)
        {
            timeRange = new TimeRange
            {
                Preset = input.TimeRange.Preset,
                From = input.TimeRange.From,
                To = input.TimeRange.To,
            };
        }

        TimeGranularity? granularity = null;
        if (input.Granularity is not null &&
            Enum.TryParse<TimeGranularity>(input.Granularity, ignoreCase: true, out var gran))
        {
            granularity = gran;
        }

        return new QueryInput
        {
            DatasetId = input.DatasetId,
            TenantId = tenantId,
            Dimensions = input.Dimensions ?? [],
            Measures = input.Measures ?? [],
            Metrics = input.Metrics ?? [],
            Filters = (input.Filters ?? [])
                .Select(f => new QueryFilter
                {
                    FieldName = f.FieldName,
                    Operator = Enum.TryParse<FilterOperator>(f.Operator, ignoreCase: true, out var op)
                        ? op : FilterOperator.Equals,
                    Value = f.Value,
                    Values = f.Values,
                    ValueFrom = f.ValueFrom,
                    ValueTo = f.ValueTo,
                })
                .ToList(),
            Sorts = (input.Sorts ?? [])
                .Select(s => new QuerySort
                {
                    FieldName = s.FieldName,
                    Direction = s.Direction.Equals("DESC", StringComparison.OrdinalIgnoreCase)
                        ? SortDirection.Descending
                        : SortDirection.Ascending,
                })
                .ToList(),
            Limit = input.Limit,
            Offset = input.Offset,
            TimeDimensionName = input.TimeDimensionName,
            Granularity = granularity,
            TimeRange = timeRange,
            IncludePreviousPeriod = input.IncludePreviousPeriod,
            ForceRefresh = input.ForceRefresh,
        };
    }
}
