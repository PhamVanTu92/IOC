using DashboardService.Domain;
using DashboardService.Infrastructure.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace DashboardService.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddDashboardInfrastructure(
        this IServiceCollection services,
        string connectionString)
    {
        services.AddScoped<IDashboardRepository>(sp =>
            new DashboardRepository(
                connectionString,
                sp.GetRequiredService<ILogger<DashboardRepository>>()));

        return services;
    }
}
