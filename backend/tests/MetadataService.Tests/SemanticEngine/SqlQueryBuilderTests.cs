using FluentAssertions;
using SemanticEngine.Builder;
using SemanticEngine.Models;

namespace MetadataService.Tests.Builder;

/// <summary>
/// Unit tests cho SqlQueryBuilder — kiểm tra SQL generation từ QueryInput + SemanticDataset.
/// Không kết nối DB thật — chỉ kiểm tra chuỗi SQL được tạo ra.
/// </summary>
public sealed class SqlQueryBuilderTests
{
    private static readonly Guid TenantId  = Guid.NewGuid();
    private static readonly Guid DatasetId = Guid.NewGuid();

    // ─── Test dataset fixture ─────────────────────────────────────────────────

    private static SemanticDataset BuildDataset(
        string sourceType = "postgresql",
        string? customSql = null) => new()
    {
        Id         = DatasetId,
        TenantId   = TenantId,
        Name       = "orders",
        SourceType = sourceType,
        SchemaName = "public",
        TableName  = "orders",
        CustomSql  = customSql,
        Dimensions =
        [
            new SemanticDimension
            {
                Name = "region", DisplayName = "Region",
                ColumnName = "region", DataType = DataType.String
            },
            new SemanticDimension
            {
                Name = "order_date", DisplayName = "Order Date",
                ColumnName = "created_at", DataType = DataType.DateTime,
                IsTimeDimension = true, DefaultGranularity = TimeGranularity.Month
            },
            new SemanticDimension
            {
                Name = "status", DisplayName = "Status",
                ColumnName = "status", DataType = DataType.String
            }
        ],
        Measures =
        [
            new SemanticMeasure
            {
                Name = "revenue", DisplayName = "Revenue",
                ColumnName = "amount", AggregationType = AggregationType.Sum,
                DataType = DataType.Decimal, Format = "#,##0.00"
            },
            new SemanticMeasure
            {
                Name = "order_count", DisplayName = "Order Count",
                ColumnName = "id", AggregationType = AggregationType.Count,
                DataType = DataType.Integer
            },
            new SemanticMeasure
            {
                Name = "completed_count", DisplayName = "Completed Orders",
                ColumnName = "id", AggregationType = AggregationType.Count,
                FilterExpression = "status = 'completed'",
                DataType = DataType.Integer
            }
        ],
        Metrics =
        [
            new SemanticMetric
            {
                Name = "avg_order_value", DisplayName = "AOV",
                Expression = "{{revenue}} / NULLIF({{order_count}}, 0)",
                DependsOnMeasures = ["revenue", "order_count"],
                DataType = DataType.Decimal
            }
        ]
    };

    // ─── SELECT + FROM ────────────────────────────────────────────────────────

    [Fact]
    public void Build_WithDimensionsAndMeasures_ShouldGenerateCorrectSelectFrom()
    {
        // Arrange
        var dataset = BuildDataset();
        var input = new QueryInput
        {
            DatasetId  = DatasetId,
            TenantId   = TenantId,
            Dimensions = ["region"],
            Measures   = ["revenue"]
        };

        // Act
        var result = SqlQueryBuilder.Build(input, dataset);

        // Assert
        result.Sql.Should().Contain("\"region\"");
        result.Sql.Should().Contain("SUM(\"amount\")");
        result.Sql.Should().Contain("FROM \"public\".\"orders\"");
    }

    [Fact]
    public void Build_WithCustomSqlDataset_ShouldWrapInSubquery()
    {
        // Arrange
        var dataset = BuildDataset(sourceType: "custom_sql",
            customSql: "SELECT * FROM raw_orders WHERE year = 2024");
        var input = new QueryInput
        {
            DatasetId = DatasetId,
            TenantId  = TenantId,
            Measures  = ["revenue"]
        };

        // Act
        var result = SqlQueryBuilder.Build(input, dataset);

        // Assert
        result.Sql.Should().Contain("FROM (SELECT * FROM raw_orders WHERE year = 2024) AS __dataset");
    }

    // ─── WHERE clause ─────────────────────────────────────────────────────────

    [Fact]
    public void Build_ShouldAlwaysIncludeTenantFilter()
    {
        // Arrange
        var input = new QueryInput
        {
            DatasetId = DatasetId,
            TenantId  = TenantId,
            Measures  = ["revenue"]
        };

        // Act
        var result = SqlQueryBuilder.Build(input, BuildDataset());

        // Assert — tenant filter phải luôn có
        result.Sql.Should().Contain("tenant_id = @tenantId");
        result.Parameters.Should().ContainKey("@tenantId")
            .WhoseValue.Should().Be(TenantId);
    }

    [Fact]
    public void Build_WithEqualsFilter_ShouldGenerateWhereClause()
    {
        // Arrange
        var input = new QueryInput
        {
            DatasetId = DatasetId,
            TenantId  = TenantId,
            Dimensions = ["region"],
            Measures   = ["revenue"],
            Filters    =
            [
                new QueryFilter
                {
                    FieldName = "status",
                    Operator  = FilterOperator.Equals,
                    Value     = "completed"
                }
            ]
        };

        // Act
        var result = SqlQueryBuilder.Build(input, BuildDataset());

        // Assert
        result.Sql.Should().Contain("\"status\" = @");
        result.Parameters.Values.Should().Contain("completed");
    }

    [Fact]
    public void Build_WithInFilter_ShouldUseAnyOperator()
    {
        // Arrange
        var input = new QueryInput
        {
            DatasetId = DatasetId,
            TenantId  = TenantId,
            Measures  = ["revenue"],
            Filters   =
            [
                new QueryFilter
                {
                    FieldName = "region",
                    Operator  = FilterOperator.In,
                    Values    = ["north", "south", "east"]
                }
            ]
        };

        // Act
        var result = SqlQueryBuilder.Build(input, BuildDataset());

        // Assert — dùng ANY() PostgreSQL syntax
        result.Sql.Should().Contain("= ANY(");
    }

    [Fact]
    public void Build_WithContainsFilter_ShouldGenerateILike()
    {
        // Arrange
        var input = new QueryInput
        {
            DatasetId = DatasetId,
            TenantId  = TenantId,
            Measures  = ["revenue"],
            Filters   =
            [
                new QueryFilter
                {
                    FieldName = "region",
                    Operator  = FilterOperator.Contains,
                    Value     = "north"
                }
            ]
        };

        // Act
        var result = SqlQueryBuilder.Build(input, BuildDataset());

        // Assert
        result.Sql.Should().Contain("ILIKE");
        result.Parameters.Values.Should().Contain("%north%");
    }

    [Fact]
    public void Build_WithIsNullFilter_ShouldGenerateIsNull()
    {
        // Arrange
        var input = new QueryInput
        {
            DatasetId = DatasetId,
            TenantId  = TenantId,
            Measures  = ["revenue"],
            Filters   =
            [
                new QueryFilter
                {
                    FieldName = "region",
                    Operator  = FilterOperator.IsNull
                }
            ]
        };

        // Act
        var result = SqlQueryBuilder.Build(input, BuildDataset());

        // Assert
        result.Sql.Should().Contain("IS NULL");
    }

    // ─── GROUP BY ─────────────────────────────────────────────────────────────

    [Fact]
    public void Build_WithDimensions_ShouldGenerateGroupBy()
    {
        // Arrange
        var input = new QueryInput
        {
            DatasetId  = DatasetId,
            TenantId   = TenantId,
            Dimensions = ["region", "status"],
            Measures   = ["revenue"]
        };

        // Act
        var result = SqlQueryBuilder.Build(input, BuildDataset());

        // Assert
        result.Sql.Should().Contain("GROUP BY");
        result.Sql.Should().Contain("\"region\"");
        result.Sql.Should().Contain("\"status\"");
    }

    [Fact]
    public void Build_WithoutDimensions_ShouldNotHaveGroupBy()
    {
        // Arrange
        var input = new QueryInput
        {
            DatasetId = DatasetId,
            TenantId  = TenantId,
            Measures  = ["revenue"]
        };

        // Act
        var result = SqlQueryBuilder.Build(input, BuildDataset());

        // Assert
        result.Sql.Should().NotContain("GROUP BY");
    }

    // ─── Time dimension ───────────────────────────────────────────────────────

    [Fact]
    public void Build_WithTimeDimension_ShouldUseDateTrunc()
    {
        // Arrange
        var input = new QueryInput
        {
            DatasetId         = DatasetId,
            TenantId          = TenantId,
            Dimensions        = ["order_date"],
            Measures          = ["revenue"],
            Granularity       = TimeGranularity.Month,
            TimeDimensionName = "order_date",
            TimeRange = new TimeRange { Preset = "last30days" }
        };

        // Act
        var result = SqlQueryBuilder.Build(input, BuildDataset());

        // Assert
        result.Sql.Should().Contain("DATE_TRUNC('month'");
        result.Sql.Should().Contain("@tFrom");
        result.Sql.Should().Contain("@tTo");
    }

    // ─── ORDER BY + LIMIT ─────────────────────────────────────────────────────

    [Fact]
    public void Build_WithSort_ShouldGenerateOrderBy()
    {
        // Arrange
        var input = new QueryInput
        {
            DatasetId  = DatasetId,
            TenantId   = TenantId,
            Dimensions = ["region"],
            Measures   = ["revenue"],
            Sorts =
            [
                new QuerySort { FieldName = "revenue", Direction = SortDirection.Descending }
            ]
        };

        // Act
        var result = SqlQueryBuilder.Build(input, BuildDataset());

        // Assert
        result.Sql.Should().Contain("ORDER BY");
        result.Sql.Should().Contain("\"revenue\" DESC");
    }

    [Fact]
    public void Build_ShouldIncludeLimitAndOffset()
    {
        // Arrange
        var input = new QueryInput
        {
            DatasetId = DatasetId,
            TenantId  = TenantId,
            Measures  = ["revenue"],
            Limit     = 500,
            Offset    = 100
        };

        // Act
        var result = SqlQueryBuilder.Build(input, BuildDataset());

        // Assert
        result.Sql.Should().Contain("LIMIT 500");
        result.Sql.Should().Contain("OFFSET 100");
    }

    // ─── Metrics ─────────────────────────────────────────────────────────────

    [Fact]
    public void Build_WithMetric_ShouldResolvePlaceholders()
    {
        // Arrange — avg_order_value = {{revenue}} / NULLIF({{order_count}}, 0)
        var input = new QueryInput
        {
            DatasetId  = DatasetId,
            TenantId   = TenantId,
            Dimensions = ["region"],
            Measures   = ["revenue", "order_count"],
            Metrics    = ["avg_order_value"]
        };

        // Act
        var result = SqlQueryBuilder.Build(input, BuildDataset());

        // Assert — placeholders resolved sang SQL aggregate
        result.Sql.Should().NotContain("{{");
        result.Sql.Should().Contain("SUM(\"amount\")");
        result.Sql.Should().Contain("NULLIF");
    }

    [Fact]
    public void Build_WithMetricOnly_ShouldAutoResolveDependentMeasures()
    {
        // Arrange — chỉ request metric, không request measure explicitly
        var input = new QueryInput
        {
            DatasetId  = DatasetId,
            TenantId   = TenantId,
            Dimensions = ["region"],
            Metrics    = ["avg_order_value"]
            // revenue và order_count không có trong Measures list
        };

        // Act
        var result = SqlQueryBuilder.Build(input, BuildDataset());

        // Assert — metric vẫn được resolve
        result.Sql.Should().NotContain("{{");
        result.Columns.Should().ContainSingle(c => c.Name == "avg_order_value");
        result.Columns.Should().ContainSingle(c => c.FieldType == "metric");
    }

    // ─── Column descriptors ───────────────────────────────────────────────────

    [Fact]
    public void Build_ShouldReturnCorrectColumnDescriptors()
    {
        // Arrange
        var input = new QueryInput
        {
            DatasetId  = DatasetId,
            TenantId   = TenantId,
            Dimensions = ["region"],
            Measures   = ["revenue"]
        };

        // Act
        var result = SqlQueryBuilder.Build(input, BuildDataset());

        // Assert
        result.Columns.Should().HaveCount(2);

        var dimCol = result.Columns.Single(c => c.Name == "region");
        dimCol.FieldType.Should().Be("dimension");
        dimCol.DisplayName.Should().Be("Region");

        var mCol = result.Columns.Single(c => c.Name == "revenue");
        mCol.FieldType.Should().Be("measure");
        mCol.Format.Should().Be("#,##0.00");
    }

    // ─── Validation ───────────────────────────────────────────────────────────

    [Fact]
    public void Build_WithNonExistentDimension_ShouldThrow()
    {
        // Arrange
        var input = new QueryInput
        {
            DatasetId  = DatasetId,
            TenantId   = TenantId,
            Dimensions = ["nonexistent_dimension"],
            Measures   = ["revenue"]
        };

        // Act
        var act = () => SqlQueryBuilder.Build(input, BuildDataset());

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*nonexistent_dimension*");
    }

    [Fact]
    public void Build_WithNonExistentMeasure_ShouldThrow()
    {
        // Arrange
        var input = new QueryInput
        {
            DatasetId = DatasetId,
            TenantId  = TenantId,
            Measures  = ["nonexistent_measure"]
        };

        // Act
        var act = () => SqlQueryBuilder.Build(input, BuildDataset());

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*nonexistent_measure*");
    }

    // ─── Conditional aggregate (FILTER) ───────────────────────────────────────

    [Fact]
    public void Build_WithFilteredMeasure_ShouldIncludeFilterClause()
    {
        // Arrange
        var input = new QueryInput
        {
            DatasetId = DatasetId,
            TenantId  = TenantId,
            Measures  = ["completed_count"]
        };

        // Act
        var result = SqlQueryBuilder.Build(input, BuildDataset());

        // Assert — FILTER (WHERE ...) clause phải xuất hiện trong SELECT
        result.Sql.Should().Contain("FILTER (WHERE status = 'completed')");
    }
}
