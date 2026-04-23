using SemanticEngine.Models;

namespace QueryService.Application.Interfaces;

/// <summary>
/// Port — load SemanticDataset từ metadata store.
/// Implemented bởi QueryService.Infrastructure, inject vào Application handler.
/// </summary>
public interface ISemanticDatasetLoader
{
    /// <summary>
    /// Load toàn bộ SemanticDataset (kèm dimensions, measures, metrics)
    /// từ persistent store. Trả về null nếu không tìm thấy.
    /// </summary>
    Task<SemanticDataset?> LoadAsync(
        Guid datasetId,
        Guid tenantId,
        CancellationToken ct = default);
}
