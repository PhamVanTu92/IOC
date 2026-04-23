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
            // Dataset schema (semantic layer registry)
            .AddTypeExtension<DatasetQuery>()
            .AddType<DatasetSummaryType>()
            .AddType<DatasetDetailType>()
            // Semantic Layer query execution
            .AddTypeExtension<SemanticQuery>()
            .AddType<QueryResultType>()
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

    public static IServiceCollection AddRealtimeBridge(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var options = new RealtimeBridgeOptions();
        configuration.GetSection("RealtimeBridge").Bind(options);
        services.AddSingleton(options);

        services.AddHostedService<RealtimeBridgeService>();

        return services;
    }
}
