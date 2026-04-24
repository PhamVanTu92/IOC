using HotChocolate;
using HotChocolate.Authorization;
using HotChocolate.Types;

namespace Gateway.Layouts;

public sealed record LayoutGql(Guid UserId, Guid? ModuleId, string LayoutJson, DateTime UpdatedAt);

[ExtendObjectType("Query")]
public sealed class LayoutQuery
{
    [Authorize]
    public async Task<LayoutGql?> MyLayoutAsync(
        Guid? moduleId,
        [Service] LayoutRepository repo,
        [Service] IHttpContextAccessor http,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId(http);
        var row = await repo.GetAsync(userId, moduleId, cancellationToken);
        return row is null ? null : new LayoutGql(row.user_id, row.module_id, row.layout_json, row.updated_at);
    }

    private static Guid GetUserId(IHttpContextAccessor http)
    {
        var raw = http.HttpContext?.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
               ?? http.HttpContext?.User.FindFirst("sub")?.Value ?? "";
        return Guid.TryParse(raw, out var g) ? g : Guid.Empty;
    }
}
