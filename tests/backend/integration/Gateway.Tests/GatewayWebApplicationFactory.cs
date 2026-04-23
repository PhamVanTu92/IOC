using DashboardService.Domain;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Moq;
using IOC.SignalR;

namespace Gateway.Tests;

// ─────────────────────────────────────────────────────────────────────────────
// GatewayWebApplicationFactory — configures the Gateway for integration tests
//
// Strategy:
//   • Replaces IDashboardRepository with a Moq stub (no real DB needed)
//   • Removes all IHostedService registrations (no Kafka / metric publishers)
//   • Provides a no-op IKafkaPublisher so plugin registrations compile
// ─────────────────────────────────────────────────────────────────────────────

public sealed class GatewayWebApplicationFactory : WebApplicationFactory<Program>
{
    public Mock<IDashboardRepository> RepositoryMock { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Test");

        builder.ConfigureServices(services =>
        {
            // Replace real repository (registered by AddDashboardInfrastructure)
            services.RemoveAll<IDashboardRepository>();
            services.AddSingleton(RepositoryMock.Object);

            // Remove all background services (Kafka consumer, Finance publisher, etc.)
            services.RemoveAll<IHostedService>();

            // Provide safe Kafka config
            services.RemoveAll<RealtimeBridgeOptions>();
            services.AddSingleton(new RealtimeBridgeOptions
            {
                BootstrapServers = "localhost:9092",
                ConsumerGroupId  = "test-group",
            });

            // No-op Kafka publisher so Finance plugin initializes without Kafka
            services.RemoveAll<IOC.Kafka.IKafkaPublisher>();
            var noopPublisher = new Mock<IOC.Kafka.IKafkaPublisher>();
            noopPublisher
                .Setup(p => p.PublishAsync(
                    It.IsAny<string>(), It.IsAny<string>(),
                    It.IsAny<object>(), It.IsAny<string?>(),
                    It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            services.AddSingleton(noopPublisher.Object);
        });
    }
}
