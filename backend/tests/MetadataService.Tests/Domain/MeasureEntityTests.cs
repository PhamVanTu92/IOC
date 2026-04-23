using FluentAssertions;
using MetadataService.Domain.Entities;

namespace MetadataService.Tests.Domain;

/// <summary>
/// Unit tests cho Measure entity — đặc biệt là GetAggregateExpression().
/// Lưu ý: AggregationType được normalize to lowercase trong entity.
/// Column name được double-quoted trong SQL output.
/// </summary>
public sealed class MeasureEntityTests
{
    private static readonly Guid _datasetId = Guid.NewGuid();
    private static readonly Guid _tenantId  = Guid.NewGuid();

    // ─── GetAggregateExpression ────────────────────────────────────────────

    [Fact]
    public void GetAggregateExpression_Sum_ShouldReturnSumExpression()
    {
        // Arrange — dùng lowercase vì entity normalize về lowercase
        var measure = Measure.Create(_datasetId, _tenantId,
            name: "total_revenue", displayName: "Total Revenue",
            columnName: "amount", aggregationType: "sum");

        // Act
        var expr = measure.GetAggregateExpression();

        // Assert — column được double-quoted trong SQL
        expr.Should().Be("SUM(\"amount\")");
    }

    [Fact]
    public void GetAggregateExpression_CountDistinct_ShouldReturnCorrectSQL()
    {
        // Arrange — dùng "count_distinct" (snake_case như trong switch case)
        var measure = Measure.Create(_datasetId, _tenantId,
            name: "unique_customers", displayName: "Unique Customers",
            columnName: "customer_id", aggregationType: "count_distinct");

        // Act
        var expr = measure.GetAggregateExpression();

        // Assert
        expr.Should().Be("COUNT(DISTINCT \"customer_id\")");
    }

    [Fact]
    public void GetAggregateExpression_WithFilter_ShouldAppendFilterClause()
    {
        // Arrange
        var measure = Measure.Create(_datasetId, _tenantId,
            name: "completed_orders", displayName: "Completed Orders",
            columnName: "id", aggregationType: "count",
            filterExpression: "status = 'completed'");

        // Act
        var expr = measure.GetAggregateExpression();

        // Assert — FILTER (WHERE ...) clause sau aggregate function
        expr.Should().Be("COUNT(\"id\") FILTER (WHERE status = 'completed')");
    }

    [Fact]
    public void GetAggregateExpression_WithCustomSql_ShouldUseCustomExpression()
    {
        // Arrange — CustomSqlExpression thay thế ColumnName
        var measure = Measure.Create(_datasetId, _tenantId,
            name: "weighted_avg", displayName: "Weighted Average",
            columnName: "amount", aggregationType: "sum",
            customSqlExpression: "amount * weight");

        // Act
        var expr = measure.GetAggregateExpression();

        // Assert — CustomSql không được double-quoted
        expr.Should().Be("SUM(amount * weight)");
    }

    [Theory]
    [InlineData("sum",            "SUM(\"col\")")]
    [InlineData("average",        "AVG(\"col\")")]
    [InlineData("count",          "COUNT(\"col\")")]
    [InlineData("count_distinct", "COUNT(DISTINCT \"col\")")]
    [InlineData("min",            "MIN(\"col\")")]
    [InlineData("max",            "MAX(\"col\")")]
    public void GetAggregateExpression_AllAggregationTypes_ShouldReturnCorrectSQL(
        string aggregationType, string expectedSql)
    {
        // Arrange
        var measure = Measure.Create(_datasetId, _tenantId,
            name: "m", displayName: "M",
            columnName: "col", aggregationType: aggregationType);

        // Act & Assert
        measure.GetAggregateExpression().Should().Be(expectedSql);
    }

    // ─── Measure.Create ────────────────────────────────────────────────────

    [Fact]
    public void Create_ShouldNormalizeAggregationType_ToLowercase()
    {
        // Arrange & Act
        var measure = Measure.Create(_datasetId, _tenantId,
            name: "revenue", displayName: "Revenue",
            columnName: "amount", aggregationType: "SUM");

        // Assert
        measure.AggregationType.Should().Be("sum");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithEmptyColumnName_ShouldThrow(string columnName)
    {
        // Act
        var act = () => Measure.Create(_datasetId, _tenantId,
            name: "m", displayName: "M",
            columnName: columnName, aggregationType: "sum");

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void GetAggregateExpression_WithFilterAndCustomSql_ShouldCombineCorrectly()
    {
        // Arrange
        var measure = Measure.Create(_datasetId, _tenantId,
            name: "active_revenue", displayName: "Active Revenue",
            columnName: "amount", aggregationType: "sum",
            filterExpression: "is_active = true",
            customSqlExpression: "unit_price * quantity");

        // Act
        var expr = measure.GetAggregateExpression();

        // Assert
        expr.Should().Be("SUM(unit_price * quantity) FILTER (WHERE is_active = true)");
    }
}
