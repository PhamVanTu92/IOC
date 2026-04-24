using HotChocolate;
using HotChocolate.Authorization;
using HotChocolate.Types;

namespace Gateway.Layouts;

[ExtendObjectType("Mutation")]
public sealed class LayoutMutation
{
    [Authorize]
    public async Task<bool> SaveLayoutAsync(
        Guid? moduleId,
        string layoutJson,
        [Service] LayoutRepository repo,
        [Service] IHttpContextAccessor http,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId(http);
        await repo.UpsertAsync(userId, moduleId, layoutJson, cancellationToken);
        return true;
    }

    private static Guid GetUserId(IHttpContextAccessor http)
    {
        var raw = http.HttpContext?.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
               ?? http.HttpContext?.User.FindFirst("sub")?.Value ?? "";
        return Guid.TryParse(raw, out var g) ? g : Guid.Empty;
    }
}
