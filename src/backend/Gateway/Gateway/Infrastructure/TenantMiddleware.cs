using System.Security.Claims;

namespace Gateway.Infrastructure;

// ─────────────────────────────────────────────────────────────────────────────
// TenantMiddleware — resolves TenantId + UserId per request
//
// Resolution order:
//   1. JWT claim "tid"  (production)
//   2. X-Tenant-Id header  (service-to-service)
//   3. Dev fallback UUID   (local development)
// ─────────────────────────────────────────────────────────────────────────────

public sealed class TenantMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context, TenantContext tenantContext)
    {
        // 1. Try JWT claims
        var user = context.User;
        if (user.Identity?.IsAuthenticated == true)
        {
            var tidClaim = user.FindFirst("tid") ?? user.FindFirst("tenant_id");
            var subClaim = user.FindFirst(ClaimTypes.NameIdentifier) ?? user.FindFirst("sub");

            if (tidClaim is not null && Guid.TryParse(tidClaim.Value, out var tenantId))
            {
                tenantContext.TenantId = tenantId;
                tenantContext.IsResolved = true;
            }

            if (subClaim is not null && Guid.TryParse(subClaim.Value, out var userId))
            {
                tenantContext.UserId = userId;
            }
        }

        // 2. Try X-Tenant-Id header (service-to-service / dev override)
        if (!tenantContext.IsResolved &&
            context.Request.Headers.TryGetValue("X-Tenant-Id", out var headerValue) &&
            Guid.TryParse(headerValue, out var headerTenantId))
        {
            tenantContext.TenantId = headerTenantId;
            tenantContext.IsResolved = true;
        }

        // 3. Dev fallback — already set in TenantContext constructor
        // (no action needed; TenantContext defaults to dev UUID)

        await next(context);
    }
}
