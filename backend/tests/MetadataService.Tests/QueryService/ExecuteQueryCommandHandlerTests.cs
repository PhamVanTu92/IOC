using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using QueryService.Application.ExecuteQuery;
using QueryService.Application.Interfaces;
using SemanticEngine.Builder;
using SemanticEngine.Models;

namespace MetadataService.Tests.QueryService;

/// <summary>
/// Unit tests cho ExecuteQueryCommandHandler — tất cả ports được mock.
/// Kiểm tra pipeline: cache check → SQL build → execute → cache write.
/// </summary>
public sealed class ExecuteQueryCommandHandlerTests
{
    private readonly Mock<ISemanticDatasetLoader> _loader   = new();
    private readonly Mock<IQueryExecutor>          _executor = new();
    private readonly Mock<ICacheService>           _cache    = new();
    private readonly ExecuteQueryCommandHandler    _handler;

    private static readonly Guid TenantId  = Guid.NewGuid();
    private static readonly Guid DatasetId = Guid.NewGuid();

    public ExecuteQueryCommandHandlerTests()
    {
        _handler = new ExecuteQueryCommandHandler(
            _loader.Object,
            _executor.Object,
            _cache.Object,
            NullLogger<ExecuteQueryCommandHandler>.Instance);
    }

    // ─── Test dataset fixture ─────────────────────────────────────────────────

    private static SemanticDataset SimpleDataset() => new()
    {
        Id         = DatasetId,
        TenantId   = TenantId,
        Name       = "orders",
        SourceType = "postgresql",
        SchemaName = "public",
        TableName  = "orders",
        Dimensions = [
            new SemanticDimension
            {
                Name = "region", DisplayName = "Region",
                ColumnName = "region", DataType = DataType.String
            }
        ],
        Measures = [
            new SemanticMeasure
            {
                Name = "revenue", DisplayName = "Revenue",
                ColumnName = "amount", AggregationType = AggregationType.Sum,
                DataType = DataType.Decimal
            }
        ]
    };

    private static QueryInput SimpleInput() => new()
    {
        DatasetId  = DatasetId,
        TenantId   = TenantId,
        Dimensions = ["region"],
        Measures   = ["revenue"]
    };

    private static QueryExecutionResult FakeExecResult(int rowCount = 2) => new()
    {
        Rows = Enumerable.Range(1, rowCount)
            .Select(i => (IReadOnlyDictionary<string, object?>)
                new Dictionary<string, object?> { ["region"] = $"R{i}", ["revenue"] = (object?)(i * 1000m) })
            .ToList(),
        TotalRows = rowCount,
        ExecutionTimeMs = 42
    };

    // ─── Happy path ───────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_WhenCacheMiss_ShouldExecuteAndCacheResult()
    {
        // Arrange
        _loader.Setup(l => l.LoadAsync(DatasetId, TenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(SimpleDataset());

        _cache.Setup(c => c.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string?)null); // cache miss

        _executor.Setup(e => e.ExecuteAsync(It.IsAny<SemanticEngine.Builder.SqlQueryResult>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(FakeExecResult());

        _cache.Setup(c => c.SetAsync(It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _handler.Handle(
            new ExecuteQueryCommand(SimpleInput()), CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Rows.Should().HaveCount(2);
        result.Metadata.FromCache.Should().BeFalse();
        result.Metadata.ExecutionTimeMs.Should().Be(42);
        result.Columns.Should().HaveCount(2); // region + revenue

        // Cache write phải được gọi
        _cache.Verify(c => c.SetAsync(
            It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenCacheHit_ShouldReturnCachedResult_WithoutExecuting()
    {
        // Arrange
        _loader.Setup(l => l.LoadAsync(DatasetId, TenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(SimpleDataset());

        // Prepare cached JSON rows
        var cachedJson = """[{"region":"North","revenue":5000},{"region":"South","revenue":3000}]""";
        _cache.Setup(c => c.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(cachedJson);

        // Act
        var result = await _handler.Handle(
            new ExecuteQueryCommand(SimpleInput()), CancellationToken.None);

        // Assert
        result.Metadata.FromCache.Should().BeTrue();
        result.Rows.Should().HaveCount(2);

        // Executor KHÔNG được gọi khi cache hit
        _executor.Verify(e => e.ExecuteAsync(
            It.IsAny<global::SemanticEngine.Builder.SqlQueryResult>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_WhenForceRefresh_ShouldBypassCache()
    {
        // Arrange — QueryInput is a class, không dùng được 'with'; tạo mới
        var input = new QueryInput
        {
            DatasetId    = DatasetId,
            TenantId     = TenantId,
            Dimensions   = ["region"],
            Measures     = ["revenue"],
            ForceRefresh = true
        };

        _loader.Setup(l => l.LoadAsync(DatasetId, TenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(SimpleDataset());

        _executor.Setup(e => e.ExecuteAsync(It.IsAny<SemanticEngine.Builder.SqlQueryResult>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(FakeExecResult());

        _cache.Setup(c => c.SetAsync(It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _handler.Handle(new ExecuteQueryCommand(input), CancellationToken.None);

        // Assert — GetAsync KHÔNG được gọi khi ForceRefresh
        _cache.Verify(c => c.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
        // Nhưng SetAsync vẫn phải cập nhật cache
        _cache.Verify(c => c.SetAsync(It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    // ─── Error handling ───────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_WhenDatasetNotFound_ShouldThrow()
    {
        // Arrange
        _loader.Setup(l => l.LoadAsync(DatasetId, TenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((SemanticDataset?)null);

        // Act
        var act = async () => await _handler.Handle(
            new ExecuteQueryCommand(SimpleInput()), CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage($"*{DatasetId}*");
    }

    [Fact]
    public async Task Handle_WhenExecutorThrows_ShouldReturnEmptyResultWithError()
    {
        // Arrange
        _loader.Setup(l => l.LoadAsync(DatasetId, TenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(SimpleDataset());

        _cache.Setup(c => c.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string?)null);

        _executor.Setup(e => e.ExecuteAsync(It.IsAny<SemanticEngine.Builder.SqlQueryResult>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Connection timeout"));

        // Act
        var result = await _handler.Handle(
            new ExecuteQueryCommand(SimpleInput()), CancellationToken.None);

        // Assert — không throw, trả về empty result với error message
        result.Rows.Should().BeEmpty();
        result.Metadata.ErrorMessage.Should().Contain("Connection timeout");
    }

    [Fact]
    public async Task Handle_WhenCacheWriteFails_ShouldStillReturnResult()
    {
        // Arrange
        _loader.Setup(l => l.LoadAsync(DatasetId, TenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(SimpleDataset());

        _cache.Setup(c => c.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string?)null);

        _executor.Setup(e => e.ExecuteAsync(It.IsAny<SemanticEngine.Builder.SqlQueryResult>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(FakeExecResult());

        // Cache write fails
        _cache.Setup(c => c.SetAsync(It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Redis unavailable"));

        // Act — không nên throw dù cache fail
        var act = async () => await _handler.Handle(
            new ExecuteQueryCommand(SimpleInput()), CancellationToken.None);

        // Assert
        await act.Should().NotThrowAsync();
        var result = await _handler.Handle(
            new ExecuteQueryCommand(SimpleInput()), CancellationToken.None);
        result.Rows.Should().HaveCount(2);
    }

    // ─── Metadata ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_ShouldIncludeGeneratedSql_InMetadata()
    {
        // Arrange
        _loader.Setup(l => l.LoadAsync(DatasetId, TenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(SimpleDataset());

        _cache.Setup(c => c.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string?)null);

        _executor.Setup(e => e.ExecuteAsync(It.IsAny<SemanticEngine.Builder.SqlQueryResult>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(FakeExecResult());

        _cache.Setup(c => c.SetAsync(It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _handler.Handle(
            new ExecuteQueryCommand(SimpleInput()), CancellationToken.None);

        // Assert
        result.Metadata.GeneratedSql.Should().NotBeNullOrEmpty();
        result.Metadata.GeneratedSql.Should().Contain("SELECT");
        result.Metadata.CacheKey.Should().NotBeNullOrEmpty();
        result.Metadata.ExecutedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
    }
}
