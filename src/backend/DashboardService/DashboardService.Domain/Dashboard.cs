namespace DashboardService.Domain;

// ─────────────────────────────────────────────────────────────────────────────
// Dashboard — aggregate root
// config_json stores the full serialized frontend DashboardConfig (JSONB).
// This avoids complex relational mapping and lets the frontend schema evolve
// independently of the backend persistence layer.
// ─────────────────────────────────────────────────────────────────────────────

public sealed class Dashboard
{
    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public Guid CreatedBy { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    /// <summary>Full serialized frontend DashboardConfig as JSON string.</summary>
    public string ConfigJson { get; private set; } = "{}";
    public bool IsActive { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    // ── Private constructor — use factory methods ─────────────────────────────
    private Dashboard() { }

    // ── Factory ───────────────────────────────────────────────────────────────

    public static Dashboard Create(
        Guid tenantId,
        Guid createdBy,
        string title,
        string configJson,
        string? description = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title, nameof(title));
        ArgumentException.ThrowIfNullOrWhiteSpace(configJson, nameof(configJson));

        var now = DateTime.UtcNow;
        return new Dashboard
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            CreatedBy = createdBy,
            Title = title.Trim(),
            Description = description?.Trim(),
            ConfigJson = configJson,
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now,
        };
    }

    // ── Mutations ─────────────────────────────────────────────────────────────

    public void Update(string title, string configJson, string? description = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title, nameof(title));
        ArgumentException.ThrowIfNullOrWhiteSpace(configJson, nameof(configJson));

        Title = title.Trim();
        Description = description?.Trim();
        ConfigJson = configJson;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }

    // ── Reconstitute from persistence (used by Dapper) ────────────────────────

    public static Dashboard Reconstitute(
        Guid id,
        Guid tenantId,
        Guid createdBy,
        string title,
        string? description,
        string configJson,
        bool isActive,
        DateTime createdAt,
        DateTime updatedAt) =>
        new()
        {
            Id = id,
            TenantId = tenantId,
            CreatedBy = createdBy,
            Title = title,
            Description = description,
            ConfigJson = configJson,
            IsActive = isActive,
            CreatedAt = createdAt,
            UpdatedAt = updatedAt,
        };
}
