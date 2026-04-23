namespace MetadataService.Domain.Entities;

public sealed class Measure
{
    public Guid Id { get; private set; }
    public Guid DatasetId { get; private set; }
    public Guid TenantId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string DisplayName { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public string ColumnName { get; private set; } = string.Empty;
    public string? CustomSqlExpression { get; private set; }

    /// <summary>'sum' | 'average' | 'count' | 'count_distinct' | 'min' | 'max'</summary>
    public string AggregationType { get; private set; } = "sum";

    /// <summary>'number' | 'integer' | 'decimal'</summary>
    public string DataType { get; private set; } = "decimal";
    public string? Format { get; private set; }

    /// <summary>Optional WHERE clause áp dụng riêng cho measure này (PostgreSQL FILTER)</summary>
    public string? FilterExpression { get; private set; }
    public int SortOrder { get; private set; }
    public bool IsActive { get; private set; } = true;
    public DateTimeOffset CreatedAt { get; private set; }

    private Measure() { }

    public static Measure Create(
        Guid datasetId,
        Guid tenantId,
        string name,
        string displayName,
        string columnName,
        string aggregationType,
        string? description = null,
        string dataType = "decimal",
        string? format = null,
        string? filterExpression = null,
        string? customSqlExpression = null,
        int sortOrder = 0)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name, nameof(name));
        ArgumentException.ThrowIfNullOrWhiteSpace(columnName, nameof(columnName));

        return new Measure
        {
            Id = Guid.NewGuid(),
            DatasetId = datasetId,
            TenantId = tenantId,
            Name = name.Trim(),
            DisplayName = displayName.Trim(),
            Description = description?.Trim(),
            ColumnName = columnName.Trim(),
            CustomSqlExpression = customSqlExpression?.Trim(),
            AggregationType = aggregationType.ToLowerInvariant(),
            DataType = dataType.ToLowerInvariant(),
            Format = format,
            FilterExpression = filterExpression?.Trim(),
            SortOrder = sortOrder,
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }

    public void Update(
        string displayName,
        string? description,
        string aggregationType,
        string dataType,
        string? format,
        string? filterExpression,
        string? customSqlExpression,
        int sortOrder)
    {
        DisplayName = displayName.Trim();
        Description = description?.Trim();
        AggregationType = aggregationType.ToLowerInvariant();
        DataType = dataType.ToLowerInvariant();
        Format = format;
        FilterExpression = filterExpression?.Trim();
        CustomSqlExpression = customSqlExpression?.Trim();
        SortOrder = sortOrder;
    }

    public void Deactivate() => IsActive = false;

    /// <summary>Tạo SQL aggregate expression cho measure này</summary>
    public string GetAggregateExpression()
    {
        var col = CustomSqlExpression ?? $"\"{ColumnName}\"";
        var filter = FilterExpression is not null ? $" FILTER (WHERE {FilterExpression})" : "";

        return AggregationType switch
        {
            "sum"            => $"SUM({col}){filter}",
            "average"        => $"AVG({col}){filter}",
            "count"          => $"COUNT({col}){filter}",
            "count_distinct" => $"COUNT(DISTINCT {col}){filter}",
            "min"            => $"MIN({col}){filter}",
            "max"            => $"MAX({col}){filter}",
            _                => $"SUM({col}){filter}"
        };
    }
}
