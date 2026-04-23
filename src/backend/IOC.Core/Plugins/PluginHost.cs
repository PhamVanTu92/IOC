using HotChocolate.Execution.Configuration;
using IOC.Core.Kafka;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace IOC.Core.Plugins;

/// <summary>
/// PluginHost — quản lý vòng đời và đăng ký của tất cả IOC plugins.
/// </summary>
public sealed class PluginHost
{
    private readonly List<IPlugin> _plugins = new();
    private readonly ILogger<PluginHost> _logger;

    public PluginHost(ILogger<PluginHost> logger)
    {
        _logger = logger;
    }

    public IReadOnlyList<IPlugin> Plugins => _plugins.AsReadOnly();

    /// <summary>Đăng ký một plugin vào host</summary>
    public PluginHost Register(IPlugin plugin)
    {
        if (_plugins.Any(p => p.Id == plugin.Id))
        {
            _logger.LogWarning("Plugin {Id} đã được đăng ký. Bỏ qua.", plugin.Id);
            return this;
        }

        _plugins.Add(plugin);
        _logger.LogInformation("Plugin [{Name}] v{Version} đã đăng ký.", plugin.Name, plugin.Version);
        return this;
    }

    /// <summary>Gọi RegisterServices cho tất cả plugins</summary>
    public void ConfigureServices(IServiceCollection services)
    {
        foreach (var plugin in _plugins)
        {
            _logger.LogDebug("Configuring services for plugin: {Name}", plugin.Name);
            plugin.RegisterServices(services);
        }
    }

    /// <summary>Gọi RegisterGraphQL cho tất cả plugins</summary>
    public void ConfigureGraphQL(IRequestExecutorBuilder builder)
    {
        foreach (var plugin in _plugins)
        {
            _logger.LogDebug("Configuring GraphQL for plugin: {Name}", plugin.Name);
            plugin.RegisterGraphQL(builder);
        }
    }

    /// <summary>Gọi RegisterKafka cho tất cả plugins</summary>
    public void ConfigureKafka(IKafkaBuilder kafkaBuilder)
    {
        foreach (var plugin in _plugins)
        {
            _logger.LogDebug("Configuring Kafka for plugin: {Name}", plugin.Name);
            plugin.RegisterKafka(kafkaBuilder);
        }
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// Extension methods để dùng trong Program.cs
// ─────────────────────────────────────────────────────────────────────────────

public static class PluginHostExtensions
{
    /// <summary>
    /// Đăng ký PluginHost và tất cả plugins vào DI container.
    /// Usage: builder.Services.AddPlugin&lt;FinancePlugin&gt;();
    /// </summary>
    public static IServiceCollection AddPlugin<TPlugin>(this IServiceCollection services)
        where TPlugin : class, IPlugin, new()
    {
        services.AddSingleton<IPlugin, TPlugin>();
        return services;
    }
}
