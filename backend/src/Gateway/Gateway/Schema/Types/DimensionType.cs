using HotChocolate.Types;
using MetadataService.Application.Datasets.DTOs;

namespace Gateway.Schema.Types;

/// <summary>
/// HotChocolate ObjectType cho DimensionDto.
/// </summary>
public sealed class DimensionType : ObjectType<DimensionDto>
{
    protected override void Configure(IObjectTypeDescriptor<DimensionDto> descriptor)
    {
        descriptor.Name("Dimension");
        descriptor.Description("Dimension — field dùng để GROUP BY hoặc filter trong query.");

        descriptor.Field(d => d.Id);
        descriptor.Field(d => d.DatasetId).Description("Dataset chứa dimension này");
        descriptor.Field(d => d.Name).Description("Tên kỹ thuật, unique trong dataset");
        descriptor.Field(d => d.DisplayName).Description("Tên hiển thị trên UI");
        descriptor.Field(d => d.Description);
        descriptor.Field(d => d.ColumnName)
            .Description("Tên cột trong database");
        descriptor.Field(d => d.CustomSqlExpression)
            .Description("SQL expression thay thế column (tuỳ chọn)");
        descriptor.Field(d => d.DataType)
            .Description("Kiểu dữ liệu: string | number | integer | decimal | date | datetime | boolean | json");
        descriptor.Field(d => d.Format).Description("Format string hiển thị");
        descriptor.Field(d => d.IsTimeDimension)
            .Description("Có phải time dimension (hỗ trợ granularity truncation)");
        descriptor.Field(d => d.DefaultGranularity)
            .Description("Granularity mặc định: hour | day | week | month | quarter | year");
        descriptor.Field(d => d.SortOrder).Description("Thứ tự sắp xếp trong danh sách");
        descriptor.Field(d => d.IsActive);
    }
}
