using Dapper;
using MetadataService.Domain.Interfaces;
using MetadataService.Infrastructure.Persistence.Repositories;
using MetadataService.Infrastructure.Persistence.TypeHandlers;
using Microsoft.Extensions.DependencyInjection;

namespace MetadataService.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        string connectionString)
    {
        // Đăng ký Dapper type handlers cho PostgreSQL
        SqlMapper.AddTypeHandler(DateTimeOffsetHandler.Instance);
        SqlMapper.RemoveTypeMap(typeof(DateTimeOffset));
        SqlMapper.RemoveTypeMap(typeof(DateTimeOffset?));

        // Repositories — sử dụng factory để inject connectionString
        services.AddScoped<IDatasetRepository>(_ => new DatasetRepository(connectionString));
        services.AddScoped<IDimensionRepository>(_ => new DimensionRepository(connectionString));
        services.AddScoped<IMeasureRepository>(_ => new MeasureRepository(connectionString));
        services.AddScoped<IMetricRepository>(_ => new MetricRepository(connectionString));

        return services;
    }
}
