using Dapper;
using MetadataService.Domain.Entities;
using MetadataService.Domain.Interfaces;
using Npgsql;

namespace MetadataService.Infrastructure.Persistence.Repositories;

public sealed class MeasureRepository : IMeasureRepository
{
    private readonly string _connectionString;
    public MeasureRepository(string connectionString) => _connectionString = connectionString;
    private NpgsqlConnection CreateConnection() => new(_connectionString);

    private const string SelectColumns = """
        id, dataset_id, tenant_id, name, display_name, description,
        column_name, custom_sql_expression, aggregation_type, data_type,
        format, filter_expression, sort_order, is_active, created_at
        """;

    public async Task<Measure?> GetByIdAsync(Guid id, Guid tenantId, CancellationToken ct = default)
    {
        var sql = $"SELECT {SelectColumns} FROM measures WHERE id = @id AND tenant_id = @tenantId";
        await using var conn = CreateConnection();
        return await conn.QuerySingleOrDefaultAsync<Measure>(
            new CommandDefinition(sql, new { id, tenantId }, cancellationToken: ct));
    }

    public async Task<IReadOnlyList<Measure>> ListByDatasetAsync(
        Guid datasetId, Guid tenantId, CancellationToken ct = default)
    {
        var sql = $"""
            SELECT {SelectColumns} FROM measures
            WHERE dataset_id = @datasetId AND tenant_id = @tenantId AND is_active = TRUE
            ORDER BY sort_order ASC, name ASC
            """;
        await using var conn = CreateConnection();
        var result = await conn.QueryAsync<Measure>(
            new CommandDefinition(sql, new { datasetId, tenantId }, cancellationToken: ct));
        return result.ToList();
    }

    public async Task<Measure> CreateAsync(Measure measure, CancellationToken ct = default)
    {
        const string sql = """
            INSERT INTO measures
                (id, dataset_id, tenant_id, name, display_name, description,
                 column_name, custom_sql_expression, aggregation_type, data_type,
                 format, filter_expression, sort_order, is_active, created_at)
            VALUES
                (@Id, @DatasetId, @TenantId, @Name, @DisplayName, @Description,
                 @ColumnName, @CustomSqlExpression, @AggregationType, @DataType,
                 @Format, @FilterExpression, @SortOrder, @IsActive, @CreatedAt)
            RETURNING id, dataset_id, tenant_id, name, display_name, description,
                      column_name, custom_sql_expression, aggregation_type, data_type,
                      format, filter_expression, sort_order, is_active, created_at
            """;
        await using var conn = CreateConnection();
        return await conn.QuerySingleAsync<Measure>(
            new CommandDefinition(sql, measure, cancellationToken: ct));
    }

    public async Task<Measure> UpdateAsync(Measure measure, CancellationToken ct = default)
    {
        const string sql = """
            UPDATE measures
            SET display_name = @DisplayName, description = @Description,
                aggregation_type = @AggregationType, data_type = @DataType,
                format = @Format, filter_expression = @FilterExpression,
                custom_sql_expression = @CustomSqlExpression,
                sort_order = @SortOrder, is_active = @IsActive
            WHERE id = @Id AND tenant_id = @TenantId
            RETURNING id, dataset_id, tenant_id, name, display_name, description,
                      column_name, custom_sql_expression, aggregation_type, data_type,
                      format, filter_expression, sort_order, is_active, created_at
            """;
        await using var conn = CreateConnection();
        return await conn.QuerySingleAsync<Measure>(
            new CommandDefinition(sql, measure, cancellationToken: ct));
    }

    public async Task DeleteAsync(Guid id, Guid tenantId, CancellationToken ct = default)
    {
        const string sql = "DELETE FROM measures WHERE id = @id AND tenant_id = @tenantId";
        await using var conn = CreateConnection();
        await conn.ExecuteAsync(new CommandDefinition(sql, new { id, tenantId }, cancellationToken: ct));
    }

    public async Task<bool> ExistsByNameAsync(string name, Guid datasetId, Guid tenantId, CancellationToken ct = default)
    {
        const string sql = """
            SELECT EXISTS(SELECT 1 FROM measures
                WHERE LOWER(name) = LOWER(@name) AND dataset_id = @datasetId AND tenant_id = @tenantId)
            """;
        await using var conn = CreateConnection();
        return await conn.ExecuteScalarAsync<bool>(
            new CommandDefinition(sql, new { name, datasetId, tenantId }, cancellationToken: ct));
    }
}
