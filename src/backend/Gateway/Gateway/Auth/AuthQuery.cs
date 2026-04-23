using System.Security.Claims;
using HotChocolate;
using HotChocolate.Authorization;
using HotChocolate.Types;

namespace Gateway.Auth;

// ─────────────────────────────────────────────────────────────────────────────
// AuthQuery — me: UserInfo [Authorize]
// ─────────────────────────────────────────────────────────────────────────────

[ExtendObjectType("Query")]
public sealed class AuthQuery
{
    /// <summary>
    /// Returns the authenticated user's profile extracted from JWT claims.
    /// Requires a valid Bearer token.
    /// </summary>
    [Authorize]
    public async Task<UserInfo> MeAsync(
        [Service] IHttpContextAccessor httpContextAccessor,
        [Service] UserRepository userRepo,
        CancellationToken cancellationToken)
    {
        var user = httpContextAccessor.HttpContext?.User
            ?? throw new GraphQLException(
                ErrorBuilder.New()
                    .SetMessage("No HTTP context available.")
                    .SetCode("AUTH_NO_CONTEXT")
                    .Build());

        var subClaim = user.FindFirst(ClaimTypes.NameIdentifier)
                    ?? user.FindFirst("sub")
            ?? throw new GraphQLException(
                ErrorBuilder.New()
                    .SetMessage("Token is missing the 'sub' claim.")
                    .SetCode("AUTH_INVALID_TOKEN")
                    .Build());

        if (!Guid.TryParse(subClaim.Value, out var userId))
            throw new GraphQLException(
                ErrorBuilder.New()
                    .SetMessage("Invalid user id in token.")
                    .SetCode("AUTH_INVALID_TOKEN")
                    .Build());

        var row = await userRepo.FindByIdAsync(userId, cancellationToken)
            ?? throw new GraphQLException(
                ErrorBuilder.New()
                    .SetMessage("User not found.")
                    .SetCode("AUTH_USER_NOT_FOUND")
                    .Build());

        return new UserInfo(row.id, row.email, row.full_name, row.role, row.tenant_id);
    }
}
