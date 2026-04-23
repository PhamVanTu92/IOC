namespace Gateway.Middleware;

/// <summary>
/// Lưu thông tin tenant hiện tại cho mỗi request.
/// Inject vào bất kỳ service/resolver nào cần tenant_id.
/// </summary>
public sealed class TenantContext
{
    public Guid TenantId { get; private set; }
    public Guid UserId { get; private set; }

    private static readonly Guid DefaultTenant = Guid.Parse("00000000-0000-0000-0000-000000000001");
    private static readonly Guid DefaultUser   = Guid.Parse("00000000-0000-0000-0000-000000000099");

    public void Set(Guid tenantId, Guid userId)
    {
        TenantId = tenantId;
        UserId   = userId;
    }

    public bool IsResolved => TenantId != Guid.Empty;
}
