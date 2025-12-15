namespace Contracts.Domain.Aggregates.Multi;

public sealed class MinQueueTask
{
    public BigInteger? Key { get; init; } 
    public MultiTaskItem? Val { get; init; }
    public BigInteger Flag { get; init; }
}