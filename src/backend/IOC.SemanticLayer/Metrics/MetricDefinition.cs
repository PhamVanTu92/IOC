namespace IOC.SemanticLayer.Metrics;

/// <summary>Loại aggregation cho metric</summary>
public enum AggregationType { Count, Sum, Average, Min, Max, DistinctCount }

/// <summary>
/// Định nghĩa một metric trong Semantic Layer.
/// Metric là đơn vị đo lường nghiệp vụ (vd: Doanh thu, Số nhân viên).
/// </summary>
public sealed class MetricDefinition
{
    /// <summary>ID duy nhất (kebab-case)</summary>
    public required string Id { get; init; }

    /// <summary>Tên hiển thị</summary>
    public required string Name { get; init; }

    /// <summary>Plugin/domain sở hữu metric</summary>
    public required string Domain { get; init; }

    /// <summary>Bảng/nguồn dữ liệu</summary>
    public required string SourceTable { get; init; }

    /// <summary>Cột tính toán (null = dùng COUNT(*))</summary>
    public string? SourceColumn { get; init; }

    /// <summary>Loại aggregation</summary>
    public AggregationType Aggregation { get; init; } = AggregationType.Sum;

    /// <summary>Đơn vị (vd: VND, %, người)</summary>
    public string? Unit { get; init; }

    /// <summary>Mô tả</summary>
    public string? Description { get; init; }

    /// <summary>Filters mặc định (WHERE conditions)</summary>
    public IReadOnlyList<string> DefaultFilters { get; init; } = [];

    /// <summary>Dimensions có thể group by</summary>
    public IReadOnlyList<DimensionRef> Dimensions { get; init; } = [];
}

/// <summary>Tham chiếu đến một dimension để group/filter</summary>
public sealed class DimensionRef
{
    public required string Name { get; init; }
    public required string Column { get; init; }
    public string? Description { get; init; }
}
