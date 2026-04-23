namespace Gateway.Middleware;

/// <summary>
/// Middleware resolve tenant_id từ request header hoặc JWT claim.
/// Header: X-Tenant-Id (development) hoặc "tid" JWT claim (production).
/// </summary>
public sealed class TenantMiddleware
{
    private readonly RequestDelegate _next;

    public TenantMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context, TenantContext tenantContext)
    {
        Guid tenantId = Guid.Empty;
        Guid userId   = Guid.Empty;

        // 1. Thử lấy từ JWT claim (production)
        var tidClaim = context.User.FindFirst("tid")?.Value
            ?? context.User.FindFirst("tenant_id")?.Value;
        if (tidClaim is not null)
            Guid.TryParse(tidClaim, out tenantId);

        var subClaim = context.User.FindFirst("sub")?.Value;
        if (subClaim is not null)
            Guid.TryParse(subClaim, out userId);

        // 2. Fallback: X-Tenant-Id header (dev / API key mode)
        if (tenantId == Guid.Empty)
        {
            var headerVal = context.Request.Headers["X-Tenant-Id"].FirstOrDefault();
            if (headerVal is not null)
                Guid.TryParse(headerVal, out tenantId);
        }

        if (userId == Guid.Empty)
        {
            var userHeader = context.Request.Headers["X-User-Id"].FirstOrDefault();
            if (userHeader is not null)
                Guid.TryParse(userHeader, out userId);
        }

        // 3. Development fallback — không yêu cầu auth
        var isDevelopment = context.RequestServices
            .GetRequiredService<IHostEnvironment>().IsDevelopment();

        if (tenantId == Guid.Empty && isDevelopment)
            tenantId = Guid.Parse("00000000-0000-0000-0000-000000000001");

        if (userId == Guid.Empty && isDevelopment)
            userId = Guid.Parse("00000000-0000-0000-0000-000000000099");

        if (tenantId != Guid.Empty)
            tenantContext.Set(tenantId, userId);

        await _next(context);
    }
}
