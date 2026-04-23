using FluentAssertions;
using MetadataService.Application.Datasets.Commands.CreateDataset;
using MetadataService.Domain.Entities;
using MetadataService.Domain.Exceptions;
using MetadataService.Domain.Interfaces;
using Moq;

namespace MetadataService.Tests.Application;

/// <summary>
/// Unit tests cho CreateDatasetCommandHandler.
/// </summary>
public sealed class CreateDatasetCommandHandlerTests
{
    private readonly Mock<IDatasetRepository> _datasetRepo = new();
    private readonly CreateDatasetCommandHandler _handler;

    private static readonly Guid _tenantId  = Guid.NewGuid();
    private static readonly Guid _createdBy = Guid.NewGuid();

    public CreateDatasetCommandHandlerTests()
    {
        _handler = new CreateDatasetCommandHandler(_datasetRepo.Object);
    }

    [Fact]
    public async Task Handle_WhenNameIsUnique_ShouldCreateDataset()
    {
        // Arrange
        var command = new CreateDatasetCommand(
            TenantId:   _tenantId,
            CreatedBy:  _createdBy,
            Name:       "orders",
            SourceType: "postgresql",
            SchemaName: "public",
            TableName:  "orders"
        );

        _datasetRepo
            .Setup(r => r.ExistsByNameAsync("orders", _tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _datasetRepo
            .Setup(r => r.CreateAsync(It.IsAny<Dataset>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Dataset d, CancellationToken _) => d);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be("orders");
        result.TenantId.Should().Be(_tenantId);
        result.SourceType.Should().Be("postgresql");
        result.IsActive.Should().BeTrue();

        _datasetRepo.Verify(
            r => r.CreateAsync(
                It.Is<Dataset>(d => d.Name == "orders" && d.TenantId == _tenantId),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WhenNameAlreadyExists_ShouldThrowDuplicateDatasetException()
    {
        // Arrange
        var command = new CreateDatasetCommand(
            TenantId:   _tenantId,
            CreatedBy:  _createdBy,
            Name:       "existing_dataset",
            SourceType: "postgresql",
            TableName:  "t"
        );

        _datasetRepo
            .Setup(r => r.ExistsByNameAsync("existing_dataset", _tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<DuplicateDatasetException>();

        _datasetRepo.Verify(
            r => r.CreateAsync(It.IsAny<Dataset>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_WithCustomSqlSource_ShouldPersistCustomSql()
    {
        // Arrange
        const string customSql = "SELECT * FROM raw_orders WHERE year = 2024";
        var command = new CreateDatasetCommand(
            TenantId:   _tenantId,
            CreatedBy:  _createdBy,
            Name:       "orders_2024",
            SourceType: "custom_sql",
            CustomSql:  customSql
        );

        _datasetRepo
            .Setup(r => r.ExistsByNameAsync("orders_2024", _tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _datasetRepo
            .Setup(r => r.CreateAsync(It.IsAny<Dataset>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Dataset d, CancellationToken _) => d);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.CustomSql.Should().Be(customSql);
        result.SourceType.Should().Be("custom_sql");
    }

    [Fact]
    public async Task Handle_ShouldPassCancellationToken_ToAllRepositoryMethods()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        var token = cts.Token;

        var command = new CreateDatasetCommand(
            TenantId:  _tenantId,
            CreatedBy: _createdBy,
            Name:      "ds",
            SourceType: "postgresql",
            TableName: "t"
        );

        _datasetRepo
            .Setup(r => r.ExistsByNameAsync(It.IsAny<string>(), It.IsAny<Guid>(), token))
            .ReturnsAsync(false);
        _datasetRepo
            .Setup(r => r.CreateAsync(It.IsAny<Dataset>(), token))
            .ReturnsAsync((Dataset d, CancellationToken _) => d);

        // Act
        await _handler.Handle(command, token);

        // Assert — CancellationToken được pass đúng đến cả hai calls
        _datasetRepo.Verify(
            r => r.ExistsByNameAsync(It.IsAny<string>(), It.IsAny<Guid>(), token),
            Times.Once);
        _datasetRepo.Verify(
            r => r.CreateAsync(It.IsAny<Dataset>(), token),
            Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldReturnDto_WithEmptyChildCollections()
    {
        // Arrange
        var command = new CreateDatasetCommand(
            TenantId:   _tenantId,
            CreatedBy:  _createdBy,
            Name:       "ds",
            SourceType: "postgresql",
            TableName:  "t"
        );

        _datasetRepo
            .Setup(r => r.ExistsByNameAsync(It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _datasetRepo
            .Setup(r => r.CreateAsync(It.IsAny<Dataset>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Dataset d, CancellationToken _) => d);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert — CreateDataset không load children (chỉ có ở GetDataset)
        result.Dimensions.Should().BeEmpty();
        result.Measures.Should().BeEmpty();
        result.Metrics.Should().BeEmpty();
    }
}
