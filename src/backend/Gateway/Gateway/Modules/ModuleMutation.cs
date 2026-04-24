using Gateway.Auth;
using Gateway.Infrastructure;
using HotChocolate;
using HotChocolate.Authorization;
using HotChocolate.Types;

namespace Gateway.Modules;

public sealed record CreateModuleInput(
    string  Name,
    string  Slug,
    string? Description,
    string  Icon,
    string  Color,
    int     SortOrder);

public sealed record UpdateModuleInput(
    Guid    Id,
    string  Name,
    string? Description,
    string  Icon,
    string  Color,
    int     SortOrder);

[ExtendObjectType("Mutation")]
public sealed class ModuleMutation
{
    [Authorize(Roles = new[] { "admin" })]
    public async Task<ModuleGql> CreateModuleAsync(
        CreateModuleInput input,
        [Service] ModuleRepository repo,
        [Service] TenantContext tenant,
        [Service] IHttpContextAccessor http,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId(http);
        var id = await repo.CreateAsync(
            tenant.TenantId, userId,
            input.Name, input.Slug, input.Description,
            input.Icon, input.Color, input.SortOrder, cancellationToken);

        var row = await repo.GetByIdAsync(id, tenant.TenantId, cancellationToken)
            ?? throw new Exception("Module not found after create");
        return ToGql(row);
    }

    [Authorize(Roles = new[] { "admin" })]
    public async Task<ModuleGql> UpdateModuleAsync(
        UpdateModuleInput input,
        [Service] ModuleRepository repo,
        [Service] TenantContext tenant,
        CancellationToken cancellationToken)
    {
        var ok = await repo.UpdateAsync(
            input.Id, tenant.TenantId,
            input.Name, input.Description, input.Icon, input.Color, input.SortOrder,
            cancellationToken);
        if (!ok) throw new HotChocolate.GraphQLException(
            HotChocolate.ErrorBuilder.New().SetMessage("Module not found").SetCode("NOT_FOUND").Build());

        var row = await repo.GetByIdAsync(input.Id, tenant.TenantId, cancellationToken)!;
        return ToGql(row!);
    }

    [Authorize(Roles = new[] { "admin" })]
    public async Task<bool> DeleteModuleAsync(
        Guid id,
        [Service] ModuleRepository repo,
        [Service] TenantContext tenant,
        CancellationToken cancellationToken)
        => await repo.DeleteAsync(id, tenant.TenantId, cancellationToken);

    private static Guid GetUserId(IHttpContextAccessor http)
    {
        var raw = http.HttpContext?.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
               ?? http.HttpContext?.User.FindFirst("sub")?.Value ?? "";
        return Guid.TryParse(raw, out var g) ? g : Guid.Empty;
    }

    private static ModuleGql ToGql(ModuleRow r) =>
        new(r.id, r.name, r.slug, r.description, r.icon, r.color, r.sort_order, r.created_at);
}
