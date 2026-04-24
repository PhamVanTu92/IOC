using HotChocolate;
using HotChocolate.Authorization;
using HotChocolate.Types;

namespace Gateway.Permissions;

public sealed record ModulePermissionGql(Guid UserId, Guid ModuleId, bool CanView, bool CanEdit, DateTime GrantedAt);

[ExtendObjectType("Query")]
public sealed class PermissionQuery
{
    [Authorize(Roles = new[] { "admin" })]
    public async Task<List<ModulePermissionGql>> ModulePermissionsAsync(
        Guid moduleId,
        [Service] PermissionRepository repo,
        CancellationToken cancellationToken)
    {
        var rows = await repo.GetModulePermissionsAsync(moduleId, cancellationToken);
        return rows.Select(r => new ModulePermissionGql(r.user_id, r.module_id, r.can_view, r.can_edit, r.granted_at)).ToList();
    }
}
