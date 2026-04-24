using System.Text;
using Gateway;
using Gateway.Auth;
using Gateway.Infrastructure;
using IOC.SignalR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// ─────────────────────────────────────────────────────────────────────────────
// Configuration
// ─────────────────────────────────────────────────────────────────────────────

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Missing 'DefaultConnection' connection string.");

var jwtOptions = builder.Configuration
    .GetSection(JwtOptions.SectionName)
    .Get<JwtOptions>()
    ?? throw new InvalidOperationException("Missing 'Jwt' configuration section.");

// ─────────────────────────────────────────────────────────────────────────────
// Services
// ─────────────────────────────────────────────────────────────────────────────

builder.Services
    .AddDashboardServices(connectionString)
    .AddGateway()
    .AddAuthServices(connectionString, jwtOptions)
    .AddModuleServices(connectionString)
    .AddRealtimeBridge(builder.Configuration);

// JWT Bearer Authentication — configured with proper validation
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey         = new SymmetricSecurityKey(
                                           Encoding.UTF8.GetBytes(jwtOptions.SecretKey)),
            ValidateIssuer           = true,
            ValidIssuer              = jwtOptions.Issuer,
            ValidateAudience         = true,
            ValidAudience            = jwtOptions.Audience,
            ValidateLifetime         = true,
            ClockSkew                = TimeSpan.FromSeconds(30),
        };
    });

builder.Services.AddAuthorization();

// Redis distributed cache
var redisConnection = builder.Configuration["Redis:ConnectionString"] ?? "localhost:6379";
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = redisConnection;
    options.InstanceName  = "ioc:";
});

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

// Auth must come before TenantMiddleware so JWT claims are available
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
