using HotChocolate.Types;

namespace Gateway.Schema.Types;

// ─────────────────────────────────────────────────────────────────────────────
// Dataset GQL types
// ─────────────────────────────────────────────────────────────────────────────

public record DatasetSummaryGql(
    Guid Id,
    Guid? TenantId,
    string Name,
    string? Description,
    string SourceType,
    bool IsActive,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public record DatasetDetailGql(
    Guid Id,
    Guid? TenantId,
    string Name,
    string? Description,
    string SourceType,
    string? SchemaName,
    string? TableName,
    string? CustomSql,
    bool IsActive,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    IReadOnlyList<DimensionGql> Dimensions,
    IReadOnlyList<MeasureGql> Measures,
    IReadOnlyList<MetricGql> Metrics);

public record DimensionGql(
    Guid Id,
    Guid DatasetId,
    string Name,
    string DisplayName,
    string? Description,
    string DataType,
    string? Format,
    bool IsTimeDimension,
    string? DefaultGranularity,
    int SortOrder,
    bool IsActive);

public record MeasureGql(
    Guid Id,
    Guid DatasetId,
    string Name,
    string DisplayName,
    string? Description,
    string AggregationType,
    string DataType,
    string? Format,
    string? FilterExpression,
    int SortOrder,
    bool IsActive);

public record MetricGql(
    Guid Id,
    Guid DatasetId,
    string Name,
    string DisplayName,
    string? Description,
    string Expression,
    string DataType,
    string? Format,
    IReadOnlyList<string> DependsOnMeasures,
    int SortOrder,
    bool IsActive);

public sealed class DatasetSummaryType : ObjectType<DatasetSummaryGql>
{
    protected override void Configure(IObjectTypeDescriptor<DatasetSummaryGql> descriptor)
    {
        descriptor.Name("DatasetSummary");
    }
}

public sealed class DatasetDetailType : ObjectType<DatasetDetailGql>
{
    protected override void Configure(IObjectTypeDescriptor<DatasetDetailGql> descriptor)
    {
        descriptor.Name("Dataset");
    }
}
