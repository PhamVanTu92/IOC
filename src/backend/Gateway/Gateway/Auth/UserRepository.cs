using Dapper;
using Npgsql;

namespace Gateway.Auth;

// ─────────────────────────────────────────────────────────────────────────────
// UserRow — Dapper row model; properties match DB column names exactly
// ─────────────────────────────────────────────────────────────────────────────

public sealed record UserRow(
    Guid   id,
    Guid   tenant_id,
    string email,
    string password_hash,
    string full_name,
    string role,
    bool   is_active);

// ─────────────────────────────────────────────────────────────────────────────
// UserRepository — Dapper-based data access for the users table
// ─────────────────────────────────────────────────────────────────────────────

public sealed class UserRepository(string connectionString)
{
    private NpgsqlConnection CreateConnection() => new(connectionString);

    /// <summary>
    /// Looks up an active user by e-mail (case-insensitive).
    /// Returns null when not found.
    /// </summary>
    public async Task<UserRow?> FindByEmailAsync(string email, CancellationToken ct)
    {
        await using var conn = CreateConnection();
        return await conn.QuerySingleOrDefaultAsync<UserRow>(
            new CommandDefinition(
                """
                SELECT id, tenant_id, email, password_hash, full_name, role, is_active
                FROM   users
                WHERE  lower(email) = lower(@Email)
                  AND  is_active = true
                """,
                new { Email = email },
                cancellationToken: ct));
    }

    /// <summary>Looks up a user by primary key. Returns null when not found.</summary>
    public async Task<UserRow?> FindByIdAsync(Guid id, CancellationToken ct)
    {
        await using var conn = CreateConnection();
        return await conn.QuerySingleOrDefaultAsync<UserRow>(
            new CommandDefinition(
                """
                SELECT id, tenant_id, email, password_hash, full_name, role, is_active
                FROM   users
                WHERE  id = @Id
                """,
                new { Id = id },
                cancellationToken: ct));
    }

    /// <summary>
    /// Inserts a new user and returns the generated <see cref="Guid"/> primary key.
    /// </summary>
    public async Task<Guid> CreateAsync(
        Guid   tenantId,
        string email,
        string passwordHash,
        string fullName,
        string role,
        CancellationToken ct)
    {
        await using var conn = CreateConnection();
        return await conn.ExecuteScalarAsync<Guid>(
            new CommandDefinition(
                """
                INSERT INTO users (tenant_id, email, password_hash, full_name, role)
                VALUES (@TenantId, @Email, @PasswordHash, @FullName, @Role)
                RETURNING id
                """,
                new { TenantId = tenantId, Email = email, PasswordHash = passwordHash, FullName = fullName, Role = role },
                cancellationToken: ct));
    }

    /// <summary>
    /// Returns <c>true</c> when a user with the given e-mail already exists
    /// (regardless of active flag, to prevent re-registration after deactivation).
    /// </summary>
    public async Task<bool> ExistsByEmailAsync(string email, CancellationToken ct)
    {
        await using var conn = CreateConnection();
        var count = await conn.ExecuteScalarAsync<int>(
            new CommandDefinition(
                "SELECT COUNT(1) FROM users WHERE lower(email) = lower(@Email)",
                new { Email = email },
                cancellationToken: ct));
        return count > 0;
    }
}
