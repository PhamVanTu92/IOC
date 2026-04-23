using Microsoft.Extensions.Logging;
using QueryService.Application.Interfaces;
using StackExchange.Redis;

namespace QueryService.Infrastructure.Cache;

/// <summary>
/// Redis implementation của ICacheService.
/// Dùng StackExchange.Redis — connection được inject từ DI.
/// Tất cả keys có prefix "ioc:" để tránh collision.
/// </summary>
public sealed class RedisCacheService : ICacheService
{
    private readonly IDatabase _db;
    private readonly ILogger<RedisCacheService> _logger;
    private const string KeyPrefix = "ioc:query:";

    public RedisCacheService(IConnectionMultiplexer redis, ILogger<RedisCacheService> logger)
    {
        _db = redis.GetDatabase();
        _logger = logger;
    }

    public async Task<string?> GetAsync(string key, CancellationToken ct = default)
    {
        try
        {
            var value = await _db.StringGetAsync(PrefixKey(key));
            return value.HasValue ? value.ToString() : null;
        }
        catch (RedisException ex)
        {
            _logger.LogWarning(ex, "Redis GET failed for key {Key}", key);
            return null; // Cache miss on error — gracefully degrade
        }
    }

    public async Task SetAsync(string key, string value, TimeSpan expiry, CancellationToken ct = default)
    {
        try
        {
            await _db.StringSetAsync(PrefixKey(key), value, expiry);
        }
        catch (RedisException ex)
        {
            _logger.LogWarning(ex, "Redis SET failed for key {Key}", key);
            // Non-fatal — query result sẽ vẫn được trả về
        }
    }

    public async Task DeleteAsync(string key, CancellationToken ct = default)
    {
        try
        {
            await _db.KeyDeleteAsync(PrefixKey(key));
        }
        catch (RedisException ex)
        {
            _logger.LogWarning(ex, "Redis DELETE failed for key {Key}", key);
        }
    }

    public async Task DeleteByPatternAsync(string pattern, CancellationToken ct = default)
    {
        // Scan + delete — chỉ dùng trong admin/invalidation, không dùng trong hot path
        try
        {
            var server = _db.Multiplexer.GetServer(
                _db.Multiplexer.GetEndPoints().FirstOrDefault()
                    ?? throw new InvalidOperationException("No Redis endpoints configured."));

            var prefixedPattern = PrefixKey(pattern);
            var keys = server
                .Keys(pattern: prefixedPattern)
                .ToArray();

            if (keys.Length > 0)
                await _db.KeyDeleteAsync(keys);

            _logger.LogDebug("Deleted {Count} Redis keys matching pattern {Pattern}",
                keys.Length, pattern);
        }
        catch (RedisException ex)
        {
            _logger.LogWarning(ex, "Redis SCAN+DELETE failed for pattern {Pattern}", pattern);
        }
    }

    private static string PrefixKey(string key) => $"{KeyPrefix}{key}";
}
