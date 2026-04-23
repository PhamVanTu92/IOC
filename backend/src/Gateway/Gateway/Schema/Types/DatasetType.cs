using HotChocolate.Types;
using MetadataService.Application.Datasets.DTOs;

namespace Gateway.Schema.Types;

/// <summary>
/// HotChocolate ObjectType cho DatasetDto.
/// </summary>
public sealed class DatasetType : ObjectType<DatasetDto>
{
    protected override void Configure(IObjectTypeDescriptor<DatasetDto> descriptor)
    {
        descriptor.Name("Dataset");
        descriptor.Description("Định nghĩa một dataset — nguồn dữ liệu cho Semantic Layer.");

        descriptor.Field(d => d.Id).Description("Dataset ID (UUID)");
        descriptor.Field(d => d.TenantId).Description("Tenant sở hữu dataset");
        descriptor.Field(d => d.Name).Description("Tên kỹ thuật, unique trong tenant");
        descriptor.Field(d => d.Description).Description("Mô tả dataset (tuỳ chọn)");
        descriptor.Field(d => d.SourceType)
            .Description("Loại nguồn: postgresql | view | custom_sql");
        descriptor.Field(d => d.SchemaName)
            .Description("Database schema name (khi SourceType = postgresql/view)");
        descriptor.Field(d => d.TableName)
            .Description("Tên bảng (khi SourceType = postgresql/view)");
        descriptor.Field(d => d.CustomSql)
            .Description("Custom SQL expression (khi SourceType = custom_sql)");
        descriptor.Field(d => d.IsActive).Description("Dataset đang hoạt động");
        descriptor.Field(d => d.CreatedAt).Description("Thời điểm tạo");
        descriptor.Field(d => d.UpdatedAt).Description("Thời điểm cập nhật cuối");
        descriptor.Field(d => d.Dimensions)
            .Description("Danh sách dimensions (GROUP BY fields)");
        descriptor.Field(d => d.Measures)
            .Description("Danh sách measures (aggregate functions)");
        descriptor.Field(d => d.Metrics)
            .Description("Danh sách metrics (computed expressions từ measures)");
    }
}
