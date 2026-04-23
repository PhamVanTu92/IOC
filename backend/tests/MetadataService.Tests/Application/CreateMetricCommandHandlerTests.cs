using FluentAssertions;
using MetadataService.Application.Metrics.Commands.CreateMetric;
using MetadataService.Domain.Entities;
using MetadataService.Domain.Exceptions;
using MetadataService.Domain.Interfaces;
using Moq;

namespace MetadataService.Tests.Application;

/// <summary>
/// Unit tests cho CreateMetricCommandHandler.
/// Đặc biệt kiểm tra logic validate DependsOnMeasures.
/// Constructor order: IDatasetRepository, IMetricRepository, IMeasureRepository.
/// </summary>
public sealed class CreateMetricCommandHandlerTests
{
    private readonly Mock<IDatasetRepository> _datasetRepo = new();
    private readonly Mock<IMetricRepository>  _metricRepo  = new();
    private readonly Mock<IMeasureRepository> _measureRepo = new();
    private readonly CreateMetricCommandHandler _handler;

    private static readonly Guid _tenantId  = Guid.NewGuid();
    private static readonly Guid _datasetId = Guid.NewGuid();

    public CreateMetricCommandHandlerTests()
    {
        // Constructor order matches: (datasetRepo, metricRepo, measureRepo)
        _handler = new CreateMetricCommandHandler(
            _datasetRepo.Object,
            _metricRepo.Object,
            _measureRepo.Object);
    }

    private void SetupDatasetExists(bool exists = true) =>
        _datasetRepo
            .Setup(r => r.ExistsAsync(_datasetId, _tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(exists);

    private void SetupExistingMeasures(params string[] names) =>
        _measureRepo
            .Setup(r => r.ListByDatasetAsync(_datasetId, _tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(names.Select(CreateFakeMeasure).ToList());

    private static Measure CreateFakeMeasure(string name) =>
        Measure.Create(_datasetId, Guid.NewGuid(), name, name, name, "sum");

    [Fact]
    public async Task Handle_WhenDependentMeasuresExist_ShouldCreateMetric()
    {
        // Arrange
        SetupDatasetExists();
        SetupExistingMeasures("total_revenue", "total_orders");

        _metricRepo
            .Setup(r => r.CreateAsync(It.IsAny<Metric>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Metric m, CancellationToken _) => m);

        var command = new CreateMetricCommand(
            DatasetId:         _datasetId,
            TenantId:          _tenantId,
            Name:              "avg_order_value",
            DisplayName:       "Average Order Value",
            Expression:        "{{total_revenue}} / {{total_orders}}",
            DependsOnMeasures: ["total_revenue", "total_orders"]
        );

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be("avg_order_value");
        result.Expression.Should().Be("{{total_revenue}} / {{total_orders}}");
        result.DependsOnMeasures.Should().Contain("total_revenue");
    }

    [Fact]
    public async Task Handle_WhenDependentMeasureDoesNotExist_ShouldThrowInvalidOperationException()
    {
        // Arrange
        SetupDatasetExists();
        SetupExistingMeasures("total_revenue"); // thiếu "total_orders"

        var command = new CreateMetricCommand(
            DatasetId:         _datasetId,
            TenantId:          _tenantId,
            Name:              "aov",
            DisplayName:       "AOV",
            Expression:        "{{total_revenue}} / {{total_orders}}",
            DependsOnMeasures: ["total_revenue", "total_orders"]
        );

        // Act
        var act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert — error message phải liệt kê measure bị thiếu
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*total_orders*");

        _metricRepo.Verify(
            r => r.CreateAsync(It.IsAny<Metric>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_WhenDatasetNotFound_ShouldThrowDatasetNotFoundException()
    {
        // Arrange
        SetupDatasetExists(exists: false);

        var command = new CreateMetricCommand(
            DatasetId:  _datasetId,
            TenantId:   _tenantId,
            Name:       "m",
            DisplayName: "M",
            Expression: "{{x}}"
        );

        // Act
        var act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<DatasetNotFoundException>();
    }

    [Fact]
    public async Task Handle_WithNullDependsOnMeasures_ShouldNotValidateMeasures()
    {
        // Arrange
        SetupDatasetExists();

        _metricRepo
            .Setup(r => r.CreateAsync(It.IsAny<Metric>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Metric m, CancellationToken _) => m);

        var command = new CreateMetricCommand(
            DatasetId:         _datasetId,
            TenantId:          _tenantId,
            Name:              "constant_target",
            DisplayName:       "Target",
            Expression:        "1000000",
            DependsOnMeasures: null // không có dependency
        );

        // Act — không nên throw
        var act = async () => await _handler.Handle(command, CancellationToken.None);
        await act.Should().NotThrowAsync();

        // Assert — ListByDatasetAsync không được gọi (không cần validate)
        _measureRepo.Verify(
            r => r.ListByDatasetAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_DependsOnMeasures_IsCaseInsensitive()
    {
        // Arrange
        SetupDatasetExists();
        SetupExistingMeasures("Total_Revenue", "Total_Orders"); // stored với case khác

        _metricRepo
            .Setup(r => r.CreateAsync(It.IsAny<Metric>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Metric m, CancellationToken _) => m);

        var command = new CreateMetricCommand(
            DatasetId:         _datasetId,
            TenantId:          _tenantId,
            Name:              "aov",
            DisplayName:       "AOV",
            Expression:        "{{total_revenue}} / {{total_orders}}",
            DependsOnMeasures: ["total_revenue", "total_orders"] // lowercase
        );

        // Act — case-insensitive matching, không throw
        var act = async () => await _handler.Handle(command, CancellationToken.None);
        await act.Should().NotThrowAsync();
    }
}
