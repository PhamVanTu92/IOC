namespace QueryService.Application.Interfaces;

/// <summary>
/// Port — cache layer cho query results (Redis).
/// </summary>
public interface ICacheService
{
    /// <summary>Lấy giá trị từ cache. Trả về null nếu miss hoặc expired.</summary>
    Task<string?> GetAsync(string key, CancellationToken ct = default);

    /// <summary>Set giá trị vào cache với TTL.</summary>
    Task SetAsync(string key, string value, TimeSpan expiry, CancellationToken ct = default);

    /// <summary>Xóa cache entry theo key.</summary>
    Task DeleteAsync(string key, CancellationToken ct = default);

    /// <summary>Xóa nhiều cache entries theo pattern (vd: "query:tenant-id:*").</summary>
    Task DeleteByPatternAsync(string pattern, CancellationToken ct = default);
}
