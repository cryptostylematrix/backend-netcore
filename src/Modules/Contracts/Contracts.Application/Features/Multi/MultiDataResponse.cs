namespace Contracts.Application.Features.Multi;

public sealed class MultiDataResponse
{
    public string ProcessorAddr { get; init; } = null!;
    public uint MaxTasks { get; init; }
    public uint QueueSize  { get; init; }
    public uint SeqNo { get; init; }
    public MultiFeesDataResponse Fees { get; init; } = null!;
    public MultiSecurityDataResponse Security { get; init; } = null!;
    public string PlaceCode { get; init; } = null!;
    public MultiQueueItemResponse[] Tasks { get; init; } = null!;
}

public sealed class MultiFeesDataResponse
{
    public decimal M1 { get; init; } 
    public decimal M2 { get; init; }
    public decimal M3 { get; init; } 
    public decimal M4 { get; init; } 
    public decimal M5 { get; init; } 
    public decimal M6 { get; init; } 
}

public sealed class MultiSecurityDataResponse
{
    public string AdminAddr { get; init; } = null!;
}

public sealed class MultiQueueItemResponse
{
    public uint Key { get; init; }
    public MultiTaskItemResponse Val { get; init; } = null!;
}