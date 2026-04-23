# ─────────────────────────────────────────────────────────────────────────────
# IOC Backend — Multi-stage Dockerfile
# Build context: ../src  (includes both backend/ and realtime/)
# ─────────────────────────────────────────────────────────────────────────────

# ── Stage 1: Build ────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy solution + all project files first (layer cache)
COPY backend/IOC.sln                                              backend/IOC.sln
COPY backend/IOC.Core/IOC.Core.csproj                            backend/IOC.Core/
COPY backend/IOC.SemanticLayer/IOC.SemanticLayer.csproj           backend/IOC.SemanticLayer/
COPY backend/DashboardService/DashboardService.Domain/DashboardService.Domain.csproj \
     backend/DashboardService/DashboardService.Domain/
COPY backend/DashboardService/DashboardService.Application/DashboardService.Application.csproj \
     backend/DashboardService/DashboardService.Application/
COPY backend/DashboardService/DashboardService.Infrastructure/DashboardService.Infrastructure.csproj \
     backend/DashboardService/DashboardService.Infrastructure/
COPY backend/Gateway/Gateway/Gateway.csproj                       backend/Gateway/Gateway/
COPY backend/Plugins/IOC.Finance/IOC.Finance.csproj              backend/Plugins/IOC.Finance/
COPY backend/Plugins/IOC.HR/IOC.HR.csproj                        backend/Plugins/IOC.HR/
COPY backend/Plugins/IOC.Marketing/IOC.Marketing.csproj          backend/Plugins/IOC.Marketing/
COPY realtime/IOC.Kafka/IOC.Kafka.csproj                         realtime/IOC.Kafka/
COPY realtime/IOC.SignalR/IOC.SignalR.csproj                     realtime/IOC.SignalR/

# Restore (cached unless .csproj changes)
RUN dotnet restore backend/IOC.sln

# Copy all source
COPY backend/ backend/
COPY realtime/ realtime/

# Build & publish Gateway
RUN dotnet publish backend/Gateway/Gateway/Gateway.csproj \
    -c Release \
    -o /app/publish \
    --no-restore

# ── Stage 2: Runtime ──────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Non-root user
RUN addgroup --system --gid 1001 ioc && \
    adduser  --system --uid 1001 --ingroup ioc --no-create-home ioc

COPY --from=build /app/publish .

# Health check dependency
RUN apt-get update && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*

USER ioc
EXPOSE 8080

ENV ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_ENVIRONMENT=Production \
    DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false

HEALTHCHECK --interval=30s --timeout=10s --start-period=40s --retries=3 \
    CMD curl -f http://localhost:8080/health || exit 1

ENTRYPOINT ["dotnet", "Gateway.dll"]
