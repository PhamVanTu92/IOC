namespace MetadataService.Domain.Entities;

public sealed class Dimension
{
    public Guid Id { get; private set; }
    public Guid DatasetId { get; private set; }
    public Guid TenantId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string DisplayName { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public string ColumnName { get; private set; } = string.Empty;
    public string? CustomSqlExpression { get; private set; }

    /// <summary>'string' | 'number' | 'integer' | 'decimal' | 'date' | 'datetime' | 'boolean'</summary>
    public string DataType { get; private set; } = "string";
    public string? Format { get; private set; }
    public bool IsTimeDimension { get; private set; }

    /// <summary>'hour' | 'day' | 'week' | 'month' | 'quarter' | 'year'</summary>
    public string? DefaultGranularity { get; private set; }
    public int SortOrder { get; private set; }
    public bool IsActive { get; private set; } = true;
    public DateTimeOffset CreatedAt { get; private set; }

    private Dimension() { }

    public static Dimension Create(
        Guid datasetId,
        Guid tenantId,
        string name,
        string displayName,
        string columnName,
        string dataType,
        bool isTimeDimension = false,
        string? description = null,
        string? format = null,
        string? defaultGranularity = null,
        string? customSqlExpression = null,
        int sortOrder = 0)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name, nameof(name));
        ArgumentException.ThrowIfNullOrWhiteSpace(columnName, nameof(columnName));

        return new Dimension
        {
            Id = Guid.NewGuid(),
            DatasetId = datasetId,
            TenantId = tenantId,
            Name = name.Trim(),
            DisplayName = displayName.Trim(),
            Description = description?.Trim(),
            ColumnName = columnName.Trim(),
            CustomSqlExpression = customSqlExpression?.Trim(),
            DataType = dataType.ToLowerInvariant(),
            Format = format,
            IsTimeDimension = isTimeDimension,
            DefaultGranularity = defaultGranularity?.ToLowerInvariant(),
            SortOrder = sortOrder,
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }

    public void Update(
        string displayName,
        string? description,
        string dataType,
        string? format,
        bool isTimeDimension,
        string? defaultGranularity,
        string? customSqlExpression,
        int sortOrder)
    {
        DisplayName = displayName.Trim();
        Description = description?.Trim();
        DataType = dataType.ToLowerInvariant();
        Format = format;
        IsTimeDimension = isTimeDimension;
        DefaultGranularity = defaultGranularity?.ToLowerInvariant();
        CustomSqlExpression = customSqlExpression?.Trim();
        SortOrder = sortOrder;
    }

    public void Deactivate() => IsActive = false;
}
