using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;

namespace Gateway.Infrastructure;

// ─────────────────────────────────────────────────────────────────────────────
// QueryCacheService — Redis-backed cache for Semantic Layer query results
// ─────────────────────────────────────────────────────────────────────────────

public sealed class QueryCacheService(
    IDistributedCache cache,
    ILogger<QueryCacheService> logger)
{
    private static readonly TimeSpan DefaultTtl = TimeSpan.FromMinutes(5);

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>
    /// Tries to retrieve a cached JSON string by key.
    /// Returns <c>null</c> on miss.
    /// </summary>
    public async Task<string?> GetAsync(string cacheKey, CancellationToken ct)
    {
        string? value;
        try
        {
            value = await cache.GetStringAsync(cacheKey, ct);
        }
        catch (Exception ex)
        {
            logger.LogDebug(ex, "QueryCache: GET failed for key {Key} — treating as miss", cacheKey);
            return null;
        }

        if (value is not null)
        {
            logger.LogDebug("QueryCache: HIT  {Key}", cacheKey);
            return value;
        }

        logger.LogDebug("QueryCache: MISS {Key}", cacheKey);
        return null;
    }

    /// <summary>
    /// Stores a JSON string in the cache with an optional TTL.
    /// Falls back to <see cref="DefaultTtl"/> when <paramref name="ttl"/> is null.
    /// </summary>
    public async Task SetAsync(
        string cacheKey,
        string json,
        TimeSpan? ttl,
        CancellationToken ct)
    {
        var effectiveTtl = ttl ?? DefaultTtl;
        var entryOptions = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = effectiveTtl,
        };

        try
        {
            await cache.SetStringAsync(cacheKey, json, entryOptions, ct);
            logger.LogDebug("QueryCache: SET  {Key} (TTL {Ttl})", cacheKey, effectiveTtl);
        }
        catch (Exception ex)
        {
            // Cache write failures are non-fatal — log and continue
            logger.LogDebug(ex, "QueryCache: SET failed for key {Key}", cacheKey);
        }
    }

    // ── Static helpers ────────────────────────────────────────────────────────

    /// <summary>
    /// Builds a deterministic, SHA-256–based cache key from the tenant id
    /// and an arbitrary query input object serialized to JSON.
    /// </summary>
    public static string BuildCacheKey(Guid tenantId, object queryInput)
    {
        var raw = $"{tenantId}|{JsonSerializer.Serialize(queryInput)}";
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(raw));
        return $"ioc:query:{Convert.ToHexString(bytes).ToLowerInvariant()}";
    }
}
