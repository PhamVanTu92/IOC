using DashboardService.Application.DTOs;
using HotChocolate.Types;

namespace Gateway.Schema.Types;

// ─────────────────────────────────────────────────────────────────────────────
// DashboardType / DashboardSummaryType — GraphQL object types
// ─────────────────────────────────────────────────────────────────────────────

public sealed class DashboardType : ObjectType<DashboardDto>
{
    protected override void Configure(IObjectTypeDescriptor<DashboardDto> descriptor)
    {
        descriptor.Name("Dashboard");
        descriptor.Description("A saved dashboard containing one or more chart widgets.");

        descriptor.Field(d => d.Id).Type<NonNullType<UuidType>>();
        descriptor.Field(d => d.TenantId).Type<NonNullType<UuidType>>();
        descriptor.Field(d => d.CreatedBy).Type<NonNullType<UuidType>>();
        descriptor.Field(d => d.Title).Type<NonNullType<StringType>>();
        descriptor.Field(d => d.Description).Type<StringType>();
        descriptor.Field(d => d.ConfigJson)
            .Type<NonNullType<StringType>>()
            .Description("Full serialized DashboardConfig as JSON string.");
        descriptor.Field(d => d.IsActive).Type<NonNullType<BooleanType>>();
        descriptor.Field(d => d.CreatedAt).Type<NonNullType<DateTimeType>>();
        descriptor.Field(d => d.UpdatedAt).Type<NonNullType<DateTimeType>>();
    }
}

public sealed class DashboardSummaryType : ObjectType<DashboardSummaryDto>
{
    protected override void Configure(IObjectTypeDescriptor<DashboardSummaryDto> descriptor)
    {
        descriptor.Name("DashboardSummary");
        descriptor.Description("Lightweight dashboard summary for list views.");

        descriptor.Field(d => d.Id).Type<NonNullType<UuidType>>();
        descriptor.Field(d => d.Title).Type<NonNullType<StringType>>();
        descriptor.Field(d => d.Description).Type<StringType>();
        descriptor.Field(d => d.IsActive).Type<NonNullType<BooleanType>>();
        descriptor.Field(d => d.UpdatedAt).Type<NonNullType<DateTimeType>>();
        descriptor.Field(d => d.WidgetCount).Type<NonNullType<IntType>>();
    }
}
