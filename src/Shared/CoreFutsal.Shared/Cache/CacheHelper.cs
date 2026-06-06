using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;

namespace CoreFutsal.Shared.Cache;

public static class CacheHelper
{
    private static readonly JsonSerializerOptions _json = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public static async Task<T?> GetAsync<T>(IDistributedCache cache, string key, CancellationToken ct = default)
    {
        try
        {
            var raw = await cache.GetStringAsync(key, ct);
            return raw is null ? default : JsonSerializer.Deserialize<T>(raw, _json);
        }
        catch
        {
            return default;
        }
    }

    public static async Task SetAsync<T>(IDistributedCache cache, string key, T value, TimeSpan ttl, CancellationToken ct = default)
    {
        try
        {
            var json = JsonSerializer.Serialize(value, _json);
            await cache.SetStringAsync(key, json, new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = ttl
            }, ct);
        }
        catch { /* cache write failure must never break the API */ }
    }

    public static async Task RemoveAsync(IDistributedCache cache, CancellationToken ct, params string[] keys)
    {
        try
        {
            await Task.WhenAll(keys.Select(k => cache.RemoveAsync(k, ct)));
        }
        catch { }
    }
}
