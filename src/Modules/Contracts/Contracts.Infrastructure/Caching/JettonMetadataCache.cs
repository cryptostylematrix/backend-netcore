namespace Contracts.Infrastructure.Caching;

// Bounded gates avoid duplicate network requests without retaining a lock for every address.
public sealed class JettonMetadataCache(IDistributedCache cache, IOptions<TonQueryCacheOptions> options) : IDisposable
{
    private readonly SemaphoreSlim[] gates = Enumerable.Range(0, 64).Select(_ => new SemaphoreSlim(1, 1)).ToArray();

    public async Task<Result<T>> GetAsync<T>(string key, Func<CancellationToken, Task<Result<T>>> fetch, CancellationToken ct)
        where T : class
    {
        key = $"{options.Value.KeyPrefix}:jetton-metadata:v1:{key}";
        var gate = gates[(uint)StringComparer.Ordinal.GetHashCode(key) % gates.Length];
        await gate.WaitAsync(ct);
        try
        {
            return await CacheGetOrFetch.GetOrFetchAsync(cache, key, fetch, _ => true,
                new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(Math.Clamp(options.Value.JettonMetadataTtlHours, 1, 168)) }, ct);
        }
        finally { gate.Release(); }
    }

    public void Dispose()
    {
        foreach (var gate in gates) gate.Dispose();
    }
}
