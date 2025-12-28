namespace Contracts.Infrastructure.Caching;

public static class CacheEntryOptions
{
    public static DistributedCacheEntryOptions TtlDays(int days) =>
        new()
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(days)
        };
}