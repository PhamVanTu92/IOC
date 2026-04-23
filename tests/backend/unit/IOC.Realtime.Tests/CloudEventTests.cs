using System.Text.Json;
using FluentAssertions;
using IOC.Kafka;
using IOC.Kafka.Events;
using Xunit;

namespace IOC.Realtime.Tests;

// ─────────────────────────────────────────────────────────────────────────────
// CloudEventTests — validates envelope structure and JSON serialisation
// ─────────────────────────────────────────────────────────────────────────────

public sealed class CloudEventTests
{
    private static readonly JsonSerializerOptions _opts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    private static readonly JsonSerializerOptions _readOpts = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    // ── Factory ───────────────────────────────────────────────────────────────

    [Fact]
    public void Create_PopulatesRequiredFields()
    {
        var evt = CloudEvent<string>.Create("ioc.test.created", "/ioc/test", "payload");

        evt.SpecVersion.Should().Be("1.0");
        evt.Type.Should().Be("ioc.test.created");
        evt.Source.Should().Be("/ioc/test");
        evt.Data.Should().Be("payload");
        evt.DataContentType.Should().Be("application/json");
        evt.Id.Should().NotBeNullOrEmpty();
        evt.Time.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void Create_AssignsUniqueIdPerInstance()
    {
        var a = CloudEvent<int>.Create("ioc.test", "/ioc", 1);
        var b = CloudEvent<int>.Create("ioc.test", "/ioc", 2);
        a.Id.Should().NotBe(b.Id);
    }

    // ── Serialisation ─────────────────────────────────────────────────────────

    [Fact]
    public void Serialise_ProducesValidCloudEventsJson()
    {
        var data = new MetricUpdatedEvent(
            DatasetId: "ds-001",
            Domain: "finance",
            MetricName: "revenue",
            Value: 1_000_000,
            Unit: "VND",
            TenantId: "tenant-abc",
            Timestamp: DateTime.UtcNow);

        var envelope = CloudEvent<MetricUpdatedEvent>.Create(
            KafkaTopics.MetricUpdated, "/ioc/metrics", data);

        var json = JsonSerializer.Serialize(envelope, _opts);

        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        root.GetProperty("specversion").GetString().Should().Be("1.0");
        root.GetProperty("type").GetString().Should().Be(KafkaTopics.MetricUpdated);
        root.GetProperty("source").GetString().Should().Be("/ioc/metrics");
        root.GetProperty("datacontenttype").GetString().Should().Be("application/json");
        root.TryGetProperty("data", out _).Should().BeTrue();
    }

    [Fact]
    public void Roundtrip_DeserialisePreservesData()
    {
        var original = new DashboardSavedEvent(
            DashboardId: Guid.NewGuid().ToString(),
            TenantId: Guid.NewGuid().ToString(),
            Title: "Q1 KPIs",
            WidgetCount: 5,
            SavedBy: "user@ioc.vn",
            SavedAt: DateTime.UtcNow);

        var envelope = CloudEvent<DashboardSavedEvent>.Create(
            KafkaTopics.DashboardSaved, "/ioc/dashboard", original);

        var json = JsonSerializer.Serialize(envelope, _opts);

        var deserialized = JsonSerializer.Deserialize<CloudEvent<DashboardSavedEvent>>(json, _readOpts);

        deserialized.Should().NotBeNull();
        deserialized!.Data.Should().NotBeNull();
        deserialized.Data.DashboardId.Should().Be(original.DashboardId);
        deserialized.Data.Title.Should().Be(original.Title);
        deserialized.Data.WidgetCount.Should().Be(original.WidgetCount);
    }

    // ── KafkaTopics constants ─────────────────────────────────────────────────

    [Theory]
    [InlineData(KafkaTopics.MetricUpdated,    "ioc.metrics.updated")]
    [InlineData(KafkaTopics.QueryExecuted,    "ioc.query.executed")]
    [InlineData(KafkaTopics.DashboardSaved,   "ioc.dashboard.saved")]
    [InlineData(KafkaTopics.DashboardDeleted, "ioc.dashboard.deleted")]
    public void KafkaTopics_MatchExpectedNames(string actual, string expected)
    {
        actual.Should().Be(expected);
    }
}
