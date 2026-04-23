using FluentAssertions;
using MetadataService.Application.Datasets.DTOs;
using MetadataService.Application.Measures.Commands.CreateMeasure;
using MetadataService.Domain.Entities;
using MetadataService.Domain.Exceptions;
using MetadataService.Domain.Interfaces;
using Moq;

namespace MetadataService.Tests.Application;

/// <summary>
/// Unit tests cho CreateMeasureCommandHandler.
/// </summary>
public sealed class CreateMeasureCommandHandlerTests
{
    private readonly Mock<IDatasetRepository> _datasetRepo  = new();
    private readonly Mock<IMeasureRepository> _measureRepo  = new();
    private readonly CreateMeasureCommandHandler _handler;

    private static readonly Guid _tenantId  = Guid.NewGuid();
    private static readonly Guid _datasetId = Guid.NewGuid();

    public CreateMeasureCommandHandlerTests()
    {
        _handler = new CreateMeasureCommandHandler(_datasetRepo.Object, _measureRepo.Object);
    }

    [Fact]
    public async Task Handle_WhenDatasetExists_ShouldCreateMeasure()
    {
        // Arrange
        var command = new CreateMeasureCommand(
            DatasetId:       _datasetId,
            TenantId:        _tenantId,
            Name:            "total_revenue",
            DisplayName:     "Total Revenue",
            ColumnName:      "amount",
            AggregationType: "Sum",
            DataType:        "decimal",
            Format:          "#,##0.00"
        );

        _datasetRepo
            .Setup(r => r.ExistsAsync(_datasetId, _tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _measureRepo
            .Setup(r => r.CreateAsync(It.IsAny<Measure>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Measure m, CancellationToken _) => m);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be("total_revenue");
        // Entity normalizes aggregationType to lowercase
        result.AggregationType.Should().Be("sum");
        result.DatasetId.Should().Be(_datasetId);
    }

    [Fact]
    public async Task Handle_WhenDatasetDoesNotExist_ShouldThrowDatasetNotFoundException()
    {
        // Arrange
        var command = new CreateMeasureCommand(
            DatasetId:       _datasetId,
            TenantId:        _tenantId,
            Name:            "m",
            DisplayName:     "M",
            ColumnName:      "col",
            AggregationType: "Count"
        );

        _datasetRepo
            .Setup(r => r.ExistsAsync(_datasetId, _tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<DatasetNotFoundException>()
            .WithMessage($"*{_datasetId}*");

        _measureRepo.Verify(
            r => r.CreateAsync(It.IsAny<Measure>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_WithFilterExpression_ShouldPersistFilter()
    {
        // Arrange
        var command = new CreateMeasureCommand(
            DatasetId:        _datasetId,
            TenantId:         _tenantId,
            Name:             "completed_orders",
            DisplayName:      "Completed Orders",
            ColumnName:       "id",
            AggregationType:  "Count",
            FilterExpression: "status = 'completed'"
        );

        _datasetRepo
            .Setup(r => r.ExistsAsync(_datasetId, _tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _measureRepo
            .Setup(r => r.CreateAsync(It.IsAny<Measure>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Measure m, CancellationToken _) => m);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.FilterExpression.Should().Be("status = 'completed'");
    }

    [Fact]
    public async Task Handle_ShouldReturnDtoWithCorrectAggregateExpression()
    {
        // Arrange
        var command = new CreateMeasureCommand(
            DatasetId:       _datasetId,
            TenantId:        _tenantId,
            Name:            "unique_customers",
            DisplayName:     "Unique Customers",
            ColumnName:      "customer_id",
            AggregationType: "count_distinct"  // snake_case để match switch case trong GetAggregateExpression
        );

        _datasetRepo
            .Setup(r => r.ExistsAsync(_datasetId, _tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Capture the entity được persist để kiểm tra aggregate expression
        Measure? capturedMeasure = null;
        _measureRepo
            .Setup(r => r.CreateAsync(It.IsAny<Measure>(), It.IsAny<CancellationToken>()))
            .Callback<Measure, CancellationToken>((m, _) => capturedMeasure = m)
            .ReturnsAsync((Measure m, CancellationToken _) => m);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert — entity phải có aggregate expression đúng
        capturedMeasure.Should().NotBeNull();
        // Column name được double-quoted trong SQL output
        capturedMeasure!.GetAggregateExpression().Should().Be("COUNT(DISTINCT \"customer_id\")");
    }
}
