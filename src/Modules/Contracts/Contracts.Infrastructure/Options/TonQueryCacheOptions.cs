namespace Contracts.Infrastructure.Options;

public sealed class TonQueryCacheOptions
{
    /// <summary>
    /// Prefix for all cache keys (useful for multi-env or multi-app Redis).
    /// </summary>
    public string KeyPrefix { get; init; } = "contracts";

    /// <summary>
    /// “As long as possible” TTL.
    /// Used for immutable or effectively immutable data.
    /// </summary>
    public int LongTtlDays { get; init; } = 365;

    /// <summary>Metadata cache lifetime in hours (1–168).</summary>
    public int JettonMetadataTtlHours { get; init; } = 24;

    /// <summary>
    /// GetNftDataAsync: cache when IsInit == -1
    /// </summary>
    public int NftDataIsInitMinusOneTtlDays { get; init; } = 1;

}
