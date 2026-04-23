namespace SemanticEngine.Models;

/// <summary>
/// QueryInput — toàn bộ thông tin cần thiết để build một dynamic SQL query.
/// Đây là contract giữa UI (Chart Builder) và Query Engine.
/// </summary>
public sealed class QueryInput
{
    /// <summary>Dataset cần query</summary>
    public required Guid DatasetId { get; init; }

    /// <summary>Tenant hiện tại — bắt buộc, dùng để isolation</summary>
    public required Guid TenantId { get; init; }

    /// <summary>Danh sách tên dimensions cần SELECT + GROUP BY</summary>
    public IReadOnlyList<string> Dimensions { get; init; } = [];

    /// <summary>Danh sách tên measures cần SELECT (aggregate)</summary>
    public IReadOnlyList<string> Measures { get; init; } = [];

    /// <summary>Danh sách tên metrics (computed measures)</summary>
    public IReadOnlyList<string> Metrics { get; init; } = [];

    /// <summary>Filter conditions → WHERE clause</summary>
    public IReadOnlyList<QueryFilter> Filters { get; init; } = [];

    /// <summary>ORDER BY</summary>
    public IReadOnlyList<QuerySort> Sorts { get; init; } = [];

    /// <summary>LIMIT (default 10000, max 100000)</summary>
    public int Limit { get; init; } = 10_000;

    /// <summary>OFFSET cho pagination</summary>
    public int Offset { get; init; } = 0;

    // ─── Time dimension ────────────────────────────────────────────────────

    /// <summary>Tên time dimension để filter theo thời gian</summary>
    public string? TimeDimensionName { get; init; }

    /// <summary>Granularity để truncate time dimension</summary>
    public TimeGranularity? Granularity { get; init; }

    /// <summary>Khoảng thời gian filter</summary>
    public TimeRange? TimeRange { get; init; }

    // ─── Advanced options ──────────────────────────────────────────────────

    /// <summary>Có compare với period trước không (WoW, MoM, YoY)</summary>
    public bool IncludePreviousPeriod { get; init; } = false;

    /// <summary>Bypass Redis cache</summary>
    public bool ForceRefresh { get; init; } = false;

    // ─── Validation ───────────────────────────────────────────────────────

    public IReadOnlyList<string> Validate()
    {
        var errors = new List<string>();

        if (DatasetId == Guid.Empty)
            errors.Add("DatasetId is required.");

        if (TenantId == Guid.Empty)
            errors.Add("TenantId is required.");

        if (Dimensions.Count == 0 && Measures.Count == 0 && Metrics.Count == 0)
            errors.Add("At least one dimension or measure must be selected.");

        if (Limit <= 0 || Limit > 100_000)
            errors.Add("Limit must be between 1 and 100,000.");

        if (Offset < 0)
            errors.Add("Offset must be >= 0.");

        if (TimeDimensionName is not null && TimeRange is null)
            errors.Add("TimeRange is required when TimeDimensionName is specified.");

        return errors;
    }

    /// <summary>Cache key duy nhất cho query này</summary>
    public string ToCacheKey()
    {
        var parts = new List<string>
        {
            $"t:{TenantId}",
            $"ds:{DatasetId}",
            $"d:{string.Join(",", Dimensions.OrderBy(x => x))}",
            $"m:{string.Join(",", Measures.OrderBy(x => x))}",
            $"met:{string.Join(",", Metrics.OrderBy(x => x))}",
            $"lim:{Limit}:{Offset}",
        };

        if (TimeDimensionName is not null)
            parts.Add($"td:{TimeDimensionName}:{Granularity}");

        if (TimeRange?.Preset is not null)
            parts.Add($"tr:{TimeRange.Preset}");
        else if (TimeRange?.From is not null || TimeRange?.To is not null)
            parts.Add($"tr:{TimeRange.From:O}~{TimeRange.To:O}");

        // Filters — sorted for determinism
        var filterHash = string.Join("|", Filters
            .OrderBy(f => f.FieldName)
            .ThenBy(f => f.Operator)
            .Select(f => $"{f.FieldName}:{f.Operator}:{f.Value}"));
        if (!string.IsNullOrEmpty(filterHash))
            parts.Add($"f:{filterHash}");

        return string.Join(";", parts);
    }
}
