using Microsoft.Extensions.DependencyInjection;

namespace DashboardService.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddDashboardApplication(this IServiceCollection services)
    {
        services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly));

        return services;
    }
}
