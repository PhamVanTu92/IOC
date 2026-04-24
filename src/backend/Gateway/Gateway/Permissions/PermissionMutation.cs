using HotChocolate;
using HotChocolate.Authorization;
using HotChocolate.Types;

namespace Gateway.Permissions;

public sealed record AssignModulePermissionInput(
    Guid UserId, Guid ModuleId, bool CanView, bool CanEdit);

public sealed record AssignChartPermissionInput(
    Guid UserId, Guid ChartId, bool CanView, bool CanEdit);

[ExtendObjectType("Mutation")]
public sealed class PermissionMutation
{
    [Authorize(Roles = new[] { "admin" })]
    public async Task<bool> AssignModulePermissionAsync(
        AssignModulePermissionInput input,
        [Service] PermissionRepository repo,
        [Service] IHttpContextAccessor http,
        CancellationToken cancellationToken)
    {
        var grantedBy = GetUserId(http);
        await repo.UpsertModulePermissionAsync(
            input.UserId, input.ModuleId, input.CanView, input.CanEdit, grantedBy, cancellationToken);
        return true;
    }

    [Authorize(Roles = new[] { "admin" })]
    public async Task<bool> RevokeModulePermissionAsync(
        Guid userId, Guid moduleId,
        [Service] PermissionRepository repo,
        CancellationToken cancellationToken)
        => await repo.RevokeModulePermissionAsync(userId, moduleId, cancellationToken);

    [Authorize(Roles = new[] { "admin" })]
    public async Task<bool> AssignChartPermissionAsync(
        AssignChartPermissionInput input,
        [Service] PermissionRepository repo,
        [Service] IHttpContextAccessor http,
        CancellationToken cancellationToken)
    {
        var grantedBy = GetUserId(http);
        await repo.UpsertChartPermissionAsync(
            input.UserId, input.ChartId, input.CanView, input.CanEdit, grantedBy, cancellationToken);
        return true;
    }

    private static Guid GetUserId(IHttpContextAccessor http)
    {
        var raw = http.HttpContext?.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
               ?? http.HttpContext?.User.FindFirst("sub")?.Value ?? "";
        return Guid.TryParse(raw, out var g) ? g : Guid.Empty;
    }
}
