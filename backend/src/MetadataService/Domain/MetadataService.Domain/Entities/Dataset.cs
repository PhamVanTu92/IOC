namespace MetadataService.Domain.Entities;

/// <summary>
/// Dataset — Aggregate Root đại diện cho một nguồn dữ liệu trong Semantic Layer.
/// </summary>
public sealed class Dataset
{
    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }

    /// <summary>'postgresql' | 'view' | 'custom_sql'</summary>
    public string SourceType { get; private set; } = string.Empty;

    public string? SchemaName { get; private set; }
    public string? TableName { get; private set; }
    public string? CustomSql { get; private set; }
    public bool IsActive { get; private set; } = true;
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }
    public Guid CreatedBy { get; private set; }

    // Dapper requires parameterless constructor
    private Dataset() { }

    // ─── Factory method ────────────────────────────────────────────────────

    public static Dataset Create(
        Guid tenantId,
        string name,
        string sourceType,
        Guid createdBy,
        string? description = null,
        string? schemaName = null,
        string? tableName = null,
        string? customSql = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name, nameof(name));
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceType, nameof(sourceType));

        if (sourceType != "custom_sql" && tableName is null)
            throw new ArgumentException("TableName is required when SourceType is not 'custom_sql'.");

        if (sourceType == "custom_sql" && customSql is null)
            throw new ArgumentException("CustomSql is required when SourceType is 'custom_sql'.");

        return new Dataset
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Name = name.Trim(),
            Description = description?.Trim(),
            SourceType = sourceType.ToLowerInvariant(),
            SchemaName = schemaName?.Trim(),
            TableName = tableName?.Trim(),
            CustomSql = customSql?.Trim(),
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
            CreatedBy = createdBy
        };
    }

    // ─── Business methods ──────────────────────────────────────────────────

    public void Update(
        string name,
        string? description,
        string? schemaName,
        string? tableName,
        string? customSql)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name, nameof(name));
        Name = name.Trim();
        Description = description?.Trim();
        SchemaName = schemaName?.Trim();
        TableName = tableName?.Trim();
        CustomSql = customSql?.Trim();
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Activate()
    {
        IsActive = true;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public string GetFromExpression()
    {
        if (CustomSql is not null)
            return $"({CustomSql}) AS __ds";
        return SchemaName is not null
            ? $"\"{SchemaName}\".\"{TableName}\""
            : $"\"{TableName}\"";
    }
}
