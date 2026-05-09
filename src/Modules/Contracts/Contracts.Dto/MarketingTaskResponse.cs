namespace Contracts.Dto;

public sealed class MarketingTaskResponse
{
    [JsonPropertyName("query_id")]
    public ulong QueryId { get; init; }
    
    [JsonPropertyName("m")]
    public byte M { get; init; }
    
    [JsonPropertyName("profile_addr")]
    public string ProfileAddr { get; init; } = null!;
    
    [JsonPropertyName("payload")]
    public MarketingTaskPayloadResponse Payload { get; init; } = null!;
}

// buy_place#1  source: MsgAddress  amount: Coins  first:Bool  pos: (Maybe ^PlacePos) = MarketingTaskPayload;
// create_clone#2 = MarketingTaskPayload;
// lock_pos#3  source:MsgAddress  pos:^PlacePos = MarketingTaskPayload;
// unlock_pos#4  source:MsgAddress  pos:^PlacePos = MarketingTaskPayload;
// jetton_bonus#5  amount:Coins  place_number:#  title:Any = MarketingTaskPayload;
// reinvest#6 = MarketingTaskPayload;
// move_or_bonus#7  amount:Coins  place_number:#  title:Any = MarketingTaskPayload;
public sealed class MarketingTaskPayloadResponse
{
    [JsonPropertyName("tag")]
    public byte Tag { get; init; }
    
    [JsonPropertyName("source_addr")]
    public string? SourceAddr { get; init; } 
    
    [JsonPropertyName("amount")]
    public ulong? Amount { get; init; }
    
    [JsonPropertyName("first")]
    public bool? First { get; init; }
    
    [JsonPropertyName("place_number")]
    public uint? PlaceNumber { get; init; }
    
    [JsonPropertyName("title")]
    public string? Title { get; init; }
    
    [JsonPropertyName("pos")]
    public PosDataResponse? Pos { get; init; }
}

public sealed class PosDataResponse
{
    [JsonPropertyName("parent_addr")]
    public string ParentAddr { get; init; } = null!;
    
    [JsonPropertyName("pos")]
    public uint Pos { get; init; }
}