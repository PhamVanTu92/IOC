using Gateway;
using Gateway.Middleware;
using MetadataService.Application;
using MetadataService.Infrastructure;
using QueryService.Application;
using QueryService.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// ─── Configuration ───────────────────────────────────────────────────────────
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is required.");

// ─── Application Layer ───────────────────────────────────────────────────────
builder.Services.AddApplication();          // MetadataService: MediatR + FluentValidation
builder.Services.AddQueryApplication();     // QueryService: MediatR handlers

// ─── Infrastructure Layer ────────────────────────────────────────────────────
builder.Services.AddInfrastructure(connectionString);  // MetadataService: Dapper repos + TypeHandlers

var redisConnectionString = builder.Configuration["Redis:ConnectionString"]
    ?? "localhost:6379";
builder.Services.AddQueryInfrastructure(connectionString, redisConnectionString);

// ─── Gateway (GraphQL + SignalR + TenantContext) ─────────────────────────────
builder.Services.AddGateway();

// ─── Authentication / Authorization ─────────────────────────────────────────
builder.Services.AddAuthentication();
builder.Services.AddAuthorization();

// ─── CORS ────────────────────────────────────────────────────────────────────
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy => policy
        .WithOrigins(
            builder.Configuration["Cors:AllowedOrigins"]?.Split(',')
            ?? ["http://localhost:5173", "http://localhost:3000"])
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials()); // required for SignalR WebSocket
});

// ─── Health Checks ───────────────────────────────────────────────────────────
builder.Services.AddHealthChecks();

// ─── Build App ───────────────────────────────────────────────────────────────
var app = builder.Build();

// ─── Middleware Pipeline ─────────────────────────────────────────────────────
app.UseCors();

app.UseAuthentication();
app.UseAuthorization();

// TenantMiddleware — phải chạy SAU auth để có thể đọc JWT claims
app.UseMiddleware<TenantMiddleware>();

// ─── Endpoints ───────────────────────────────────────────────────────────────

// GraphQL — HotChocolate
app.MapGraphQL("/graphql");

// SignalR Hubs
app.MapHub<Gateway.Hubs.DashboardHub>("/hubs/dashboard");

// Health checks
app.MapHealthChecks("/health");
app.MapHealthChecks("/health/ready", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready")
});

app.Run();

// ─── Partial class for WebApplicationFactory in integration tests ─────────────
public partial class Program { }
