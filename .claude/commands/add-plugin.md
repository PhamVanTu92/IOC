# /project:add-plugin — Scaffold Plugin Module Mới

**Cú pháp:** `/project:add-plugin <PluginName>`
Ví dụ: `/project:add-plugin Logistics`

## Tạo Backend Plugin

```bash
# 1. Tạo project
mkdir src/backend/Plugins/IOC.$PLUGIN_NAME
cd src/backend/Plugins/IOC.$PLUGIN_NAME
dotnet new classlib -n IOC.$PLUGIN_NAME

# 2. Thêm vào solution
cd src/backend
dotnet sln IOC.sln add Plugins/IOC.$PLUGIN_NAME/IOC.$PLUGIN_NAME.csproj

# 3. Thêm reference IOC.Core
dotnet add Plugins/IOC.$PLUGIN_NAME/IOC.$PLUGIN_NAME.csproj \
  reference IOC.Core/IOC.Core.csproj
```

## Template file cần tạo

### `{PluginName}Plugin.cs`
```csharp
using IOC.Core.Plugins;

namespace IOC.{PluginName};

public class {PluginName}Plugin : IPlugin
{
    public string Name => "{PluginName}";
    public string Version => "1.0.0";

    public void RegisterServices(IServiceCollection services)
    {
        services.AddScoped<I{PluginName}Service, {PluginName}Service>();
    }

    public void RegisterGraphQL(ISchemaBuilder schema)
    {
        schema.AddType<{PluginName}Query>();
        schema.AddType<{PluginName}Mutation>();
    }

    public void RegisterKafkaTopics(IKafkaBuilder kafka)
    {
        kafka.AddTopic($"ioc.{pluginName.ToLower()}.events");
    }
}
```

## Tạo Frontend Plugin

```bash
mkdir -p src/frontend/src/plugins/{plugin-name}
```

### Template `index.ts`
```typescript
import type { IOCPlugin } from '@/core/PluginRegistry';

export const {PluginName}Plugin: IOCPlugin = {
  id: '{plugin-name}',
  name: '{Plugin Display Name}',
  icon: 'icon-name',
  routes: [],
  widgets: [],
  menuItems: [],
};
```

## Đăng ký plugin

**Backend** — `src/backend/IOC.Api/Program.cs`:
```csharp
builder.Services.AddPlugin<{PluginName}Plugin>();
```

**Frontend** — `src/frontend/src/core/PluginRegistry.ts`:
```typescript
import { {PluginName}Plugin } from '../plugins/{plugin-name}';
registry.register({PluginName}Plugin);
```
