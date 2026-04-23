namespace SemanticEngine.Models;

public sealed class QuerySort
{
    /// <summary>Tên field (dimension hoặc measure) trong SemanticDataset</summary>
    public required string FieldName { get; init; }
    public SortDirection Direction { get; init; } = SortDirection.Descending;
}
