namespace Contracts.Application.Features.Multi;

public sealed class MinQueueTaskResponse
{
    [JsonPropertyName("key")]
    public uint? Key { get; init; } 
    
    [JsonPropertyName("val")]
    public MultiTaskItemResponse? Val { get; init; }
    
    [JsonPropertyName("flag")]
    public int Flag { get; init; }
}

public sealed class MultiTaskItemResponse
{
    [JsonPropertyName("query_id")]
    public ulong QueryId { get; init; }
    
    [JsonPropertyName("m")]
    public uint M { get; init; }
    
    [JsonPropertyName("profile_addr")]
    public string ProfileAddr { get; init; } = null!;
    
    [JsonPropertyName("payload")]
    public MultiTaskPayloadResponse Payload { get; init; } = null!;
}

public sealed class MultiTaskPayloadResponse
{
    [JsonPropertyName("tag")]
    public uint Tag { get; init; }
    
    [JsonPropertyName("source_addr")]
    public string? SourceAddr { get; init; } 
    
    [JsonPropertyName("pos")]
    public PlacePosDataResponse? Pos { get; init; }
}

public sealed class PlacePosDataResponse
{
    [JsonPropertyName("parent_addr")]
    public string ParentAddr { get; init; } = null!;
    
    [JsonPropertyName("pos")]
    public uint Pos { get; init; }
}