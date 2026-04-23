# /project:gen-module — Generate CRUD Module

**Cú pháp:** `/project:gen-module <PluginName> <EntityName>`
Ví dụ: `/project:gen-module Finance Budget`

## Tự động tạo

### Backend
- `{Entity}.cs` — EF Core model
- `{Entity}Repository.cs` + `I{Entity}Repository.cs`
- `{Entity}Service.cs` + `I{Entity}Service.cs`
- `{Entity}Query.cs` — GraphQL Query type
- `{Entity}Mutation.cs` — GraphQL Mutation type
- `{Entity}Subscription.cs` — GraphQL Subscription (Kafka bridge)
- `{Entity}Tests.cs` — xUnit unit tests

### Frontend
- `{Entity}List.tsx` — Danh sách với ECharts chart
- `{Entity}Detail.tsx` — Chi tiết / form
- `use{Entity}.ts` — Custom hook (GraphQL query)
- `{entity}.graphql` — GraphQL operations
- `{Entity}.test.tsx` — React Testing Library tests

## Semantic Layer
Tạo metric definition trong `IOC.SemanticLayer`:
```csharp
new MetricDefinition
{
    Name = "{Entity}Count",
    Domain = "{PluginName}",
    Aggregation = AggregationType.Count,
    Source = "{TableName}"
}
```
