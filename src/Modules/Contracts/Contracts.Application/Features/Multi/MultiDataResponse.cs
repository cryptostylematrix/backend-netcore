namespace Contracts.Application.Features.Multi;

public sealed class MultiDataResponse
{
    [JsonPropertyName("processor_addr")]
    public string ProcessorAddr { get; init; } = null!;
    
    [JsonPropertyName("max_tasks")]
    public uint MaxTasks { get; init; }
    
    [JsonPropertyName("queue_size")]
    public uint QueueSize  { get; init; }
    
    [JsonPropertyName("seq_no")]
    public uint SeqNo { get; init; }
    
    [JsonPropertyName("fees")]
    public MultiFeesDataResponse Fees { get; init; } = null!;
    
    [JsonPropertyName("security")]
    public MultiSecurityDataResponse Security { get; init; } = null!;
    
    [JsonPropertyName("place_code")]
    public string PlaceCode { get; init; } = null!;
    
    [JsonPropertyName("tasks")]
    public MultiQueueItemResponse[] Tasks { get; init; } = null!;
}

public sealed class MultiFeesDataResponse
{
    [JsonPropertyName("m1")]
    public decimal M1 { get; init; } 
    
    [JsonPropertyName("m2")]
    public decimal M2 { get; init; }
    
    [JsonPropertyName("m3")]
    public decimal M3 { get; init; } 
    
    [JsonPropertyName("m4")]
    public decimal M4 { get; init; } 
    
    [JsonPropertyName("m5")]
    public decimal M5 { get; init; } 
    
    [JsonPropertyName("m6")]
    public decimal M6 { get; init; } 
}

public sealed class MultiSecurityDataResponse
{
    [JsonPropertyName("admin_addr")]
    public string AdminAddr { get; init; } = null!;
}

public sealed class MultiQueueItemResponse
{
    [JsonPropertyName("key")]
    public uint Key { get; init; }
    
    [JsonPropertyName("val")]
    public MultiTaskItemResponse Val { get; init; } = null!;
}