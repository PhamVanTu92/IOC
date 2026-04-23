using Dapper;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace Gateway.Auth;

// ─────────────────────────────────────────────────────────────────────────────
// DataSeeder — IHostedService that ensures demo tenant + users exist at startup
// ─────────────────────────────────────────────────────────────────────────────

public sealed class DataSeeder(
    string connectionString,
    ILogger<DataSeeder> logger) : IHostedService
{
    private static readonly Guid DemoTenantId =
        new("00000000-0000-0000-0000-000000000001");

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("DataSeeder: starting seed check…");

        await using var conn = new NpgsqlConnection(connectionString);
        await conn.OpenAsync(cancellationToken);

        await EnsureTenantAsync(conn, cancellationToken);
        await EnsureUserAsync(conn, "admin@ioc.local", "Admin@123", "IOC Admin",  "admin",  cancellationToken);
        await EnsureUserAsync(conn, "user@ioc.local",  "User@123",  "IOC Editor", "editor", cancellationToken);

        logger.LogInformation("DataSeeder: seed check complete.");
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    // ── Helpers ──────────────────────────────────────────────────────────────

    private async Task EnsureTenantAsync(NpgsqlConnection conn, CancellationToken ct)
    {
        var exists = await conn.ExecuteScalarAsync<bool>(
            new CommandDefinition(
                "SELECT EXISTS(SELECT 1 FROM tenants WHERE id = @Id)",
                new { Id = DemoTenantId },
                cancellationToken: ct));

        if (!exists)
        {
            await conn.ExecuteAsync(new CommandDefinition(
                """
                INSERT INTO tenants (id, name, slug, is_active)
                VALUES (@Id, 'IOC Demo', 'ioc-demo', true)
                ON CONFLICT (id) DO NOTHING
                """,
                new { Id = DemoTenantId },
                cancellationToken: ct));

            logger.LogInformation("DataSeeder: created demo tenant {TenantId}", DemoTenantId);
        }
    }

    private async Task EnsureUserAsync(
        NpgsqlConnection conn,
        string email,
        string plainPassword,
        string fullName,
        string role,
        CancellationToken ct)
    {
        var exists = await conn.ExecuteScalarAsync<bool>(
            new CommandDefinition(
                "SELECT EXISTS(SELECT 1 FROM users WHERE lower(email) = lower(@Email))",
                new { Email = email },
                cancellationToken: ct));

        if (!exists)
        {
            var hash = PasswordHasher.Hash(plainPassword);

            await conn.ExecuteAsync(new CommandDefinition(
                """
                INSERT INTO users (tenant_id, email, password_hash, full_name, role, is_active)
                VALUES (@TenantId, @Email, @Hash, @FullName, @Role, true)
                ON CONFLICT (tenant_id, email) DO NOTHING
                """,
                new
                {
                    TenantId = DemoTenantId,
                    Email    = email,
                    Hash     = hash,
                    FullName = fullName,
                    Role     = role,
                },
                cancellationToken: ct));

            logger.LogInformation("DataSeeder: created user {Email} ({Role})", email, role);
        }
    }
}
