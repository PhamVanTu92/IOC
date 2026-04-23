using Microsoft.AspNetCore.Mvc.Testing;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace IOC.Api.Tests;

/// <summary>
/// Integration tests cho GraphQL API.
/// Dùng WebApplicationFactory để spin up app thật.
/// </summary>
public sealed class GraphQLIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public GraphQLIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task HealthEndpoint_ShouldReturnHealthy()
    {
        // Act
        var response = await _client.GetAsync("/health");

        // Assert
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("healthy", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GraphQL_PluginsQuery_ShouldReturnRegisteredPlugins()
    {
        // Arrange
        var query = new { query = "{ plugins { id name version } }" };

        // Act
        var response = await _client.PostAsJsonAsync("/graphql", query);

        // Assert
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        var plugins = json.GetProperty("data").GetProperty("plugins");
        Assert.True(plugins.GetArrayLength() >= 3, "Phải có ít nhất 3 plugins (Finance, HR, Marketing)");
    }

    [Fact]
    public async Task GraphQL_MetricsQuery_ShouldReturnMetrics()
    {
        // Arrange
        var query = new { query = "{ metrics { id name domain unit } }" };

        // Act
        var response = await _client.PostAsJsonAsync("/graphql", query);

        // Assert
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(json.TryGetProperty("data", out _));
    }

    [Fact]
    public async Task GraphQL_PingMutation_ShouldReturnPong()
    {
        // Arrange
        var mutation = new { query = "mutation { ping }" };

        // Act
        var response = await _client.PostAsJsonAsync("/graphql", mutation);

        // Assert
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        var ping = json.GetProperty("data").GetProperty("ping").GetString();
        Assert.Equal("pong", ping);
    }
}
