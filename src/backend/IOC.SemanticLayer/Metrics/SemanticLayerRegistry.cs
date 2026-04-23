using Microsoft.Extensions.Logging;

namespace IOC.SemanticLayer.Metrics;

/// <summary>
/// Registry trung tâm cho tất cả MetricDefinitions.
/// Mỗi plugin đăng ký metrics của mình vào đây khi khởi động.
/// </summary>
public sealed class SemanticLayerRegistry
{
    private readonly Dictionary<string, MetricDefinition> _metrics = new();
    private readonly ILogger<SemanticLayerRegistry> _logger;

    public SemanticLayerRegistry(ILogger<SemanticLayerRegistry> logger)
    {
        _logger = logger;
    }

    /// <summary>Đăng ký metric mới</summary>
    public void Register(MetricDefinition metric)
    {
        if (_metrics.ContainsKey(metric.Id))
        {
            _logger.LogWarning("Metric {Id} đã tồn tại. Bỏ qua.", metric.Id);
            return;
        }
        _metrics[metric.Id] = metric;
        _logger.LogDebug("Metric [{Domain}/{Id}] đã đăng ký.", metric.Domain, metric.Id);
    }

    /// <summary>Đăng ký nhiều metrics cùng lúc</summary>
    public void RegisterMany(IEnumerable<MetricDefinition> metrics)
    {
        foreach (var metric in metrics)
            Register(metric);
    }

    public MetricDefinition? GetById(string id) =>
        _metrics.TryGetValue(id, out var m) ? m : null;

    public IReadOnlyList<MetricDefinition> GetAll() =>
        _metrics.Values.ToList().AsReadOnly();

    public IReadOnlyList<MetricDefinition> GetByDomain(string domain) =>
        _metrics.Values.Where(m => m.Domain == domain).ToList().AsReadOnly();
}
