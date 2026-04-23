namespace Gateway.Schema.Inputs;

/// <summary>
/// GraphQL Input type cho dynamic query execution qua Semantic Layer.
/// Maps 1-1 lên SemanticEngine.Models.QueryInput.
/// </summary>
public sealed record QueryRequestInput(
    Guid DatasetId,
    List<string>? Dimensions = null,
    List<string>? Measures = null,
    List<string>? Metrics = null,
    List<QueryFilterInput>? Filters = null,
    List<QuerySortInput>? Sorts = null,
    int Limit = 10_000,
    int Offset = 0,
    string? TimeDimensionName = null,
    string? Granularity = null,
    TimeRangeInput? TimeRange = null,
    bool IncludePreviousPeriod = false,
    bool ForceRefresh = false
);

/// <summary>Filter condition cho query.</summary>
public sealed record QueryFilterInput(
    string FieldName,
    string Operator,
    string? Value = null,
    List<string>? Values = null,
    string? ValueFrom = null,
    string? ValueTo = null
);

/// <summary>Sort order cho query.</summary>
public sealed record QuerySortInput(
    string FieldName,
    string Direction = "ASC"
);

/// <summary>Time range filter — dùng preset hoặc custom from/to.</summary>
public sealed record TimeRangeInput(
    string? Preset = null,
    DateTimeOffset? From = null,
    DateTimeOffset? To = null
);
