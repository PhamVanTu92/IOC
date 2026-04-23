using HotChocolate;
using HotChocolate.Types;

namespace Gateway.Auth;

// ─────────────────────────────────────────────────────────────────────────────
// Auth output types
// ─────────────────────────────────────────────────────────────────────────────

public sealed record UserInfo(
    Guid   Id,
    string Email,
    string FullName,
    string Role,
    Guid   TenantId);

public sealed record AuthPayload(
    string   Token,
    DateTime ExpiresAt,
    UserInfo User);

// ─────────────────────────────────────────────────────────────────────────────
// AuthMutation — login + register GraphQL mutations
// ─────────────────────────────────────────────────────────────────────────────

[ExtendObjectType("Mutation")]
public sealed class AuthMutation
{
    /// <summary>
    /// Authenticates a user with e-mail + password.
    /// Returns a signed JWT on success; throws on invalid credentials.
    /// </summary>
    public async Task<AuthPayload> LoginAsync(
        string email,
        string password,
        [Service] UserRepository userRepo,
        [Service] TokenService tokenService,
        CancellationToken cancellationToken)
    {
        var user = await userRepo.FindByEmailAsync(email, cancellationToken);

        if (user is null || !PasswordHasher.Verify(password, user.password_hash))
            throw new GraphQLException(
                ErrorBuilder.New()
                    .SetMessage("Invalid email or password.")
                    .SetCode("AUTH_INVALID_CREDENTIALS")
                    .Build());

        if (!user.is_active)
            throw new GraphQLException(
                ErrorBuilder.New()
                    .SetMessage("Account is disabled.")
                    .SetCode("AUTH_ACCOUNT_DISABLED")
                    .Build());

        var token     = tokenService.GenerateAccessToken(user.id, user.tenant_id, user.email, user.role);
        var expiresAt = tokenService.GetExpiresAt();

        return new AuthPayload(
            token,
            expiresAt,
            new UserInfo(user.id, user.email, user.full_name, user.role, user.tenant_id));
    }

    /// <summary>
    /// Registers a new user under the default demo tenant.
    /// Throws when the e-mail is already taken.
    /// </summary>
    public async Task<AuthPayload> RegisterAsync(
        string email,
        string password,
        string fullName,
        [Service] UserRepository userRepo,
        [Service] TokenService tokenService,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(email))
            throw new GraphQLException(
                ErrorBuilder.New()
                    .SetMessage("Email is required.")
                    .SetCode("REGISTER_INVALID_INPUT")
                    .Build());

        if (password.Length < 6)
            throw new GraphQLException(
                ErrorBuilder.New()
                    .SetMessage("Password must be at least 6 characters.")
                    .SetCode("REGISTER_INVALID_INPUT")
                    .Build());

        var exists = await userRepo.ExistsByEmailAsync(email, cancellationToken);
        if (exists)
            throw new GraphQLException(
                ErrorBuilder.New()
                    .SetMessage("A user with that email already exists.")
                    .SetCode("REGISTER_DUPLICATE_EMAIL")
                    .Build());

        var demoTenantId = new Guid("00000000-0000-0000-0000-000000000001");
        var hash         = PasswordHasher.Hash(password);

        var newId = await userRepo.CreateAsync(
            demoTenantId, email, hash, fullName, "viewer", cancellationToken);

        var token     = tokenService.GenerateAccessToken(newId, demoTenantId, email, "viewer");
        var expiresAt = tokenService.GetExpiresAt();

        return new AuthPayload(
            token,
            expiresAt,
            new UserInfo(newId, email, fullName, "viewer", demoTenantId));
    }
}
