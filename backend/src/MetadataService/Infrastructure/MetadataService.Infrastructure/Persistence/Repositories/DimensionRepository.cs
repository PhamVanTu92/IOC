using Dapper;
using MetadataService.Domain.Entities;
using MetadataService.Domain.Interfaces;
using Npgsql;

namespace MetadataService.Infrastructure.Persistence.Repositories;

public sealed class DimensionRepository : IDimensionRepository
{
    private readonly string _connectionString;
    public DimensionRepository(string connectionString) => _connectionString = connectionString;
    private NpgsqlConnection CreateConnection() => new(_connectionString);

    private const string SelectColumns = """
        id, dataset_id, tenant_id, name, display_name, description,
        column_name, custom_sql_expression, data_type, format,
        is_time_dimension, default_granularity, sort_order, is_active, created_at
        """;

    public async Task<Dimension?> GetByIdAsync(Guid id, Guid tenantId, CancellationToken ct = default)
    {
        var sql = $"SELECT {SelectColumns} FROM dimensions WHERE id = @id AND tenant_id = @tenantId";
        await using var conn = CreateConnection();
        return await conn.QuerySingleOrDefaultAsync<Dimension>(
            new CommandDefinition(sql, new { id, tenantId }, cancellationToken: ct));
    }

    public async Task<IReadOnlyList<Dimension>> ListByDatasetAsync(
        Guid datasetId, Guid tenantId, CancellationToken ct = default)
    {
        var sql = $"""
            SELECT {SelectColumns} FROM dimensions
            WHERE dataset_id = @datasetId AND tenant_id = @tenantId AND is_active = TRUE
            ORDER BY sort_order ASC, name ASC
            """;
        await using var conn = CreateConnection();
        var result = await conn.QueryAsync<Dimension>(
            new CommandDefinition(sql, new { datasetId, tenantId }, cancellationToken: ct));
        return result.ToList();
    }

    public async Task<Dimension> CreateAsync(Dimension dimension, CancellationToken ct = default)
    {
        const string sql = """
            INSERT INTO dimensions
                (id, dataset_id, tenant_id, name, display_name, description,
                 column_name, custom_sql_expression, data_type, format,
                 is_time_dimension, default_granularity, sort_order, is_active, created_at)
            VALUES
                (@Id, @DatasetId, @TenantId, @Name, @DisplayName, @Description,
                 @ColumnName, @CustomSqlExpression, @DataType, @Format,
                 @IsTimeDimension, @DefaultGranularity, @SortOrder, @IsActive, @CreatedAt)
            RETURNING id, dataset_id, tenant_id, name, display_name, description,
                      column_name, custom_sql_expression, data_type, format,
                      is_time_dimension, default_granularity, sort_order, is_active, created_at
            """;
        await using var conn = CreateConnection();
        return await conn.QuerySingleAsync<Dimension>(
            new CommandDefinition(sql, dimension, cancellationToken: ct));
    }

    public async Task<Dimension> UpdateAsync(Dimension dimension, CancellationToken ct = default)
    {
        const string sql = """
            UPDATE dimensions
            SET display_name = @DisplayName, description = @Description,
                data_type = @DataType, format = @Format,
                is_time_dimension = @IsTimeDimension,
                default_granularity = @DefaultGranularity,
                custom_sql_expression = @CustomSqlExpression,
                sort_order = @SortOrder, is_active = @IsActive
            WHERE id = @Id AND tenant_id = @TenantId
            RETURNING id, dataset_id, tenant_id, name, display_name, description,
                      column_name, custom_sql_expression, data_type, format,
                      is_time_dimension, default_granularity, sort_order, is_active, created_at
            """;
        await using var conn = CreateConnection();
        return await conn.QuerySingleAsync<Dimension>(
            new CommandDefinition(sql, dimension, cancellationToken: ct));
    }

    public async Task DeleteAsync(Guid id, Guid tenantId, CancellationToken ct = default)
    {
        const string sql = "DELETE FROM dimensions WHERE id = @id AND tenant_id = @tenantId";
        await using var conn = CreateConnection();
        await conn.ExecuteAsync(new CommandDefinition(sql, new { id, tenantId }, cancellationToken: ct));
    }

    public async Task<bool> ExistsByNameAsync(string name, Guid datasetId, Guid tenantId, CancellationToken ct = default)
    {
        const string sql = """
            SELECT EXISTS(SELECT 1 FROM dimensions
                WHERE LOWER(name) = LOWER(@name) AND dataset_id = @datasetId AND tenant_id = @tenantId)
            """;
        await using var conn = CreateConnection();
        return await conn.ExecuteScalarAsync<bool>(
            new CommandDefinition(sql, new { name, datasetId, tenantId }, cancellationToken: ct));
    }
}
