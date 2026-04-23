# Plugin System Architecture

## Tổng quan

IOC sử dụng **Plugin Architecture** cho phép thêm/bớt module nghiệp vụ mà không cần
thay đổi core framework.

## Backend Plugin Lifecycle

```
Program.cs
  └── services.AddPlugin<FinancePlugin>()
        └── PluginHost.Register(plugin)
              ├── plugin.RegisterServices(IServiceCollection)
              ├── plugin.RegisterGraphQL(IRequestExecutorBuilder)
              └── plugin.RegisterKafka(IKafkaBuilder)
```

## Tạo plugin mới

### 1. Tạo project
```bash
mkdir src/backend/Plugins/IOC.MyModule
cd src/backend/Plugins/IOC.MyModule
dotnet new classlib -n IOC.MyModule
dotnet sln ../IOC.sln add IOC.MyModule.csproj
dotnet add reference ../../IOC.Core/IOC.Core.csproj
```

### 2. Implement IPlugin
```csharp
public sealed class MyModulePlugin : IPlugin
{
    public string Id => "my-module";
    public string Name => "My Module";
    public string Version => "1.0.0";
    public string Description => "...";

    public void RegisterServices(IServiceCollection services)
    {
        services.AddScoped<IMyService, MyService>();
    }

    public void RegisterGraphQL(IRequestExecutorBuilder builder)
    {
        builder.AddTypeExtension<MyModuleQuery>();
        builder.AddTypeExtension<MyModuleMutation>();
    }

    public void RegisterKafka(IKafkaBuilder kafka)
    {
        kafka.AddTopic("ioc.my-module.events");
    }
}
```

### 3. Đăng ký vào API
```csharp
// IOC.Api/Program.cs
builder.Services.AddPlugin<MyModulePlugin>();
```

## Frontend Plugin Lifecycle

```
App.tsx
  └── pluginRegistry.register(MyModulePlugin)
        └── AppShell initializes
              ├── pluginRegistry.getAllRoutes()  → React Router
              ├── pluginRegistry.getAllMenuItems() → Sidebar
              └── pluginRegistry.getAllWidgets()  → DashboardBuilder
```

## Dependency Rules

```
IOC.Api → IOC.Core ✓
IOC.Api → IOC.SemanticLayer ✓
IOC.Api → Plugins/* ✓
Plugins/* → IOC.Core ✓
Plugins/* → IOC.SemanticLayer ✓
Plugins/* → IOC.Api ✗  (không được phép — circular!)
Plugins/* → other Plugins/* ✗  (không được phép — coupling!)
```

## Semantic Layer Integration

Mỗi plugin đăng ký metrics của mình vào `SemanticLayerRegistry`:

```csharp
// Trong RegisterServices:
var registry = services.BuildServiceProvider().GetRequiredService<SemanticLayerRegistry>();
registry.RegisterMany([
    new MetricDefinition {
        Id = "finance.revenue",
        Name = "Doanh thu",
        Domain = "finance",
        SourceTable = "Invoices",
        SourceColumn = "Amount",
        Aggregation = AggregationType.Sum,
        Unit = "VND"
    }
]);
```
