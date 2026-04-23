using MetadataService.Domain.Entities;

namespace MetadataService.Domain.Interfaces;

public interface IDatasetRepository
{
    Task<Dataset?> GetByIdAsync(Guid id, Guid tenantId, CancellationToken ct = default);
    Task<Dataset?> GetByNameAsync(string name, Guid tenantId, CancellationToken ct = default);
    Task<IReadOnlyList<Dataset>> ListAsync(Guid tenantId, bool includeInactive = false, CancellationToken ct = default);
    Task<Dataset> CreateAsync(Dataset dataset, CancellationToken ct = default);
    Task<Dataset> UpdateAsync(Dataset dataset, CancellationToken ct = default);
    Task DeleteAsync(Guid id, Guid tenantId, CancellationToken ct = default);
    Task<bool> ExistsAsync(Guid id, Guid tenantId, CancellationToken ct = default);
    Task<bool> ExistsByNameAsync(string name, Guid tenantId, CancellationToken ct = default);
}
