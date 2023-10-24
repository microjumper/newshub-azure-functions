using System;
using Microsoft.Extensions.Caching.Memory;

namespace newshub.functions.utils;

public static class CacheManager
{
    private static Lazy<IMemoryCache> lazyCache = new (new MemoryCache(new MemoryCacheOptions()));
    
    public static IMemoryCache Instance => lazyCache.Value;

    public static void Invalidate()
    {
        CacheManager.lazyCache = new (new MemoryCache(new MemoryCacheOptions()));
    }
}