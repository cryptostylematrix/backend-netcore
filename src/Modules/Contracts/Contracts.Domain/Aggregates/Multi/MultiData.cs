namespace Contracts.Domain.Aggregates.Multi;

public sealed class MultiData
{
    public Address Processor { get; init; } = null!;
    public BigInteger MaxTasks { get; init; }
    public BigInteger QueueSize  { get; init; }
    public BigInteger SeqNo { get; init; }
    public MultiFeesData Fees { get; init; } = null!;
    public MultiSecurityData Security { get; init; } = null!;
    public Cell PlaceCode { get; init; } = null!;
    public List<MultiQueueItem> Tasks { get; init; } = null!;
}