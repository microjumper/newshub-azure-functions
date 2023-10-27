using System;
using Microsoft.Extensions.Caching.Memory;

namespace newshub.functions.utils;

public static class CacheManager
{
    private static readonly Lazy<MemoryCache> lazyCache = new (new MemoryCache(new MemoryCacheOptions()));
    
    public static MemoryCache Instance => lazyCache.Value;

    public static void Invalidate()
    {
        Instance.Compact(1);    // Remove at least the given percentage (0.10 for 10%) of the total entries
    }
}