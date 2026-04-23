namespace MetadataService.Domain.Entities;

public sealed class Metric
{
    public Guid Id { get; private set; }
    public Guid DatasetId { get; private set; }
    public Guid TenantId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string DisplayName { get; private set; } = string.Empty;
    public string? Description { get; private set; }

    /// <summary>
    /// SQL expression với placeholders {{measure_name}}.
    /// Ví dụ: "{{revenue}} / NULLIF({{orders}}, 0)"
    /// </summary>
    public string Expression { get; private set; } = string.Empty;

    public string DataType { get; private set; } = "decimal";
    public string? Format { get; private set; }

    /// <summary>Danh sách tên measures mà metric này phụ thuộc</summary>
    public string[] DependsOnMeasures { get; private set; } = [];

    public int SortOrder { get; private set; }
    public bool IsActive { get; private set; } = true;
    public DateTimeOffset CreatedAt { get; private set; }

    private Metric() { }

    public static Metric Create(
        Guid datasetId,
        Guid tenantId,
        string name,
        string displayName,
        string expression,
        string[]? dependsOnMeasures = null,
        string? description = null,
        string dataType = "decimal",
        string? format = null,
        int sortOrder = 0)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name, nameof(name));
        ArgumentException.ThrowIfNullOrWhiteSpace(expression, nameof(expression));

        return new Metric
        {
            Id = Guid.NewGuid(),
            DatasetId = datasetId,
            TenantId = tenantId,
            Name = name.Trim(),
            DisplayName = displayName.Trim(),
            Description = description?.Trim(),
            Expression = expression.Trim(),
            DependsOnMeasures = dependsOnMeasures ?? [],
            DataType = dataType.ToLowerInvariant(),
            Format = format,
            SortOrder = sortOrder,
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }

    public void Update(
        string displayName,
        string? description,
        string expression,
        string[] dependsOnMeasures,
        string dataType,
        string? format,
        int sortOrder)
    {
        DisplayName = displayName.Trim();
        Description = description?.Trim();
        Expression = expression.Trim();
        DependsOnMeasures = dependsOnMeasures;
        DataType = dataType.ToLowerInvariant();
        Format = format;
        SortOrder = sortOrder;
    }

    public void Deactivate() => IsActive = false;
}
