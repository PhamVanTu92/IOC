using IOC.Core.Plugins;
using IOC.SemanticLayer.Metrics;

namespace IOC.Api.Schema;

/// <summary>
/// Root GraphQL Query — các query cốt lõi của IOC platform.
/// Plugins mở rộng bằng cách AddTypeExtension&lt;PluginQueryExtension&gt;
/// </summary>
[ExtendObjectType("Query")]
public sealed class CoreQuery
{
    /// <summary>Lấy danh sách plugins đang active</summary>
    public IReadOnlyList<PluginInfo> Plugins([Service] PluginHost pluginHost) =>
        pluginHost.Plugins
            .Select(p => new PluginInfo(p.Id, p.Name, p.Version, p.Description))
            .ToList();

    /// <summary>Lấy tất cả metric definitions từ Semantic Layer</summary>
    public IReadOnlyList<MetricDefinition> Metrics([Service] SemanticLayerRegistry registry) =>
        registry.GetAll();

    /// <summary>Lấy metrics theo domain</summary>
    public IReadOnlyList<MetricDefinition> MetricsByDomain(
        string domain,
        [Service] SemanticLayerRegistry registry
    ) => registry.GetByDomain(domain);
}

/// <summary>DTO trả về thông tin plugin qua GraphQL</summary>
public record PluginInfo(string Id, string Name, string Version, string Description);
