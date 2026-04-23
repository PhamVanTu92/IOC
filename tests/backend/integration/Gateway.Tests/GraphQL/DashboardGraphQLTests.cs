using System.Net.Http.Json;
using System.Text.Json;
using DashboardService.Domain;
using FluentAssertions;
using Moq;
using Xunit;

namespace Gateway.Tests.GraphQL;

// ─────────────────────────────────────────────────────────────────────────────
// DashboardGraphQLTests — integration tests for the GraphQL dashboard API
//
// Field names (HotChocolate strips "Async" suffix and uses camelCase):
//   DashboardsAsync  → dashboards
//   DashboardAsync   → dashboard
//   CreateDashboardAsync → createDashboard
//   UpdateDashboardAsync → updateDashboard
//   DeleteDashboardAsync → deleteDashboard
// ─────────────────────────────────────────────────────────────────────────────

public sealed class DashboardGraphQLTests : IClassFixture<GatewayWebApplicationFactory>
{
    private readonly GatewayWebApplicationFactory _factory;
    private readonly HttpClient _client;

    private static readonly Guid _tenantId = Guid.Parse("00000000-0000-0000-0000-000000000001");
    private static readonly Guid _userId   = Guid.Parse("00000000-0000-0000-0000-000000000002");

    public DashboardGraphQLTests(GatewayWebApplicationFactory factory)
    {
        _factory = factory;
        _client  = factory.CreateClient();
        // TenantMiddleware resolves from this header in non-JWT environments
        _client.DefaultRequestHeaders.Add("X-Tenant-Id", _tenantId.ToString());
    }

    // ── Health ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GET_health_Returns200()
    {
        var response = await _client.GetAsync("/health");
        response.IsSuccessStatusCode.Should().BeTrue();
    }

    [Fact]
    public async Task GET_health_ready_Returns200WithReadyStatus()
    {
        var response = await _client.GetAsync("/health/ready");
        response.IsSuccessStatusCode.Should().BeTrue();

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("status").GetString().Should().Be("ready");
    }

    // ── dashboards query ──────────────────────────────────────────────────────

    [Fact]
    public async Task dashboards_WhenNoneExist_ReturnsEmptyArray()
    {
        _factory.RepositoryMock
            .Setup(r => r.ListByTenantAsync(_tenantId, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var result = await PostGraphQLAsync("""
            query {
              dashboards {
                id
                title
                widgetCount
              }
            }
            """);

        AssertNoErrors(result);
        var data = result!.Value.GetProperty("data").GetProperty("dashboards");
        data.GetArrayLength().Should().Be(0);
    }

    [Fact]
    public async Task dashboards_WithExistingDashboards_ReturnsSummaryList()
    {
        var dashboards = new[]
        {
            Dashboard.Reconstitute(
                Guid.NewGuid(), _tenantId, _userId,
                "Q1 KPIs", null,
                """{"id":"d1","title":"Q1 KPIs","widgets":[{},{}]}""",
                true, DateTime.UtcNow.AddDays(-7), DateTime.UtcNow.AddHours(-2)),

            Dashboard.Reconstitute(
                Guid.NewGuid(), _tenantId, _userId,
                "Finance Overview", "Tổng quan tài chính",
                """{"id":"d2","title":"Finance","widgets":[{}]}""",
                true, DateTime.UtcNow.AddDays(-3), DateTime.UtcNow.AddMinutes(-30)),
        };

        _factory.RepositoryMock
            .Setup(r => r.ListByTenantAsync(_tenantId, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(dashboards);

        var result = await PostGraphQLAsync("""
            query {
              dashboards {
                id
                title
                widgetCount
                updatedAt
              }
            }
            """);

        AssertNoErrors(result);
        var data = result!.Value.GetProperty("data").GetProperty("dashboards");
        data.GetArrayLength().Should().Be(2);
        data[0].GetProperty("title").GetString().Should().Be("Q1 KPIs");
        data[0].GetProperty("widgetCount").GetInt32().Should().Be(2);
        data[1].GetProperty("title").GetString().Should().Be("Finance Overview");
        data[1].GetProperty("widgetCount").GetInt32().Should().Be(1);
    }

    // ── dashboard(id) query ───────────────────────────────────────────────────

    [Fact]
    public async Task dashboard_WhenFound_ReturnsFullDto()
    {
        var id = Guid.NewGuid();
        var dash = Dashboard.Reconstitute(
            id, _tenantId, _userId,
            "Test Dashboard", "A test",
            """{"id":"dx","title":"Test","widgets":[]}""",
            true, DateTime.UtcNow.AddDays(-1), DateTime.UtcNow);

        _factory.RepositoryMock
            .Setup(r => r.GetByIdAsync(id, _tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(dash);

        var result = await PostGraphQLAsync($$"""
            query {
              dashboard(id: "{{id}}") {
                id
                title
                description
                isActive
                configJson
              }
            }
            """);

        AssertNoErrors(result);
        var data = result!.Value.GetProperty("data").GetProperty("dashboard");
        data.GetProperty("title").GetString().Should().Be("Test Dashboard");
        data.GetProperty("description").GetString().Should().Be("A test");
        data.GetProperty("isActive").GetBoolean().Should().BeTrue();
    }

    // ── createDashboard mutation ──────────────────────────────────────────────

    [Fact]
    public async Task createDashboard_WithValidInput_ReturnsNewDashboard()
    {
        _factory.RepositoryMock
            .Setup(r => r.AddAsync(It.IsAny<Dashboard>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var result = await PostGraphQLAsync("""
            mutation {
              createDashboard(input: {
                title: "New Board"
                configJson: "{\"id\":\"temp\",\"title\":\"New Board\",\"widgets\":[]}"
              }) {
                id
                title
                isActive
              }
            }
            """);

        AssertNoErrors(result);
        var data = result!.Value.GetProperty("data").GetProperty("createDashboard");
        data.GetProperty("title").GetString().Should().Be("New Board");
        data.GetProperty("isActive").GetBoolean().Should().BeTrue();
        data.GetProperty("id").GetString().Should().NotBeNullOrEmpty();
    }

    // ── deleteDashboard mutation ──────────────────────────────────────────────

    [Fact]
    public async Task deleteDashboard_WhenExists_ReturnsTrue()
    {
        var id = Guid.NewGuid();
        var dash = Dashboard.Reconstitute(
            id, _tenantId, _userId, "To Delete", null,
            """{"id":"d","title":"To Delete","widgets":[]}""",
            true, DateTime.UtcNow, DateTime.UtcNow);

        _factory.RepositoryMock
            .Setup(r => r.GetByIdAsync(id, _tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(dash);
        _factory.RepositoryMock
            .Setup(r => r.UpdateAsync(It.IsAny<Dashboard>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var result = await PostGraphQLAsync($$"""
            mutation {
              deleteDashboard(id: "{{id}}")
            }
            """);

        AssertNoErrors(result);
        var data = result!.Value.GetProperty("data").GetProperty("deleteDashboard");
        data.GetBoolean().Should().BeTrue();
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private async Task<JsonElement?> PostGraphQLAsync(string query)
    {
        var response = await _client.PostAsJsonAsync("/graphql", new { query });
        response.IsSuccessStatusCode.Should().BeTrue(
            $"HTTP {response.StatusCode}: {await response.Content.ReadAsStringAsync()}");

        return await response.Content.ReadFromJsonAsync<JsonElement>();
    }

    private static void AssertNoErrors(JsonElement? result)
    {
        result.Should().NotBeNull();
        if (result!.Value.TryGetProperty("errors", out var errors))
        {
            var msg = errors.GetRawText();
            throw new Exception($"GraphQL returned errors: {msg}");
        }
    }
}
