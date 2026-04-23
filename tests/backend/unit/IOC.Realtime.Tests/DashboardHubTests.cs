using IOC.SignalR;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace IOC.Realtime.Tests;

// ─────────────────────────────────────────────────────────────────────────────
// DashboardHubTests — verifies group subscription logic in DashboardHub
// ─────────────────────────────────────────────────────────────────────────────

public sealed class DashboardHubTests
{
    // ── Helpers ───────────────────────────────────────────────────────────────

    private static (DashboardHub hub, Mock<IGroupManager> groupsMock) CreateHub(
        string connectionId = "conn-1")
    {
        var groupsMock = new Mock<IGroupManager>();
        groupsMock
            .Setup(g => g.AddToGroupAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        groupsMock
            .Setup(g => g.RemoveFromGroupAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var contextMock = new Mock<HubCallerContext>();
        contextMock.Setup(c => c.ConnectionId).Returns(connectionId);

        var hub = new DashboardHub(NullLogger<DashboardHub>.Instance)
        {
            Groups = groupsMock.Object,
            Context = contextMock.Object,
        };

        return (hub, groupsMock);
    }

    // ── Domain ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task SubscribeToDomain_AddsClientToCorrectGroup()
    {
        var (hub, groups) = CreateHub("conn-A");

        await hub.SubscribeToDomain("Finance");

        groups.Verify(
            g => g.AddToGroupAsync("conn-A", "domain-finance", It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task UnsubscribeFromDomain_RemovesClientFromGroup()
    {
        var (hub, groups) = CreateHub("conn-A");

        await hub.UnsubscribeFromDomain("Finance");

        groups.Verify(
            g => g.RemoveFromGroupAsync("conn-A", "domain-finance", It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // ── Dataset ───────────────────────────────────────────────────────────────

    [Theory]
    [InlineData("ds-001")]
    [InlineData("dataset-uuid-1234")]
    public async Task SubscribeToDataset_AddsClientToDatasetGroup(string datasetId)
    {
        var (hub, groups) = CreateHub("conn-B");

        await hub.SubscribeToDataset(datasetId);

        groups.Verify(
            g => g.AddToGroupAsync("conn-B", $"dataset-{datasetId}", It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task UnsubscribeFromDataset_RemovesClientFromDatasetGroup()
    {
        var (hub, groups) = CreateHub("conn-B");

        await hub.UnsubscribeFromDataset("ds-001");

        groups.Verify(
            g => g.RemoveFromGroupAsync("conn-B", "dataset-ds-001", It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // ── Tenant ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task SubscribeToTenant_AddsClientToTenantGroup()
    {
        var (hub, groups) = CreateHub("conn-C");
        var tenantId = Guid.NewGuid().ToString();

        await hub.SubscribeToTenant(tenantId);

        groups.Verify(
            g => g.AddToGroupAsync("conn-C", $"tenant-{tenantId}", It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // ── Dashboard ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task SubscribeToDashboard_AddsClientToDashboardGroup()
    {
        var (hub, groups) = CreateHub("conn-D");
        var dashboardId = Guid.NewGuid().ToString();

        await hub.SubscribeToDashboard(dashboardId);

        groups.Verify(
            g => g.AddToGroupAsync("conn-D", $"dashboard-{dashboardId}", It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task UnsubscribeFromDashboard_RemovesClientFromDashboardGroup()
    {
        var (hub, groups) = CreateHub("conn-D");
        var dashboardId = "dash-abc";

        await hub.UnsubscribeFromDashboard(dashboardId);

        groups.Verify(
            g => g.RemoveFromGroupAsync("conn-D", "dashboard-dash-abc", It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
