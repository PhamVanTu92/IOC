using Gateway.Infrastructure;
using Gateway.Middleware;
using Gateway.Schema.Mutations;
using Gateway.Schema.Queries;
using Gateway.Schema.Types;

namespace Gateway;

public static class DependencyInjection
{
    /// <summary>
    /// Đăng ký tất cả Gateway services: TenantContext, HotChocolate GraphQL, SignalR.
    /// </summary>
    public static IServiceCollection AddGateway(this IServiceCollection services)
    {
        // Tenant context — scoped per request
        services.AddScoped<TenantContext>();

        // HotChocolate GraphQL Server
        services
            .AddGraphQLServer()
            // ─── Object Types ────────────────────────────────────────────────
            .AddType<DatasetType>()
            .AddType<DimensionType>()
            .AddType<MeasureType>()
            .AddType<MetricType>()
            .AddType<QueryResultType>()
            .AddType<QueryResultColumnType>()
            .AddType<QueryExecutionMetadataType>()
            // ─── Query resolvers ─────────────────────────────────────────────
            .AddTypeExtension<MetadataQuery>()
            .AddTypeExtension<QueryExecutionQuery>()
            // ─── Mutation resolvers ──────────────────────────────────────────
            .AddTypeExtension<MetadataMutation>()
            // ─── Query root (bắt buộc khi chỉ dùng AddTypeExtension) ────────
            .AddQueryType(d => d.Name(OperationTypeNames.Query))
            .AddMutationType(d => d.Name(OperationTypeNames.Mutation))
            // ─── Features ────────────────────────────────────────────────────
            .AddFiltering()
            .AddSorting()
            .AddProjections()
            // ─── Error handling ──────────────────────────────────────────────
            .AddErrorFilter<GraphQLErrorFilter>()
            // ─── Schema options ──────────────────────────────────────────────
            .ModifyRequestOptions(opt =>
            {
                opt.IncludeExceptionDetails = true; // chỉ true ở dev, override trong production
            });

        // SignalR
        services.AddSignalR();

        return services;
    }
}
