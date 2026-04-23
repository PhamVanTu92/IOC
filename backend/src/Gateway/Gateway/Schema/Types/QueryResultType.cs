using HotChocolate.Types;
using SemanticEngine.Models;

namespace Gateway.Schema.Types;

/// <summary>
/// GraphQL wrapper cho QueryResult — kết quả dynamic query từ Semantic Layer.
/// </summary>
public sealed class QueryResultType : ObjectType<QueryResult>
{
    protected override void Configure(IObjectTypeDescriptor<QueryResult> descriptor)
    {
        descriptor.Name("QueryResult");
        descriptor.Description("Kết quả thực thi một dynamic query từ Semantic Layer.");

        descriptor.Field(r => r.Columns)
            .Description("Danh sách cột trong kết quả");

        // Rows là List<Dictionary<string, object?>> — không thể map trực tiếp lên GraphQL scalar.
        // Serialize mỗi row thành JSON string để frontend tự parse.
        descriptor.Field("rows")
            .Description("Dữ liệu kết quả — mỗi row được serialize thành JSON string")
            .Resolve(ctx =>
            {
                var result = ctx.Parent<QueryResult>();
                return result.Rows
                    .Select(row => System.Text.Json.JsonSerializer.Serialize(row))
                    .ToList();
            })
            .Type<ListType<NonNullType<StringType>>>();

        descriptor.Field(r => r.Metadata)
            .Description("Metadata thực thi: SQL, execution time, cache info...");
    }
}

/// <summary>
/// HotChocolate ObjectType cho QueryResultColumn.
/// </summary>
public sealed class QueryResultColumnType : ObjectType<QueryResultColumn>
{
    protected override void Configure(IObjectTypeDescriptor<QueryResultColumn> descriptor)
    {
        descriptor.Name("QueryResultColumn");
        descriptor.Field(c => c.Name).Description("Tên kỹ thuật của cột");
        descriptor.Field(c => c.DisplayName).Description("Tên hiển thị");
        descriptor.Field(c => c.DataType).Description("Kiểu dữ liệu: string | number | date...");
        descriptor.Field(c => c.Format).Description("Format string");
        descriptor.Field(c => c.FieldType)
            .Description("Loại field: dimension | measure | metric");
    }
}

/// <summary>
/// HotChocolate ObjectType cho QueryExecutionMetadata.
/// </summary>
public sealed class QueryExecutionMetadataType : ObjectType<QueryExecutionMetadata>
{
    protected override void Configure(IObjectTypeDescriptor<QueryExecutionMetadata> descriptor)
    {
        descriptor.Name("QueryExecutionMetadata");
        descriptor.Field(m => m.GeneratedSql).Description("SQL đã được generate (chỉ hiển thị trong dev)");
        descriptor.Field(m => m.ExecutionTimeMs).Description("Thời gian thực thi (milliseconds)");
        descriptor.Field(m => m.TotalRows).Description("Tổng số rows trong kết quả");
        descriptor.Field(m => m.FromCache).Description("Kết quả có lấy từ Redis cache không");
        descriptor.Field(m => m.CacheKey).Description("Cache key đã dùng");
        descriptor.Field(m => m.ExecutedAt).Description("Thời điểm thực thi");
        descriptor.Field(m => m.ErrorMessage).Description("Error message (nếu có lỗi)");

        // Bỏ qua Parameters (Dictionary<string, object?>) — quá phức tạp cho GraphQL
        descriptor.Ignore(m => m.Parameters);
    }
}
