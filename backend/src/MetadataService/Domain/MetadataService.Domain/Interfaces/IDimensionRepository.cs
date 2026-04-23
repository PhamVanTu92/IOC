using MetadataService.Domain.Entities;

namespace MetadataService.Domain.Interfaces;

public interface IDimensionRepository
{
    Task<Dimension?> GetByIdAsync(Guid id, Guid tenantId, CancellationToken ct = default);
    Task<IReadOnlyList<Dimension>> ListByDatasetAsync(Guid datasetId, Guid tenantId, CancellationToken ct = default);
    Task<Dimension> CreateAsync(Dimension dimension, CancellationToken ct = default);
    Task<Dimension> UpdateAsync(Dimension dimension, CancellationToken ct = default);
    Task DeleteAsync(Guid id, Guid tenantId, CancellationToken ct = default);
    Task<bool> ExistsByNameAsync(string name, Guid datasetId, Guid tenantId, CancellationToken ct = default);
}
