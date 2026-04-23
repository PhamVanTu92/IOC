using FluentAssertions;
using MetadataService.Domain.Entities;

namespace MetadataService.Tests.Domain;

/// <summary>
/// Unit tests cho Dataset aggregate root.
/// Kiểm tra factory method, business rules, state transitions.
/// </summary>
public sealed class DatasetEntityTests
{
    private static readonly Guid _tenantId  = Guid.NewGuid();
    private static readonly Guid _createdBy = Guid.NewGuid();

    // ─── Dataset.Create ────────────────────────────────────────────────────

    [Fact]
    public void Create_WithValidTableSource_ShouldReturnActiveDataset()
    {
        // Arrange & Act
        var dataset = Dataset.Create(
            tenantId:   _tenantId,
            name:       "sales_orders",
            sourceType: "postgresql",
            createdBy:  _createdBy,
            schemaName: "public",
            tableName:  "orders"
        );

        // Assert
        dataset.Id.Should().NotBe(Guid.Empty);
        dataset.TenantId.Should().Be(_tenantId);
        dataset.Name.Should().Be("sales_orders");
        dataset.SourceType.Should().Be("postgresql");
        dataset.SchemaName.Should().Be("public");
        dataset.TableName.Should().Be("orders");
        dataset.IsActive.Should().BeTrue();
        dataset.CreatedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
        dataset.CustomSql.Should().BeNull();
    }

    [Fact]
    public void Create_WithCustomSqlSource_ShouldSetCustomSql()
    {
        // Arrange
        const string customSql = "SELECT * FROM orders WHERE status = 'active'";

        // Act
        var dataset = Dataset.Create(
            tenantId:   _tenantId,
            name:       "active_orders",
            sourceType: "custom_sql",
            createdBy:  _createdBy,
            customSql:  customSql
        );

        // Assert
        dataset.SourceType.Should().Be("custom_sql");
        dataset.CustomSql.Should().Be(customSql);
        dataset.TableName.Should().BeNull();
        dataset.SchemaName.Should().BeNull();
    }

    [Fact]
    public void Create_WithPostgresqlSource_RequiresTableName()
    {
        // Act — thiếu tableName
        var act = () => Dataset.Create(
            tenantId:   _tenantId,
            name:       "dataset",
            sourceType: "postgresql",
            createdBy:  _createdBy
            // tableName: null — intentionally omitted
        );

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*TableName*");
    }

    [Fact]
    public void Create_WithCustomSqlSource_RequiresCustomSql()
    {
        // Act — thiếu customSql
        var act = () => Dataset.Create(
            tenantId:   _tenantId,
            name:       "dataset",
            sourceType: "custom_sql",
            createdBy:  _createdBy
            // customSql: null — intentionally omitted
        );

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*CustomSql*");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithEmptyName_ShouldThrow(string name)
    {
        // Act
        var act = () => Dataset.Create(
            tenantId:   _tenantId,
            name:       name,
            sourceType: "postgresql",
            createdBy:  _createdBy,
            tableName:  "t"
        );

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_ShouldNormalizeName_ByTrimming()
    {
        // Arrange & Act
        var dataset = Dataset.Create(_tenantId, "  orders  ", "postgresql", _createdBy,
            tableName: "orders");

        // Assert
        dataset.Name.Should().Be("orders");
    }

    [Fact]
    public void Create_ShouldNormalizeSourceType_ToLowercase()
    {
        // Arrange & Act
        var dataset = Dataset.Create(_tenantId, "ds", "PostgreSQL", _createdBy,
            tableName: "t");

        // Assert
        dataset.SourceType.Should().Be("postgresql");
    }

    // ─── Dataset.GetFromExpression ─────────────────────────────────────────

    [Fact]
    public void GetFromExpression_WithSchemaAndTable_ShouldReturnQuotedSchemaTable()
    {
        // Arrange
        var dataset = Dataset.Create(_tenantId, "orders", "postgresql", _createdBy,
            schemaName: "public", tableName: "orders");

        // Act
        var expr = dataset.GetFromExpression();

        // Assert
        expr.Should().Be("\"public\".\"orders\"");
    }

    [Fact]
    public void GetFromExpression_WithTableOnly_ShouldReturnQuotedTable()
    {
        // Arrange
        var dataset = Dataset.Create(_tenantId, "orders", "postgresql", _createdBy,
            tableName: "orders");

        // Act
        var expr = dataset.GetFromExpression();

        // Assert
        expr.Should().Be("\"orders\"");
    }

    [Fact]
    public void GetFromExpression_WithCustomSql_ShouldWrapInSubquery()
    {
        // Arrange
        const string sql = "SELECT id, amount FROM raw_orders";
        var dataset = Dataset.Create(_tenantId, "orders", "custom_sql", _createdBy,
            customSql: sql);

        // Act
        var expr = dataset.GetFromExpression();

        // Assert
        expr.Should().Be($"({sql}) AS __ds");
    }

    // ─── Dataset.Update ────────────────────────────────────────────────────

    [Fact]
    public void Update_ShouldChangeName_AndBumpUpdatedAt()
    {
        // Arrange
        var dataset = Dataset.Create(_tenantId, "old_name", "postgresql", _createdBy,
            tableName: "t");
        var originalUpdatedAt = dataset.UpdatedAt;

        // Act
        dataset.Update(name: "new_name", description: "new desc",
            schemaName: null, tableName: "t", customSql: null);

        // Assert
        dataset.Name.Should().Be("new_name");
        dataset.Description.Should().Be("new desc");
        dataset.UpdatedAt.Should().BeOnOrAfter(originalUpdatedAt);
    }

    // ─── Dataset.Deactivate ────────────────────────────────────────────────

    [Fact]
    public void Deactivate_ShouldSetIsActiveFalse()
    {
        // Arrange
        var dataset = Dataset.Create(_tenantId, "orders", "postgresql", _createdBy,
            tableName: "orders");

        // Act
        dataset.Deactivate();

        // Assert
        dataset.IsActive.Should().BeFalse();
    }

    [Fact]
    public void Activate_AfterDeactivate_ShouldRestoreActive()
    {
        // Arrange
        var dataset = Dataset.Create(_tenantId, "orders", "postgresql", _createdBy,
            tableName: "orders");
        dataset.Deactivate();

        // Act
        dataset.Activate();

        // Assert
        dataset.IsActive.Should().BeTrue();
    }
}
