using DashboardService.Domain;
using FluentAssertions;

namespace DashboardService.Tests.Domain;

// ─────────────────────────────────────────────────────────────────────────────
// Dashboard entity — factory + mutation tests
// ─────────────────────────────────────────────────────────────────────────────

public sealed class DashboardEntityTests
{
    private static readonly Guid _tenantId = Guid.NewGuid();
    private static readonly Guid _userId = Guid.NewGuid();
    private const string ValidConfigJson = """{"id":"test","title":"t","widgets":[]}""";

    // ── Create ────────────────────────────────────────────────────────────────

    [Fact]
    public void Create_WithValidInputs_ShouldReturnActiveDashboard()
    {
        // Act
        var d = Dashboard.Create(_tenantId, _userId, "My Dashboard", ValidConfigJson);

        // Assert
        d.Id.Should().NotBeEmpty();
        d.TenantId.Should().Be(_tenantId);
        d.CreatedBy.Should().Be(_userId);
        d.Title.Should().Be("My Dashboard");
        d.ConfigJson.Should().Be(ValidConfigJson);
        d.IsActive.Should().BeTrue();
        d.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
        d.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
    }

    [Fact]
    public void Create_ShouldGenerateUniqueIds()
    {
        var a = Dashboard.Create(_tenantId, _userId, "A", ValidConfigJson);
        var b = Dashboard.Create(_tenantId, _userId, "B", ValidConfigJson);
        a.Id.Should().NotBe(b.Id);
    }

    [Fact]
    public void Create_WithDescription_ShouldStoreDescription()
    {
        var d = Dashboard.Create(_tenantId, _userId, "Title", ValidConfigJson, "desc");
        d.Description.Should().Be("desc");
    }

    [Fact]
    public void Create_WithWhitespaceTitle_ShouldThrow()
    {
        var act = () => Dashboard.Create(_tenantId, _userId, "   ", ValidConfigJson);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_WithEmptyConfigJson_ShouldThrow()
    {
        var act = () => Dashboard.Create(_tenantId, _userId, "Title", "");
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_ShouldTrimTitle()
    {
        var d = Dashboard.Create(_tenantId, _userId, "  Finance  ", ValidConfigJson);
        d.Title.Should().Be("Finance");
    }

    // ── Update ────────────────────────────────────────────────────────────────

    [Fact]
    public void Update_ShouldChangeFields()
    {
        var d = Dashboard.Create(_tenantId, _userId, "Old", ValidConfigJson);
        var newJson = """{"id":"test","title":"New","widgets":[]}""";

        d.Update("New Title", newJson, "new desc");

        d.Title.Should().Be("New Title");
        d.ConfigJson.Should().Be(newJson);
        d.Description.Should().Be("new desc");
    }

    [Fact]
    public void Update_ShouldAdvanceUpdatedAt()
    {
        var d = Dashboard.Create(_tenantId, _userId, "Old", ValidConfigJson);
        var before = d.UpdatedAt;

        System.Threading.Thread.Sleep(10);
        d.Update("New", ValidConfigJson);

        d.UpdatedAt.Should().BeAfter(before);
    }

    [Fact]
    public void Update_WithWhitespaceTitle_ShouldThrow()
    {
        var d = Dashboard.Create(_tenantId, _userId, "Title", ValidConfigJson);
        var act = () => d.Update("  ", ValidConfigJson);
        act.Should().Throw<ArgumentException>();
    }

    // ── Deactivate ────────────────────────────────────────────────────────────

    [Fact]
    public void Deactivate_ShouldSetIsActiveFalse()
    {
        var d = Dashboard.Create(_tenantId, _userId, "Title", ValidConfigJson);
        d.Deactivate();
        d.IsActive.Should().BeFalse();
    }

    [Fact]
    public void Deactivate_ShouldAdvanceUpdatedAt()
    {
        var d = Dashboard.Create(_tenantId, _userId, "Title", ValidConfigJson);
        var before = d.UpdatedAt;
        System.Threading.Thread.Sleep(10);
        d.Deactivate();
        d.UpdatedAt.Should().BeAfter(before);
    }

    // ── Reconstitute ──────────────────────────────────────────────────────────

    [Fact]
    public void Reconstitute_ShouldRestoreAllFields()
    {
        var id = Guid.NewGuid();
        var created = DateTime.UtcNow.AddDays(-7);
        var updated = DateTime.UtcNow.AddHours(-1);

        var d = Dashboard.Reconstitute(
            id, _tenantId, _userId,
            "Title", "Desc", ValidConfigJson,
            isActive: true, created, updated);

        d.Id.Should().Be(id);
        d.TenantId.Should().Be(_tenantId);
        d.Title.Should().Be("Title");
        d.Description.Should().Be("Desc");
        d.IsActive.Should().BeTrue();
        d.CreatedAt.Should().Be(created);
        d.UpdatedAt.Should().Be(updated);
    }
}
