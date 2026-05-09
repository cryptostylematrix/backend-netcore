namespace Contracts.Dto;

public sealed class MatrixPlaceDataResponse
{
    [JsonPropertyName("init")]
    public bool Init { get; init; }
    
    [JsonPropertyName("marketing_addr")]
    public string MarketingAddr { get; init; } = null!;
    
    [JsonPropertyName("m")]
    public byte M { get; init; }
    
    [JsonPropertyName("parent_addr")]
    public string? ParentAddr { get; init; }
    
    [JsonPropertyName("pos")]
    public uint Pos { get; init; }
    
    [JsonPropertyName("seq_no")]
    public uint SeqNo { get; init; }
    
    [JsonPropertyName("width")]
    public byte Width { get; init; }
    
    [JsonPropertyName("height")]
    public byte Height { get; init; }
    
    [JsonPropertyName("admin_addr")]
    public string? AdminAddr { get; init; }
    
    [JsonPropertyName("info")]
    public PlaceInfoResponse? Info { get; init; }
    
    [JsonPropertyName("descendants")]
    public PlaceDescendantsResponse? Descendants { get; init; }
}

public sealed class PlaceInfoResponse
{
    [JsonPropertyName("kind")]
    public byte Kind { get; init; }
    
    [JsonPropertyName("profile_addr")]
    public string ProfileAddr { get; init; } = null!;
    
    [JsonPropertyName("place_number")]
    public uint PlaceNumber { get; init; }
    
    [JsonPropertyName("inviter_profile_addr")]
    public string? InviterProfileAddr { get; init; }
}

public sealed class PlaceDescendantsResponse
{
  
}

