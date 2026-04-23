using FluentAssertions;
using MetadataService.Domain.Entities;

namespace MetadataService.Tests.Domain;

/// <summary>
/// Unit tests cho Metric entity.
/// Lưu ý: Metric.ResolveExpression() nằm trong SemanticEngine.Models.SemanticMetric.
/// Domain Metric chỉ lưu trữ expression string và DependsOnMeasures.
/// </summary>
public sealed class MetricEntityTests
{
    private static readonly Guid _datasetId = Guid.NewGuid();
    private static readonly Guid _tenantId  = Guid.NewGuid();

    // ─── Metric.Create ────────────────────────────────────────────────────

    [Fact]
    public void Create_WithDependsOnMeasures_ShouldStoreThem()
    {
        // Arrange & Act
        var metric = Metric.Create(_datasetId, _tenantId,
            name: "avg_order_value", displayName: "AOV",
            expression: "{{total_revenue}} / {{total_orders}}",
            dependsOnMeasures: ["total_revenue", "total_orders"]);

        // Assert
        metric.DependsOnMeasures.Should().HaveCount(2);
        metric.DependsOnMeasures.Should().Contain("total_revenue");
        metric.DependsOnMeasures.Should().Contain("total_orders");
    }

    [Fact]
    public void Create_WithNullDependsOnMeasures_ShouldStoreEmptyArray()
    {
        // Arrange & Act
        var metric = Metric.Create(_datasetId, _tenantId,
            name: "constant", displayName: "Constant",
            expression: "1000000",
            dependsOnMeasures: null);

        // Assert
        metric.DependsOnMeasures.Should().BeEmpty();
    }

    [Fact]
    public void Create_ShouldStoreExpression_Unchanged()
    {
        // Arrange
        const string expression = "{{revenue}} / NULLIF({{orders}}, 0) * 100";

        // Act
        var metric = Metric.Create(_datasetId, _tenantId,
            name: "conversion_rate", displayName: "Conversion Rate",
            expression: expression);

        // Assert
        metric.Expression.Should().Be(expression);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithEmptyExpression_ShouldThrow(string expression)
    {
        // Act
        var act = () => Metric.Create(_datasetId, _tenantId,
            name: "m", displayName: "M",
            expression: expression);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithEmptyName_ShouldThrow(string name)
    {
        // Act
        var act = () => Metric.Create(_datasetId, _tenantId,
            name: name, displayName: "M",
            expression: "{{x}}");

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_ShouldNormalizeDataType_ToLowercase()
    {
        // Arrange & Act
        var metric = Metric.Create(_datasetId, _tenantId,
            name: "m", displayName: "M",
            expression: "1",
            dataType: "Decimal");

        // Assert
        metric.DataType.Should().Be("decimal");
    }

    [Fact]
    public void Create_ShouldSetIsActiveTrue_ByDefault()
    {
        // Arrange & Act
        var metric = Metric.Create(_datasetId, _tenantId,
            name: "m", displayName: "M",
            expression: "1");

        // Assert
        metric.IsActive.Should().BeTrue();
    }

    // ─── Metric.Update ────────────────────────────────────────────────────

    [Fact]
    public void Update_ShouldChangeExpression_AndDependencies()
    {
        // Arrange
        var metric = Metric.Create(_datasetId, _tenantId,
            name: "aov", displayName: "AOV",
            expression: "{{revenue}} / {{orders}}",
            dependsOnMeasures: ["revenue", "orders"]);

        // Act — cập nhật sang expression khác
        metric.Update(
            displayName: "Updated AOV",
            description: "Updated description",
            expression: "{{revenue}} / NULLIF({{orders}}, 0)",
            dependsOnMeasures: ["revenue", "orders"],
            dataType: "decimal",
            format: "0.00",
            sortOrder: 1);

        // Assert
        metric.DisplayName.Should().Be("Updated AOV");
        metric.Expression.Should().Contain("NULLIF");
        metric.Format.Should().Be("0.00");
        metric.SortOrder.Should().Be(1);
    }

    // ─── Metric.Deactivate ─────────────────────────────────────────────────

    [Fact]
    public void Deactivate_ShouldSetIsActiveFalse()
    {
        // Arrange
        var metric = Metric.Create(_datasetId, _tenantId,
            name: "m", displayName: "M", expression: "1");

        // Act
        metric.Deactivate();

        // Assert
        metric.IsActive.Should().BeFalse();
    }
}
