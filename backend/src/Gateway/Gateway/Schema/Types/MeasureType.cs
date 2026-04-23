using HotChocolate.Types;
using MetadataService.Application.Datasets.DTOs;

namespace Gateway.Schema.Types;

/// <summary>
/// HotChocolate ObjectType cho MeasureDto.
/// </summary>
public sealed class MeasureType : ObjectType<MeasureDto>
{
    protected override void Configure(IObjectTypeDescriptor<MeasureDto> descriptor)
    {
        descriptor.Name("Measure");
        descriptor.Description("Measure — aggregate function áp dụng trên một cột (SUM, AVG, COUNT...).");

        descriptor.Field(m => m.Id);
        descriptor.Field(m => m.DatasetId).Description("Dataset chứa measure này");
        descriptor.Field(m => m.Name).Description("Tên kỹ thuật, unique trong dataset");
        descriptor.Field(m => m.DisplayName).Description("Tên hiển thị trên UI");
        descriptor.Field(m => m.Description);
        descriptor.Field(m => m.ColumnName).Description("Tên cột để aggregate");
        descriptor.Field(m => m.CustomSqlExpression)
            .Description("SQL expression thay thế column (tuỳ chọn)");
        descriptor.Field(m => m.AggregationType)
            .Description("Hàm aggregate: Sum | Average | Count | CountDistinct | Min | Max | RunningTotal | PercentOfTotal");
        descriptor.Field(m => m.DataType)
            .Description("Kiểu dữ liệu kết quả: string | number | integer | decimal | date | datetime | boolean | json");
        descriptor.Field(m => m.Format).Description("Format string hiển thị");
        descriptor.Field(m => m.FilterExpression)
            .Description("FILTER (WHERE ...) condition cho conditional aggregation");
        descriptor.Field(m => m.SortOrder);
        descriptor.Field(m => m.IsActive);
    }
}
