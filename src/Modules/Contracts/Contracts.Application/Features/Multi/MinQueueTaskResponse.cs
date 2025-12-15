namespace Contracts.Application.Features.Multi;

public sealed class MinQueueTaskResponse
{
    public uint? Key { get; init; } 
    public MultiTaskItemResponse? Val { get; init; }
    public int Flag { get; init; }
}

public sealed class MultiTaskItemResponse
{
    public ulong QueryId { get; init; }
    public uint M { get; init; }
    public string ProfileAddr { get; init; } = null!;
    public MultiTaskPayloadResponse Payload { get; init; } = null!;
}

public sealed class MultiTaskPayloadResponse
{
    public uint Tag { get; init; }
    
    public string? SourceAddr { get; init; } 
    public PlacePosDataResponse? Pos { get; init; }
}

public sealed class PlacePosDataResponse
{
    public string ParentAddr { get; init; } = null!;
    public uint Pos { get; init; }
}