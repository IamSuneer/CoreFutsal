using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace CoreFutsal.Auth.Tests;

internal static class TestHelpers
{
    public static IDistributedCache MemoryCache() =>
        new MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions()));
}
