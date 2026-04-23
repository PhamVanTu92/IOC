using Dapper;
using MetadataService.Domain.Entities;
using MetadataService.Domain.Interfaces;
using Npgsql;

namespace MetadataService.Infrastructure.Persistence.Repositories;

public sealed class DatasetRepository : IDatasetRepository
{
    private readonly string _connectionString;

    public DatasetRepository(string connectionString)
    {
        _connectionString = connectionString;
    }

    private NpgsqlConnection CreateConnection() => new(_connectionString);

    public async Task<Dataset?> GetByIdAsync(Guid id, Guid tenantId, CancellationToken ct = default)
    {
        const string sql = """
            SELECT id, tenant_id, name, description, source_type,
                   schema_name, table_name, custom_sql,
                   is_active, created_at, updated_at, created_by
            FROM datasets
            WHERE id = @id AND tenant_id = @tenantId
            """;

        await using var conn = CreateConnection();
        return await conn.QuerySingleOrDefaultAsync<Dataset>(
            new CommandDefinition(sql, new { id, tenantId }, cancellationToken: ct));
    }

    public async Task<Dataset?> GetByNameAsync(string name, Guid tenantId, CancellationToken ct = default)
    {
        const string sql = """
            SELECT id, tenant_id, name, description, source_type,
                   schema_name, table_name, custom_sql,
                   is_active, created_at, updated_at, created_by
            FROM datasets
            WHERE LOWER(name) = LOWER(@name) AND tenant_id = @tenantId
            """;

        await using var conn = CreateConnection();
        return await conn.QuerySingleOrDefaultAsync<Dataset>(
            new CommandDefinition(sql, new { name, tenantId }, cancellationToken: ct));
    }

    public async Task<IReadOnlyList<Dataset>> ListAsync(
        Guid tenantId,
        bool includeInactive = false,
        CancellationToken ct = default)
    {
        var sql = """
            SELECT id, tenant_id, name, description, source_type,
                   schema_name, table_name, custom_sql,
                   is_active, created_at, updated_at, created_by
            FROM datasets
            WHERE tenant_id = @tenantId
            """ + (includeInactive ? "" : " AND is_active = TRUE") +
            " ORDER BY name ASC";

        await using var conn = CreateConnection();
        var results = await conn.QueryAsync<Dataset>(
            new CommandDefinition(sql, new { tenantId }, cancellationToken: ct));
        return results.ToList();
    }

    public async Task<Dataset> CreateAsync(Dataset dataset, CancellationToken ct = default)
    {
        const string sql = """
            INSERT INTO datasets
                (id, tenant_id, name, description, source_type,
                 schema_name, table_name, custom_sql, is_active, created_at, updated_at, created_by)
            VALUES
                (@Id, @TenantId, @Name, @Description, @SourceType,
                 @SchemaName, @TableName, @CustomSql, @IsActive, @CreatedAt, @UpdatedAt, @CreatedBy)
            RETURNING id, tenant_id, name, description, source_type,
                      schema_name, table_name, custom_sql, is_active, created_at, updated_at, created_by
            """;

        await using var conn = CreateConnection();
        return await conn.QuerySingleAsync<Dataset>(
            new CommandDefinition(sql, dataset, cancellationToken: ct));
    }

    public async Task<Dataset> UpdateAsync(Dataset dataset, CancellationToken ct = default)
    {
        const string sql = """
            UPDATE datasets
            SET name = @Name,
                description = @Description,
                schema_name = @SchemaName,
                table_name = @TableName,
                custom_sql = @CustomSql,
                is_active = @IsActive,
                updated_at = NOW()
            WHERE id = @Id AND tenant_id = @TenantId
            RETURNING id, tenant_id, name, description, source_type,
                      schema_name, table_name, custom_sql, is_active, created_at, updated_at, created_by
            """;

        await using var conn = CreateConnection();
        return await conn.QuerySingleAsync<Dataset>(
            new CommandDefinition(sql, dataset, cancellationToken: ct));
    }

    public async Task DeleteAsync(Guid id, Guid tenantId, CancellationToken ct = default)
    {
        const string sql = "DELETE FROM datasets WHERE id = @id AND tenant_id = @tenantId";
        await using var conn = CreateConnection();
        await conn.ExecuteAsync(new CommandDefinition(sql, new { id, tenantId }, cancellationToken: ct));
    }

    public async Task<bool> ExistsAsync(Guid id, Guid tenantId, CancellationToken ct = default)
    {
        const string sql = "SELECT EXISTS(SELECT 1 FROM datasets WHERE id = @id AND tenant_id = @tenantId)";
        await using var conn = CreateConnection();
        return await conn.ExecuteScalarAsync<bool>(
            new CommandDefinition(sql, new { id, tenantId }, cancellationToken: ct));
    }

    public async Task<bool> ExistsByNameAsync(string name, Guid tenantId, CancellationToken ct = default)
    {
        const string sql = """
            SELECT EXISTS(
                SELECT 1 FROM datasets
                WHERE LOWER(name) = LOWER(@name) AND tenant_id = @tenantId
            )
            """;
        await using var conn = CreateConnection();
        return await conn.ExecuteScalarAsync<bool>(
            new CommandDefinition(sql, new { name, tenantId }, cancellationToken: ct));
    }
}
