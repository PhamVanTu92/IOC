using IOC.Core.Plugins;
using IOC.Finance;
using IOC.HR;
using IOC.Marketing;
using IOC.SemanticLayer.Metrics;

var builder = WebApplication.CreateBuilder(args);

// ─────────────────────────────────────────────────────────────────────────────
// Plugin Host — đăng ký tất cả plugins
// ─────────────────────────────────────────────────────────────────────────────

builder.Services.AddLogging();
builder.Services.AddSingleton<PluginHost>();
builder.Services.AddSingleton<SemanticLayerRegistry>();

// Đăng ký plugins
builder.Services
    .AddPlugin<FinancePlugin>()
    .AddPlugin<HRPlugin>()
    .AddPlugin<MarketingPlugin>();

// ─────────────────────────────────────────────────────────────────────────────
// GraphQL — HotChocolate
// ─────────────────────────────────────────────────────────────────────────────

var graphqlBuilder = builder.Services
    .AddGraphQLServer()
    .AddQueryType(d => d.Name("Query"))
    .AddMutationType(d => d.Name("Mutation"))
    .AddSubscriptionType(d => d.Name("Subscription"))
    .AddInMemorySubscriptions()
    .ModifyRequestOptions(opt =>
    {
        opt.IncludeExceptionDetails = builder.Environment.IsDevelopment();
    });

// Cho phép plugins đăng ký GraphQL types
using (var sp = builder.Services.BuildServiceProvider())
{
    var pluginHost = sp.GetRequiredService<PluginHost>();
    var plugins = sp.GetServices<IPlugin>();
    foreach (var plugin in plugins)
        pluginHost.Register(plugin);

    pluginHost.ConfigureGraphQL(graphqlBuilder);
}

// ─────────────────────────────────────────────────────────────────────────────
// SignalR
// ─────────────────────────────────────────────────────────────────────────────

builder.Services.AddSignalR();

// ─────────────────────────────────────────────────────────────────────────────
// CORS (cho frontend Vite dev server)
// ─────────────────────────────────────────────────────────────────────────────

builder.Services.AddCors(options =>
{
    options.AddPolicy("IOCFrontend", policy =>
    {
        policy
            .WithOrigins("http://localhost:5173", "http://localhost:3000")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials(); // Required for SignalR
    });
});

// ─────────────────────────────────────────────────────────────────────────────

var app = builder.Build();

app.UseCors("IOCFrontend");
app.UseWebSockets();

// GraphQL endpoint
app.MapGraphQL("/graphql");

// SignalR hubs
// app.MapHub<DashboardHub>("/hubs/dashboard");
// app.MapHub<AlertHub>("/hubs/alerts");

// Health checks
app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }));
app.MapGet("/health/ready", async (IServiceProvider sp) =>
{
    // TODO: check DB, Kafka connectivity
    return Results.Ok(new { status = "ready" });
});

if (app.Environment.IsDevelopment())
{
    app.MapGet("/", () => Results.Redirect("/graphql"));
}

app.Run();

// Expose Program for integration tests
public partial class Program { }
