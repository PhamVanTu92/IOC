namespace SemanticEngine.Models;

/// <summary>
/// SemanticDataset — định nghĩa đầy đủ một nguồn dữ liệu trong Semantic Layer.
/// Chứa metadata của bảng/view cùng tất cả dimensions, measures, metrics.
/// </summary>
public sealed class SemanticDataset
{
    public Guid Id { get; init; }
    public Guid TenantId { get; init; }
    public required string Name { get; init; }
    public string? Description { get; init; }

    /// <summary>Loại source: 'postgresql', 'view', 'custom_sql'</summary>
    public required string SourceType { get; init; }

    /// <summary>Schema PostgreSQL, null nếu dùng default</summary>
    public string? SchemaName { get; init; }

    /// <summary>Tên bảng/view, null nếu dùng CustomSql</summary>
    public string? TableName { get; init; }

    /// <summary>Custom SQL subquery — dùng làm FROM clause</summary>
    public string? CustomSql { get; init; }

    public IReadOnlyList<SemanticDimension> Dimensions { get; init; } = [];
    public IReadOnlyList<SemanticMeasure> Measures { get; init; } = [];
    public IReadOnlyList<SemanticMetric> Metrics { get; init; } = [];

    // ─── Lookup helpers ───────────────────────────────────────────────────────

    public SemanticDimension? GetDimension(string name) =>
        Dimensions.FirstOrDefault(d => d.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

    public SemanticMeasure? GetMeasure(string name) =>
        Measures.FirstOrDefault(m => m.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

    public SemanticMetric? GetMetric(string name) =>
        Metrics.FirstOrDefault(m => m.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

    // ─── SQL generation helpers ───────────────────────────────────────────────

    /// <summary>
    /// Trả về FROM expression:
    /// - custom SQL → "(SELECT ...) AS __dataset"
    /// - schema.table → "\"schema\".\"table\""
    /// - table only → "\"table\""
    /// </summary>
    public string GetFromExpression()
    {
        if (CustomSql is not null)
            return $"({CustomSql}) AS __dataset";

        return SchemaName is not null
            ? $"\"{SchemaName}\".\"{TableName}\""
            : $"\"{TableName}\"";
    }

    /// <summary>Tenant filter SQL — mọi query PHẢI có filter này</summary>
    public string GetTenantFilter(string paramName = "@tenantId") =>
        $"tenant_id = {paramName}";
}
