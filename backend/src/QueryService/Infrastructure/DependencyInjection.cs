using Microsoft.Extensions.DependencyInjection;
using QueryService.Application.Interfaces;
using QueryService.Infrastructure.Cache;
using QueryService.Infrastructure.Executor;
using QueryService.Infrastructure.SemanticLoader;
using StackExchange.Redis;

namespace QueryService.Infrastructure;

public static class DependencyInjection
{
    /// <summary>
    /// Đăng ký QueryService Infrastructure: SemanticLoader, QueryExecutor, CacheService.
    /// </summary>
    /// <param name="services">DI container</param>
    /// <param name="connectionString">PostgreSQL connection string</param>
    /// <param name="redisConnectionString">Redis connection string (vd: "localhost:6379")</param>
    public static IServiceCollection AddQueryInfrastructure(
        this IServiceCollection services,
        string connectionString,
        string redisConnectionString)
    {
        // Semantic dataset loader — đọc từ PostgreSQL trực tiếp qua Dapper
        services.AddScoped<ISemanticDatasetLoader>(
            _ => new SemanticDatasetLoader(connectionString));

        // Query executor — thực thi SQL qua Dapper + Npgsql
        services.AddScoped<IQueryExecutor>(
            _ => new DapperQueryExecutor(connectionString));

        // Redis connection — singleton (StackExchange.Redis khuyến khích singleton)
        services.AddSingleton<IConnectionMultiplexer>(
            _ => ConnectionMultiplexer.Connect(redisConnectionString));

        // Cache service — dùng Redis connection
        services.AddScoped<ICacheService, RedisCacheService>();

        return services;
    }
}
