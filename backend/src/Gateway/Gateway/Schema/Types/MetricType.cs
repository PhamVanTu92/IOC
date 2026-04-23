using HotChocolate.Types;
using MetadataService.Application.Datasets.DTOs;

namespace Gateway.Schema.Types;

/// <summary>
/// HotChocolate ObjectType cho MetricDto.
/// </summary>
public sealed class MetricType : ObjectType<MetricDto>
{
    protected override void Configure(IObjectTypeDescriptor<MetricDto> descriptor)
    {
        descriptor.Name("Metric");
        descriptor.Description("Metric — computed expression từ một hoặc nhiều measures. Dùng {{measure_name}} làm placeholder.");

        descriptor.Field(m => m.Id);
        descriptor.Field(m => m.DatasetId).Description("Dataset chứa metric này");
        descriptor.Field(m => m.Name).Description("Tên kỹ thuật, unique trong dataset");
        descriptor.Field(m => m.DisplayName).Description("Tên hiển thị trên UI");
        descriptor.Field(m => m.Description);
        descriptor.Field(m => m.Expression)
            .Description("SQL expression với placeholder {{measure_name}} — vd: {{revenue}} / {{orders}}");
        descriptor.Field(m => m.DataType)
            .Description("Kiểu dữ liệu kết quả");
        descriptor.Field(m => m.Format).Description("Format string hiển thị");
        descriptor.Field(m => m.DependsOnMeasures)
            .Description("Danh sách measure names mà metric này phụ thuộc");
        descriptor.Field(m => m.SortOrder);
        descriptor.Field(m => m.IsActive);
    }
}
