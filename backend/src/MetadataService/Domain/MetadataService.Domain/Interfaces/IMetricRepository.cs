using MetadataService.Domain.Entities;

namespace MetadataService.Domain.Interfaces;

public interface IMetricRepository
{
    Task<Metric?> GetByIdAsync(Guid id, Guid tenantId, CancellationToken ct = default);
    Task<IReadOnlyList<Metric>> ListByDatasetAsync(Guid datasetId, Guid tenantId, CancellationToken ct = default);
    Task<Metric> CreateAsync(Metric metric, CancellationToken ct = default);
    Task<Metric> UpdateAsync(Metric metric, CancellationToken ct = default);
    Task DeleteAsync(Guid id, Guid tenantId, CancellationToken ct = default);
    Task<bool> ExistsByNameAsync(string name, Guid datasetId, Guid tenantId, CancellationToken ct = default);
}
