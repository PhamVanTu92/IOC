using Dapper;
using MetadataService.Domain.Entities;
using MetadataService.Domain.Interfaces;
using Npgsql;

namespace MetadataService.Infrastructure.Persistence.Repositories;

public sealed class MetricRepository : IMetricRepository
{
    private readonly string _connectionString;
    public MetricRepository(string connectionString) => _connectionString = connectionString;
    private NpgsqlConnection CreateConnection() => new(_connectionString);

    private const string SelectColumns = """
        id, dataset_id, tenant_id, name, display_name, description,
        expression, data_type, format, depends_on_measures, sort_order, is_active, created_at
        """;

    public async Task<Metric?> GetByIdAsync(Guid id, Guid tenantId, CancellationToken ct = default)
    {
        var sql = $"SELECT {SelectColumns} FROM metrics WHERE id = @id AND tenant_id = @tenantId";
        await using var conn = CreateConnection();
        return await conn.QuerySingleOrDefaultAsync<Metric>(
            new CommandDefinition(sql, new { id, tenantId }, cancellationToken: ct));
    }

    public async Task<IReadOnlyList<Metric>> ListByDatasetAsync(
        Guid datasetId, Guid tenantId, CancellationToken ct = default)
    {
        var sql = $"""
            SELECT {SelectColumns} FROM metrics
            WHERE dataset_id = @datasetId AND tenant_id = @tenantId AND is_active = TRUE
            ORDER BY sort_order ASC, name ASC
            """;
        await using var conn = CreateConnection();
        var result = await conn.QueryAsync<Metric>(
            new CommandDefinition(sql, new { datasetId, tenantId }, cancellationToken: ct));
        return result.ToList();
    }

    public async Task<Metric> CreateAsync(Metric metric, CancellationToken ct = default)
    {
        const string sql = """
            INSERT INTO metrics
                (id, dataset_id, tenant_id, name, display_name, description,
                 expression, data_type, format, depends_on_measures, sort_order, is_active, created_at)
            VALUES
                (@Id, @DatasetId, @TenantId, @Name, @DisplayName, @Description,
                 @Expression, @DataType, @Format, @DependsOnMeasures, @SortOrder, @IsActive, @CreatedAt)
            RETURNING id, dataset_id, tenant_id, name, display_name, description,
                      expression, data_type, format, depends_on_measures, sort_order, is_active, created_at
            """;
        await using var conn = CreateConnection();
        return await conn.QuerySingleAsync<Metric>(
            new CommandDefinition(sql, metric, cancellationToken: ct));
    }

    public async Task<Metric> UpdateAsync(Metric metric, CancellationToken ct = default)
    {
        const string sql = """
            UPDATE metrics
            SET display_name = @DisplayName, description = @Description,
                expression = @Expression, data_type = @DataType, format = @Format,
                depends_on_measures = @DependsOnMeasures,
                sort_order = @SortOrder, is_active = @IsActive
            WHERE id = @Id AND tenant_id = @TenantId
            RETURNING id, dataset_id, tenant_id, name, display_name, description,
                      expression, data_type, format, depends_on_measures, sort_order, is_active, created_at
            """;
        await using var conn = CreateConnection();
        return await conn.QuerySingleAsync<Metric>(
            new CommandDefinition(sql, metric, cancellationToken: ct));
    }

    public async Task DeleteAsync(Guid id, Guid tenantId, CancellationToken ct = default)
    {
        const string sql = "DELETE FROM metrics WHERE id = @id AND tenant_id = @tenantId";
        await using var conn = CreateConnection();
        await conn.ExecuteAsync(new CommandDefinition(sql, new { id, tenantId }, cancellationToken: ct));
    }

    public async Task<bool> ExistsByNameAsync(string name, Guid datasetId, Guid tenantId, CancellationToken ct = default)
    {
        const string sql = """
            SELECT EXISTS(SELECT 1 FROM metrics
                WHERE LOWER(name) = LOWER(@name) AND dataset_id = @datasetId AND tenant_id = @tenantId)
            """;
        await using var conn = CreateConnection();
        return await conn.ExecuteScalarAsync<bool>(
            new CommandDefinition(sql, new { name, datasetId, tenantId }, cancellationToken: ct));
    }
}
