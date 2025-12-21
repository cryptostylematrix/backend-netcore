namespace Matrix.Application.Features.Places;

public sealed class PlaceResponse
{
    [JsonPropertyName("addr")]
    public string Addr { get; init; } = null!;
    
    [JsonPropertyName("parent_addr")]
    public string ParentAddr { get; init; } = null!;
    
    [JsonPropertyName("place_number")]
    public int PlaceNumber { get; init; }
    
    [JsonPropertyName("created_at")]
    public long CreatedAt { get; init; }
    
    [JsonPropertyName("fill_count")]
    public short FillCount { get;  init; }
    
    [JsonPropertyName("clone")]
    public short Clone { get; init; }
    
    [JsonPropertyName("pos")]
    public short Pos { get; init; }
    
    [JsonPropertyName("login")]
    public string Login { get; init; } = null!;
    
    [JsonPropertyName("m")]
    public short M { get; init; }
    
    [JsonPropertyName("profile_addr")]
    public string ProfileAddr { get; init; } = null!;
}