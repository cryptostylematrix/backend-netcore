namespace Contracts.Infrastructure.Caching;

public static class DistributedCacheJson
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    extension(IDistributedCache cache)
    {
        public async Task<T?> GetJsonAsync<T>(string key, CancellationToken ct)
        {
            var bytes = await cache.GetAsync(key, ct);
            if (bytes is null || bytes.Length == 0) return default;
            return JsonSerializer.Deserialize<T>(bytes, JsonOptions);
        }

        public Task SetJsonAsync<T>(string key, T value, DistributedCacheEntryOptions options, CancellationToken ct)
        {
            var bytes = JsonSerializer.SerializeToUtf8Bytes(value, JsonOptions);
            return cache.SetAsync(key, bytes, options, ct);
        }
    }
}