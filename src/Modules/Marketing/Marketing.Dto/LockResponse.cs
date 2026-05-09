namespace Marketing.Dto;

public sealed class LockResponse
{
    [JsonPropertyName("marketing_addr")]
    public string MarketingAddr { get; init; } = null!;
    
    [JsonPropertyName("m")]
    public short M { get; init; }
    
    [JsonPropertyName("profile_addr")]
    public string ProfileAddr { get; init; } = null!;
    
    [JsonPropertyName("place_addr")]
    public string PlaceAddr { get; init; } = null!;
    
    [JsonPropertyName("locked_pos")]
    public short LockedPos { get; init; }
    
    [JsonPropertyName("place_profile_login")]
    public string PlaceProfileLogin { get; init; } = null!;
    
    [JsonPropertyName("place_number")]
    public int PlaceNumber { get; init; }
    
    [JsonPropertyName("created_at")]
    public long CreatedAt { get; init; }
}