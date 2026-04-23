using IOC.SignalR;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace IOC.Realtime.Tests;

// ─────────────────────────────────────────────────────────────────────────────
// DashboardNotifierTests — verifies imperative push helpers
// ─────────────────────────────────────────────────────────────────────────────

public sealed class DashboardNotifierTests
{
    private static (DashboardNotifier notifier, Mock<IClientProxy> clientProxyMock) CreateNotifier()
    {
        var proxyMock = new Mock<IClientProxy>();
        proxyMock
            .Setup(p => p.SendCoreAsync(
                It.IsAny<string>(),
                It.IsAny<object?[]>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var groupsMock = new Mock<IHubClients>();
        groupsMock
            .Setup(g => g.Group(It.IsAny<string>()))
            .Returns(proxyMock.Object);

        var hubContextMock = new Mock<IHubContext<DashboardHub>>();
        hubContextMock.Setup(h => h.Clients).Returns(groupsMock.Object);

        var notifier = new DashboardNotifier(hubContextMock.Object, NullLogger<DashboardNotifier>.Instance);

        return (notifier, proxyMock);
    }

    [Fact]
    public async Task NotifyMetricUpdatedAsync_SendsToCorrectDomainGroup()
    {
        var (notifier, proxy) = CreateNotifier();

        var payload = new MetricUpdatePayload(
            MetricId: "revenue",
            Domain: "Finance",
            Value: 1_000_000,
            Unit: "VND",
            Timestamp: DateTime.UtcNow);

        await notifier.NotifyMetricUpdatedAsync(payload);

        // Verifies SendAsync was called with "ReceiveMetricUpdate"
        proxy.Verify(
            p => p.SendCoreAsync(
                "ReceiveMetricUpdate",
                It.Is<object?[]>(args => args.Length == 1 && args[0] is MetricUpdatePayload),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task NotifyDatasetRefreshedAsync_SendsToDatasetGroup()
    {
        var (notifier, proxy) = CreateNotifier();

        await notifier.NotifyDatasetRefreshedAsync("ds-42", DateTime.UtcNow);

        proxy.Verify(
            p => p.SendCoreAsync(
                "DatasetRefreshed",
                It.IsAny<object?[]>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
