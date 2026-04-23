using DashboardService.Application;
using DashboardService.Infrastructure;
using Gateway.Auth;
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

        // HttpContext access (required by AuthQuery.me)
        services.AddHttpContextAccessor();

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
            // Auth schema
            .AddTypeExtension<AuthMutation>()
            .AddTypeExtension<AuthQuery>()
            // Authorization support
            .AddAuthorization()
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
    /// Registers JWT token generation, user repository, data seeder, and
    /// the Redis query cache. Call this after AddGateway().
    /// </summary>
    public static IServiceCollection AddAuthServices(
        this IServiceCollection services,
        string connectionString,
        JwtOptions jwtOptions)
    {
        services.AddSingleton(jwtOptions);
        services.AddScoped<TokenService>();
        services.AddScoped<UserRepository>(_ => new UserRepository(connectionString));
        services.AddScoped<QueryCacheService>();

        // DataSeeder runs once at startup to guarantee demo users exist
        services.AddSingleton(sp =>
            new DataSeeder(connectionString, sp.GetRequiredService<ILogger<DataSeeder>>()));
        services.AddHostedService(sp => sp.GetRequiredService<DataSeeder>());

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
