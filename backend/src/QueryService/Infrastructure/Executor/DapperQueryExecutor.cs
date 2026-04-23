using System.Diagnostics;
using Dapper;
using Npgsql;
using QueryService.Application.Interfaces;
using SemanticEngine.Builder;

namespace QueryService.Infrastructure.Executor;

/// <summary>
/// Dapper + Npgsql implementation của IQueryExecutor.
/// Thực thi parameterized SQL và trả về raw rows.
/// </summary>
public sealed class DapperQueryExecutor : IQueryExecutor
{
    private readonly string _connectionString;

    public DapperQueryExecutor(string connectionString)
        => _connectionString = connectionString;

    public async Task<QueryExecutionResult> ExecuteAsync(
        SqlQueryResult query, CancellationToken ct = default)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync(ct);

        // Convert parameters sang DynamicParameters để Dapper dùng với Npgsql
        var dynParams = BuildDynamicParameters(query.Parameters);

        var sw = Stopwatch.StartNew();

        // Thực thi main query và count query song song
        var rowsTask = ExecuteMainQueryAsync(conn, query.Sql, dynParams, ct);
        var countTask = ExecuteCountQueryAsync(conn, query.CountSql, dynParams, ct);

        await Task.WhenAll(rowsTask, countTask);
        sw.Stop();

        var rows = await rowsTask;
        var totalRows = await countTask;

        return new QueryExecutionResult
        {
            Rows = rows,
            TotalRows = totalRows,
            ExecutionTimeMs = sw.ElapsedMilliseconds
        };
    }

    private static async Task<IReadOnlyList<IReadOnlyDictionary<string, object?>>> ExecuteMainQueryAsync(
        NpgsqlConnection conn, string sql, DynamicParameters dynParams, CancellationToken ct)
    {
        var results = await conn.QueryAsync(
            new CommandDefinition(sql, dynParams, cancellationToken: ct));

        return results
            .Select(row => (IReadOnlyDictionary<string, object?>)
                ((IDictionary<string, object>)row)
                .ToDictionary(kv => kv.Key, kv => (object?)kv.Value))
            .ToList();
    }

    private static async Task<int> ExecuteCountQueryAsync(
        NpgsqlConnection conn, string? countSql, DynamicParameters dynParams, CancellationToken ct)
    {
        if (countSql is null) return 0;

        try
        {
            return await conn.ExecuteScalarAsync<int>(
                new CommandDefinition(countSql, dynParams, cancellationToken: ct));
        }
        catch
        {
            // Count query failure không nên block main result
            return 0;
        }
    }

    private static DynamicParameters BuildDynamicParameters(
        IReadOnlyDictionary<string, object?> parameters)
    {
        var dynParams = new DynamicParameters();
        foreach (var (key, value) in parameters)
        {
            // Bỏ tiền tố "@" vì DynamicParameters tự thêm
            var paramName = key.TrimStart('@');
            dynParams.Add(paramName, value);
        }
        return dynParams;
    }
}
