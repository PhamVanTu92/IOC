using DashboardService.Application;
using DashboardService.Infrastructure;
using Gateway.Infrastructure;
using Gateway.Schema.Mutations;
using Gateway.Schema.Queries;
using Gateway.Schema.Types;
using IOC.SignalR;

namespace Gateway;

// ─────────────────────────────────────────────────────────────────────────────
// DependencyInjection — wires all services needed by the Gateway
// ─────────────────────────────────────────────────────────────────────────────

public static class DependencyInjection
{
    public static IServiceCollection AddGateway(this IServiceCollection services)
    {
        // Tenant resolution (scoped per request)
        services.AddScoped<TenantContext>();

        // HotChocolate GraphQL
        // Include full exception details when explicitly enabled (dev/debug) or in Development env
        var includeExceptionDetails =
            Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development" ||
            Environment.GetEnvironmentVariable("GraphQL__IncludeExceptionDetails") == "true";

        services
            .AddGraphQLServer()
            .AddQueryType(d => d.Name("Query"))
            .AddMutationType(d => d.Name("Mutation"))
            // Dashboard schema
            .AddTypeExtension<DashboardQuery>()
            .AddTypeExtension<DashboardMutation>()
            .AddType<DashboardType>()
            .AddType<DashboardSummaryType>()
            // Error handling
            .AddErrorFilter<GraphQLErrorFilter>()
            .ModifyRequestOptions(opt =>
                opt.IncludeExceptionDetails = includeExceptionDetails);

        // SignalR
        services.AddSignalR(options =>
        {
            options.EnableDetailedErrors =
                Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development";
        });

        // DashboardNotifier — used by application code for imperative pushes
        services.AddSingleton<DashboardNotifier>();

        return services;
    }

    public static IServiceCollection AddDashboardServices(
        this IServiceCollection services,
        string connectionString)
    {
        services
            .AddDashboardApplication()
            .AddDashboardInfrastructure(connectionString);

        return services;
    }

    /// <summary>
    /// Register the Kafka→SignalR bridge and its configuration.
    /// Call after AddGateway() so SignalR + IHubContext are already registered.
    /// </summary>
    public static IServiceCollection AddRealtimeBridge(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Bind options from "RealtimeBridge" section (or use defaults)
        var options = new RealtimeBridgeOptions();
        configuration.GetSection("RealtimeBridge").Bind(options);
        services.AddSingleton(options);

        // Register as IHostedService — starts consuming Kafka on app startup
        services.AddHostedService<RealtimeBridgeService>();

        return services;
    }
}
