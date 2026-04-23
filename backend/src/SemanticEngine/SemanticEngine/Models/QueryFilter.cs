namespace SemanticEngine.Models;

/// <summary>
/// Filter condition cho một query — áp vào WHERE clause.
/// </summary>
public sealed class QueryFilter
{
    /// <summary>Tên dimension hoặc measure trong SemanticDataset</summary>
    public required string FieldName { get; init; }

    public required FilterOperator Operator { get; init; }

    /// <summary>Giá trị đơn (dùng với Equals, GreaterThan, Contains...)</summary>
    public object? Value { get; init; }

    /// <summary>Danh sách giá trị (dùng với In, NotIn)</summary>
    public IReadOnlyList<object>? Values { get; init; }

    /// <summary>Giá trị từ/đến cho Between</summary>
    public object? ValueFrom { get; init; }
    public object? ValueTo { get; init; }
}
