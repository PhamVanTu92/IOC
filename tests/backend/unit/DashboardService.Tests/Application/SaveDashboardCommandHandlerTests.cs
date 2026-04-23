using DashboardService.Application.Commands.SaveDashboard;
using DashboardService.Domain;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace DashboardService.Tests.Application;

// ─────────────────────────────────────────────────────────────────────────────
// SaveDashboardCommandHandler — create + update paths
// ─────────────────────────────────────────────────────────────────────────────

public sealed class SaveDashboardCommandHandlerTests
{
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly Guid UserId = Guid.NewGuid();
    private const string ConfigJson = """{"id":"cfg","title":"t","widgets":[]}""";

    private readonly Mock<IDashboardRepository> _repo = new();
    private readonly SaveDashboardCommandHandler _handler;

    public SaveDashboardCommandHandlerTests()
    {
        _handler = new SaveDashboardCommandHandler(
            _repo.Object,
            NullLogger<SaveDashboardCommandHandler>.Instance);
    }

    // ── Create path ───────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_WhenIdIsNull_ShouldCreateDashboard()
    {
        // Arrange
        _repo.Setup(r => r.AddAsync(It.IsAny<Dashboard>(), default));

        var cmd = new SaveDashboardCommand(
            Id: null, TenantId, UserId, "My Board", ConfigJson);

        // Act
        var result = await _handler.Handle(cmd, default);

        // Assert
        result.Id.Should().NotBeEmpty();
        result.Title.Should().Be("My Board");
        result.TenantId.Should().Be(TenantId);
        result.CreatedBy.Should().Be(UserId);
        result.ConfigJson.Should().Be(ConfigJson);
        _repo.Verify(r => r.AddAsync(It.IsAny<Dashboard>(), default), Times.Once);
    }

    [Fact]
    public async Task Handle_Create_ShouldPassDescriptionThrough()
    {
        var cmd = new SaveDashboardCommand(
            null, TenantId, UserId, "Board", ConfigJson, "A description");

        var result = await _handler.Handle(cmd, default);

        result.Description.Should().Be("A description");
    }

    // ── Update path ───────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_WhenIdProvided_ShouldUpdateDashboard()
    {
        // Arrange
        var existing = Dashboard.Create(TenantId, UserId, "Old Title", ConfigJson);
        _repo.Setup(r => r.GetByIdAsync(existing.Id, TenantId, default))
             .ReturnsAsync(existing);

        var newJson = """{"id":"cfg","title":"new","widgets":[{"id":"w1"}]}""";
        var cmd = new SaveDashboardCommand(existing.Id, TenantId, UserId, "New Title", newJson);

        // Act
        var result = await _handler.Handle(cmd, default);

        // Assert
        result.Title.Should().Be("New Title");
        result.ConfigJson.Should().Be(newJson);
        _repo.Verify(r => r.UpdateAsync(existing, default), Times.Once);
        _repo.Verify(r => r.AddAsync(It.IsAny<Dashboard>(), default), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenDashboardNotFound_ShouldThrowDashboardNotFoundException()
    {
        var id = Guid.NewGuid();
        _repo.Setup(r => r.GetByIdAsync(id, TenantId, default))
             .ReturnsAsync((Dashboard?)null);

        var cmd = new SaveDashboardCommand(id, TenantId, UserId, "Title", ConfigJson);

        var act = async () => await _handler.Handle(cmd, default);

        await act.Should().ThrowAsync<DashboardNotFoundException>();
    }
}
