using HotChocolate.Types;

namespace Gateway.Schema.Types;

// ─────────────────────────────────────────────────────────────────────────────
// Semantic Layer — Query execution types
// ─────────────────────────────────────────────────────────────────────────────

// ── Input ─────────────────────────────────────────────────────────────────────

public record QueryRequestInput(
    Guid DatasetId,
    string[]? Dimensions,
    string[]? Measures,
    string[]? Metrics,
    QueryFilterInput[]? Filters,
    QuerySortInput[]? Sorts,
    int? Limit,
    string? TimeDimensionName,
    string? Granularity,
    TimeRangeInput? TimeRange,
    bool ForceRefresh = false);

public record QueryFilterInput(
    string FieldName,
    string Operator,
    string? Value,
    string[]? Values,
    string? ValueFrom,
    string? ValueTo);

public record QuerySortInput(
    string FieldName,
    string Direction);  // "asc" | "desc"

public record TimeRangeInput(
    string? Preset,
    string? From,
    string? To);

// ── Output ────────────────────────────────────────────────────────────────────

public record QueryResultGql(
    IReadOnlyList<QueryColumnGql> Columns,
    IReadOnlyList<string> Rows,
    QueryMetadataGql Metadata);

public record QueryColumnGql(
    string Name,
    string DisplayName,
    string DataType,
    string? Format,
    string FieldType);  // "dimension" | "measure" | "metric"

public record QueryMetadataGql(
    string? GeneratedSql,
    long ExecutionTimeMs,
    int TotalRows,
    bool FromCache,
    string? CacheKey,
    DateTime ExecutedAt,
    string? ErrorMessage);

// ── HotChocolate type registrations ──────────────────────────────────────────

public sealed class QueryResultType : ObjectType<QueryResultGql>
{
    protected override void Configure(IObjectTypeDescriptor<QueryResultGql> descriptor)
    {
        descriptor.Name("QueryResult");
    }
}
