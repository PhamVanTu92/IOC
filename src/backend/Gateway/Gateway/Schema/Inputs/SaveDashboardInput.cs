namespace Gateway.Schema.Inputs;

// ─────────────────────────────────────────────────────────────────────────────
// SaveDashboardInput — input for createDashboard / updateDashboard mutations
// ─────────────────────────────────────────────────────────────────────────────

/// <summary>
/// For createDashboard — Id must be null (backend generates it).
/// For updateDashboard — Id must be the existing dashboard UUID.
/// </summary>
public sealed record SaveDashboardInput(
    Guid? Id,
    string Title,
    /// <summary>Full serialized frontend DashboardConfig JSON string.</summary>
    string ConfigJson,
    string? Description = null);
