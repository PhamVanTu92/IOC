namespace Gateway.Infrastructure;

// ─────────────────────────────────────────────────────────────────────────────
// TenantContext — scoped per-request, resolved by TenantMiddleware
// ─────────────────────────────────────────────────────────────────────────────

public sealed class TenantContext
{
    private static readonly Guid _devFallbackTenantId =
        new("00000000-0000-0000-0000-000000000001");

    private static readonly Guid _devFallbackUserId =
        new("00000000-0000-0000-0000-000000000002");

    public Guid TenantId { get; set; } = _devFallbackTenantId;
    public Guid UserId { get; set; } = _devFallbackUserId;
    public bool IsResolved { get; set; }
}
