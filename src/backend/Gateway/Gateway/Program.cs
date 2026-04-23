using Gateway;
using Gateway.Infrastructure;
using IOC.SignalR;

var builder = WebApplication.CreateBuilder(args);

// ─────────────────────────────────────────────────────────────────────────────
// Configuration
// ─────────────────────────────────────────────────────────────────────────────

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Missing 'DefaultConnection' connection string.");

// ─────────────────────────────────────────────────────────────────────────────
// Services
// ─────────────────────────────────────────────────────────────────────────────

builder.Services
    .AddDashboardServices(connectionString)
    .AddGateway()
    .AddRealtimeBridge(builder.Configuration);

// Authentication (JWT Bearer)
builder.Services
    .AddAuthentication()
    .AddJwtBearer();

builder.Services.AddAuthorization();

// CORS — allow Vite dev server + production origins
builder.Services.AddCors(options =>
{
    options.AddPolicy("IOCFrontend", policy =>
    {
        var allowedOrigins = builder.Configuration
            .GetSection("Cors:AllowedOrigins")
            .Get<string[]>()
            ?? ["http://localhost:5173", "http://localhost:3000"];

        policy
            .WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials(); // Required for SignalR WebSocket negotiation
    });
});

builder.Services.AddHealthChecks();

// ─────────────────────────────────────────────────────────────────────────────
// Pipeline
// ─────────────────────────────────────────────────────────────────────────────

var app = builder.Build();

app.UseWebSockets();
app.UseCors("IOCFrontend");
app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<TenantMiddleware>();

// GraphQL endpoint
app.MapGraphQL("/graphql");

// SignalR hubs
app.MapHub<DashboardHub>("/hubs/dashboard");

// Health checks
app.MapHealthChecks("/health");
app.MapGet("/health/ready", () => Results.Ok(new { status = "ready", timestamp = DateTime.UtcNow }));

if (app.Environment.IsDevelopment())
{
    app.MapGet("/", () => Results.Redirect("/graphql"));
}

app.Run();

// Expose for WebApplicationFactory in integration tests
public partial class Program { }
