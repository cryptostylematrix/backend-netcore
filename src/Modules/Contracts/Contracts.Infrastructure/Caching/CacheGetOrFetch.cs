namespace Contracts.Infrastructure.Caching;

public static class CacheGetOrFetch
{
    public static async Task<Result<T>> GetOrFetchAsync<T>(
        IDistributedCache cache,
        string key,
        Func<CancellationToken, Task<Result<T>>> fetch,
        Func<T, bool> shouldCache,
        DistributedCacheEntryOptions options,
        CancellationToken ct)
        where T : class
    {
        var cached = await cache.GetJsonAsync<T>(key, ct);
        if (cached is not null)
            return Result.Success(cached);

        var res = await fetch(ct);
        if (!res.IsSuccess || res.Value is null)
            return res;

        if (shouldCache(res.Value))
            await cache.SetJsonAsync(key, res.Value, options, ct);

        return res;
    }
}