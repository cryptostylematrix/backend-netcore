namespace Contracts.Infrastructure.Options;

public sealed class TonCenterOptions
{
    public string Endpoint { get; init; } = null!;
    public string ApiKey { get; init; } = null!;
    
    // TonCenter cap
    public int RequestsPerSecond { get; init; } = 20;

    // how many requests can wait in queue before rejecting
    public int QueueLimit { get; init; } = 500;

    // optional timeout for waiting on a permit (0 = no extra timeout)
    public int AcquireTimeoutMs { get; init; } = 0;
}