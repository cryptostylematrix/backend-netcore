using System.Threading.RateLimiting;
using Polly;
using Polly.RateLimiting;

namespace Contracts.Infrastructure.Ton;

public static class TonCenterPipelineFactory
{
    public static ResiliencePipeline Create(int rps, int queueLimit, int acquireTimeoutMs = 0)
    {
        // IMPORTANT: single shared limiter instance per app
        var limiter = new TokenBucketRateLimiter(new TokenBucketRateLimiterOptions
        {
            TokenLimit = rps,
            TokensPerPeriod = rps,
            ReplenishmentPeriod = TimeSpan.FromSeconds(1),
            AutoReplenishment = true,
            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
            QueueLimit = queueLimit
        });

        return new ResiliencePipelineBuilder()
            .AddRateLimiter(new RateLimiterStrategyOptions
            {
                // Your Polly version expects a delegate, not the limiter object.
                RateLimiter = args =>
                {
                    var ct = args.Context.CancellationToken;

                    if (acquireTimeoutMs <= 0) return limiter.AcquireAsync(permitCount: 1, cancellationToken: ct);
                    
                    using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                    cts.CancelAfter(acquireTimeoutMs);
                    return limiter.AcquireAsync(permitCount: 1, cancellationToken: cts.Token);

                },
                OnRejected = _ => throw new TonCenterRateLimitException("TonCenter rate limit exceeded (queued too long / queue full).")
            })
            .Build();
    }
}

public sealed class TonCenterRateLimitException(string message) : Exception(message);