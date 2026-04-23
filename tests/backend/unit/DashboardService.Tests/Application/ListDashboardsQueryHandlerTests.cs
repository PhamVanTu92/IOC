using DashboardService.Application.Queries.ListDashboards;
using DashboardService.Domain;
using FluentAssertions;
using Moq;

namespace DashboardService.Tests.Application;

public sealed class ListDashboardsQueryHandlerTests
{
    private static readonly Guid TenantId = Guid.NewGuid();
    private readonly Mock<IDashboardRepository> _repo = new();
    private readonly ListDashboardsQueryHandler _handler;

    public ListDashboardsQueryHandlerTests()
    {
        _handler = new ListDashboardsQueryHandler(_repo.Object);
    }

    [Fact]
    public async Task Handle_ShouldReturnSummariesForTenant()
    {
        // Arrange
        var dashboards = new[]
        {
            Dashboard.Create(TenantId, Guid.NewGuid(), "Board A",
                """{"id":"a","title":"A","widgets":[{"id":"w1"},{"id":"w2"}]}"""),
            Dashboard.Create(TenantId, Guid.NewGuid(), "Board B",
                """{"id":"b","title":"B","widgets":[]}"""),
        };

        _repo.Setup(r => r.ListByTenantAsync(TenantId, false, default))
             .ReturnsAsync(dashboards);

        // Act
        var result = await _handler.Handle(new ListDashboardsQuery(TenantId), default);

        // Assert
        result.Should().HaveCount(2);
        result[0].Title.Should().Be("Board A");
        result[0].WidgetCount.Should().Be(2);
        result[1].Title.Should().Be("Board B");
        result[1].WidgetCount.Should().Be(0);
    }

    [Fact]
    public async Task Handle_WhenConfigJsonMalformed_WidgetCountShouldBeZero()
    {
        var d = Dashboard.Reconstitute(
            Guid.NewGuid(), TenantId, Guid.NewGuid(),
            "Bad Config", null, "not-valid-json",
            true, DateTime.UtcNow, DateTime.UtcNow);

        _repo.Setup(r => r.ListByTenantAsync(TenantId, false, default))
             .ReturnsAsync([d]);

        var result = await _handler.Handle(new ListDashboardsQuery(TenantId), default);

        result[0].WidgetCount.Should().Be(0);
    }

    [Fact]
    public async Task Handle_WhenNoDashboards_ReturnsEmptyList()
    {
        _repo.Setup(r => r.ListByTenantAsync(TenantId, false, default))
             .ReturnsAsync([]);

        var result = await _handler.Handle(new ListDashboardsQuery(TenantId), default);

        result.Should().BeEmpty();
    }
}
