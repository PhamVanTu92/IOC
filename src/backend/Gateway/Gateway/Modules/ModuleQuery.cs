using Gateway.Auth;
using Gateway.Infrastructure;
using Gateway.Permissions;
using HotChocolate;
using HotChocolate.Types;

namespace Gateway.Modules;

// GQL output type
public sealed record ModuleGql(
    Guid    Id,
    string  Name,
    string  Slug,
    string? Description,
    string  Icon,
    string  Color,
    int     SortOrder,
    DateTime CreatedAt);

[ExtendObjectType("Query")]
public sealed class ModuleQuery
{
    /// <summary>Returns all active modules the current user has access to.</summary>
    [HotChocolate.Authorization.Authorize]
    public async Task<List<ModuleGql>> ModulesAsync(
        [Service] ModuleRepository repo,
        [Service] TenantContext tenant,
        [Service] IHttpContextAccessor http,
        CancellationToken cancellationToken)
    {
        var role = http.HttpContext?.User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value ?? "";
        var userId = GetUserId(http);
        var all = await repo.GetByTenantAsync(tenant.TenantId, cancellationToken);

        // Admins see everything; others see only permitted modules
        if (role == "admin")
            return all.Select(ToGql).ToList();

        var permRepo = http.HttpContext!.RequestServices.GetRequiredService<PermissionRepository>();
        var permitted = await permRepo.GetUserModuleIdsAsync(userId, cancellationToken);
        return all.Where(m => permitted.Contains(m.id)).Select(ToGql).ToList();
    }

    private static Guid GetUserId(IHttpContextAccessor http)
    {
        var raw = http.HttpContext?.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
               ?? http.HttpContext?.User.FindFirst("sub")?.Value ?? "";
        return Guid.TryParse(raw, out var g) ? g : Guid.Empty;
    }

    private static ModuleGql ToGql(ModuleRow r) =>
        new(r.id, r.name, r.slug, r.description, r.icon, r.color, r.sort_order, r.created_at);
}
